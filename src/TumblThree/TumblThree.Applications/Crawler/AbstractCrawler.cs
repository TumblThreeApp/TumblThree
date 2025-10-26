﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TumblThree.Applications.Converter;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractCrawler
    {
        private const int MAX_PATH = 260;
        private const int MaximumComponentLength = 255;

        protected IBlog Blog { get; }
        protected IProgress<DownloadProgress> Progress { get; }
        protected ISharedCookieService CookieService { get; }
        protected IWebRequestFactory WebRequestFactory { get; }
        protected object LockObjectDb { get; } = new object();
        protected object LockObjectDirectory { get; } = new object();
        protected object LockObjectDownload { get; } = new object();
        protected object LockObjectProgress { get; } = new object();
        protected ICrawlerService CrawlerService { get; }
        protected IShellService ShellService { get; }
        protected PauseToken Pt { get; }
        protected CancellationToken Ct { get; }

        private readonly CancellationTokenSource InterruptionTokenSource;

        private readonly CancellationTokenSource LinkedTokenSource;
        protected IPostQueue<AbstractPost> PostQueue { get; }
        protected ConcurrentBag<TumblrPost> StatisticsBag { get; set; } = new ConcurrentBag<TumblrPost>();
        protected List<string> Tags { get; set; } = new List<string>();

        protected IDownloader Downloader;

        protected AbstractCrawler(IShellService shellService, ICrawlerService crawlerService, IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IPostQueue<AbstractPost> postQueue, IBlog blog, IDownloader downloader,
            PauseToken pt, CancellationToken ct)
        {
            ShellService = shellService;
            CrawlerService = crawlerService;
            WebRequestFactory = webRequestFactory;
            CookieService = cookieService;
            PostQueue = postQueue;
            Blog = blog;
            Downloader = downloader;
            Progress = progress;
            Pt = pt;
            // TODO: Find a better way for this construct
            InterruptionTokenSource = new CancellationTokenSource();
            LinkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct, InterruptionTokenSource.Token);
            Ct = LinkedTokenSource.Token;
        }

        public virtual async Task UpdateMetaInformationAsync()
        {
            await Task.FromResult<object>(null);
        }

        public virtual async Task IsBlogOnlineAsync()
        {
            try
            {
                string[] cookieHosts = { "https://www.tumblr.com/" };
                await RequestDataAsync(Blog.Url, null, cookieHosts);
                Blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                Logger.Error("AbstractCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
                Blog.Online = false;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                Blog.Online = false;
            }
            catch (Exception ex) when (ex.Message == "Acceptance of privacy consent needed!")
            {
                Blog.Online = false;
            }
        }

        public void UpdateProgressQueueInformation(string format, params object[] args)
        {
            var newProgress = new DownloadProgress
            {
                Progress = string.Format(CultureInfo.CurrentCulture, format, args)
            };
            Progress.Report(newProgress);
        }

        public void InterruptionRequestedEventHandler(object sender, EventArgs e)
        {
            InterruptionTokenSource.Cancel();
        }

        protected async Task<T> ThrottleConnectionAsync<T>(string url, Func<string, Task<T>> method)
        {
            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }

            return await method(url);
        }

        protected async Task<string> RequestDataAsync(string url, Dictionary<string, string> headers = null,
            IEnumerable<string> cookieHosts = null)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                int redirects = 0;
                ResponseDetails responseDetails;

                do
                {
                    HttpWebRequest request = WebRequestFactory.CreateGetRequest(url, string.Empty, headers, false);
                    cookieHosts = cookieHosts ?? new List<string>();
                    string cookieDomain = null;
                    foreach (string cookieHost in cookieHosts)
                    {
                        if (cookieDomain == null) cookieDomain = new Uri(cookieHost).Host;
                        CookieService.GetUriCookie(request.CookieContainer, new Uri(cookieHost));
                    }

                    requestRegistration = Ct.Register(() => request.Abort());
                    responseDetails = await WebRequestFactory.ReadRequestToEnd2Async(request, cookieDomain);

                    url = responseDetails.RedirectUrl ?? url;

                    if (responseDetails.HttpStatusCode == HttpStatusCode.Found)
                    {
                        if (url.Contains("privacy/consent"))
                        {
                            var ex = new Exception("Acceptance of privacy consent needed!");
                            ShellService.ShowError(new TumblrPrivacyConsentException(ex), Resources.ConfirmationTumblrPrivacyConsentNeeded);
                            throw ex;
                        }
                        if (!url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                            url = request.RequestUri.GetLeftPart(UriPartial.Authority) + url;
                    }

                    if (responseDetails.HttpStatusCode == HttpStatusCode.Moved)
                    {
                        Uri uri = new Uri(request.RequestUri, url);
                        url = uri.ToString();
                        if (!uri.Authority.Contains(".tumblr.")) Blog.Url = uri.GetLeftPart(UriPartial.Authority);
                    }

                } while ((responseDetails.HttpStatusCode == HttpStatusCode.Found || responseDetails.HttpStatusCode == HttpStatusCode.Moved) && redirects++ < 5);

                if (responseDetails.HttpStatusCode == HttpStatusCode.Found) throw new WebException("Too many automatic redirections were attempted.", WebExceptionStatus.ProtocolError);

                return responseDetails.Response;
            }
            catch (Exception e)
            {
                Logger.Error("AbstractCrawler.RequestDataAsync: {0}", e);
                throw;
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        protected async Task<string> RequestApiDataAsync(string url, string bearerToken, Dictionary<string, string> headers = null,
            IEnumerable<string> cookieHosts = null)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = WebRequestFactory.CreateGetRequest(url, string.Empty, headers);
                cookieHosts = cookieHosts ?? new List<string>();
                foreach (string cookieHost in cookieHosts)
                {
                    CookieService.GetUriCookie(request.CookieContainer, new Uri(cookieHost));
                }

                request.PreAuthenticate = true;
                request.Headers.Add("Authorization", "Bearer " + bearerToken);
                request.Accept = "application/json";

                requestRegistration = Ct.Register(() => request.Abort());
                return await WebRequestFactory.ReadRequestToEndAsync(request);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        protected async Task<string> PostDataAsync(string url, string referer, Dictionary<string, string> parameters, IEnumerable<string> cookieHosts = null)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                var request = WebRequestFactory.CreatePostRequest(url, referer);
                cookieHosts = cookieHosts ?? new List<string>();
                foreach (string cookieHost in cookieHosts)
                {
                    CookieService.GetUriCookie(request.CookieContainer, new Uri(cookieHost));
                }
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                requestRegistration = Ct.Register(() => request.Abort());
                await WebRequestFactory.PerformPostRequestAsync(request, parameters);
                var document = await WebRequestFactory.ReadRequestToEndAsync(request);
                return document;
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        public virtual T ConvertJsonToClass<T>(string json) where T : new()
        {
            try
            {
                json = json.Replace(":undefined", ":null");
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    return (T)serializer.ReadObject(ms);
                }
            }
            catch (SerializationException serializationException)
            {
                if (json.TrimStart(new char[] { '\r', '\n', ' ' }).StartsWith("<"))
                {
                    Logger.Error("AbstractCrawler:ConvertJsonToClass<T>: {0}", "Html instead of Json data");
                    ShellService.ShowError(serializationException, Resources.GotHtmlNotJson, Blog.Name);
                }
                else
                {
                    Logger.Error("AbstractCrawler:ConvertJsonToClass<T>: {0}", "Could not parse data");
                    ShellService.ShowError(serializationException, Resources.PostNotParsable, Blog.Name);
                }
                return new T();
            }
        }

        public virtual T ConvertJsonToClassNew<T>(string json, bool ignoreMetadata = false) where T : new()
        {
            try
            {
                json = json.Replace(":undefined", ":null");
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var deserializer = new Newtonsoft.Json.JsonSerializer();
                    deserializer.MetadataPropertyHandling = ignoreMetadata ? Newtonsoft.Json.MetadataPropertyHandling.Ignore : Newtonsoft.Json.MetadataPropertyHandling.Default;
                    deserializer.Converters.Add(new SingleOrArrayConverter<T>());
                    using (StreamReader sr = new StreamReader(ms))
                    using (var jsonTextReader = new Newtonsoft.Json.JsonTextReader(sr))
                    {
                        return deserializer.Deserialize<T>(jsonTextReader);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonException serializationException)
            {
                Logger.Error("AbstractCrawler:ConvertJsonToClassNew<T>: {0}", "Could not parse data");
                ShellService.ShowError(serializationException, Resources.PostNotParsable, Blog.Name);
                return new T();
            }
        }

        protected string GetCollectionName(IBlog blog)
        {
            return ShellService.Settings.GetCollection(blog.CollectionId)?.Name ?? "";
        }

        protected static string UrlEncode(IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> val in parameters)
            {
                sb.AppendFormat("{0}={1}&", val.Key, HttpUtility.UrlEncode(val.Value));
            }

            sb.Remove(sb.Length - 1, 1); // remove last '&'
            return sb.ToString();
        }

        protected virtual IEnumerable<int> GetPageNumbers()
        {
            return string.IsNullOrEmpty(Blog.DownloadPages)
                ? Enumerable.Range(0, ShellService.Settings.ConcurrentScans)
                : RangeToSequence(Blog.DownloadPages);
        }

        protected static bool TestRange(int numberToCheck, int bottom, int top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
        }

        protected static IEnumerable<int> RangeToSequence(string input)
        {
            string[] parts = input.Split(',');
            foreach (string part in parts)
            {
                if (!part.Contains('-'))
                {
                    yield return int.Parse(part);
                    continue;
                }

                string[] rangeParts = part.Split('-');
                int start = int.Parse(rangeParts[0]);
                int end = int.Parse(rangeParts[1]);

                while (start <= end)
                {
                    yield return start;
                    start++;
                }
            }
        }

        protected void AddToDownloadList(TumblrPost addToList)
        {
            PostQueue.Add(addToList);
            StatisticsBag.Add(addToList);
        }

        protected ulong GetLastPostId()
        {
            if (Blog.ForceRescan)
            {
                return 0;
            }
            return !string.IsNullOrEmpty(Blog.DownloadPages) ? 0 : Blog.LastId;
        }

        protected void GenerateTags()
        {
            if (!string.IsNullOrWhiteSpace(Blog.Tags))
            {
                Tags = Blog.Tags.Split(',').Select(x => x.Trim()).ToList();
            }
        }

        protected static string Sanitize(string filename)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("-", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        protected static string ReplaceCI(string input, string search, string replacement)
        {
            string result = Regex.Replace(
                input,
                Regex.Escape(search),
                replacement.Replace("$", "$$"),
                RegexOptions.IgnoreCase
            );
            return result;
        }

        protected static bool ContainsCI(string input, string search)
        {
            return input.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected static string FileName(string url)
        {
            return url.Split('/').Last();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        protected virtual string BuildFileNameCore(string url, string blogName, DateTime date, int timestamp, int index, string type, string id,
            List<string> tags, string slug, string title, string rebloggedFromName, string rebloggedRootName, string reblogKey, int noteCount,
            bool extraReserve = false)
        {
            /*
             * Replaced are:
             *  %f  original filename (default)
                %b  blog name
                %d  post date (yyyyMMdd)
                %e  post date and time (yyyyMMddHHmmss)
                %g  post date in GMT (yyyyMMdd)
                %h  post date and time in GMT (yyyyMMddHHmmss)
                %u  post timestamp (number)
                %o  blog name of reblog origin
                %q  blog name of origin (either reblog origin or blog)
                %p  post title (shorted if needed…)
                %i  post id
                %n  image index (of photo sets)
                %t  for all tags (cute+cats,big+dogs)
                %r  for reblog ("" / "reblog")
                %s  slug (last part of a post's url)
                %k  reblog-key
                %l  likes/note count
               Tokens to make filenames unique:
                %x  "_{number}" ({number}: 2..n)
                %y  " ({number})" ({number}: 2..n)
             */
            url = url ?? "";
            blogName = blogName ?? "";
            id = id ?? "";
            slug = slug ?? "";
            title = title ?? "";
            rebloggedFromName = rebloggedFromName ?? "";
            reblogKey = reblogKey ?? "";

            url = url.IndexOf('?') > 0 ? url.Substring(0, url.IndexOf('?')) : url;

            string extension = Path.GetExtension(FileName(url));
            if (extension.ToLower() == ".gifv")
                extension = ".gif";
            else if (extension.ToLower() == ".pnj")
                extension += $".{Blog.PnjDownloadFormat}";
            string filename = Blog.FilenameTemplate + extension;
            if (ContainsCI(filename, "%f")) filename = ReplaceCI(filename, "%f", Path.GetFileNameWithoutExtension(FileName(url)));
            if (ContainsCI(filename, "%d")) filename = ReplaceCI(filename, "%d", date.ToString("yyyyMMdd"));
            if (ContainsCI(filename, "%e")) filename = ReplaceCI(filename, "%e", date.ToString("yyyyMMddHHmmss"));
            if (ContainsCI(filename, "%g")) filename = ReplaceCI(filename, "%g", date.ToUniversalTime().ToString("yyyyMMdd"));
            if (ContainsCI(filename, "%h")) filename = ReplaceCI(filename, "%h", date.ToUniversalTime().ToString("yyyyMMddHHmmss"));
            if (ContainsCI(filename, "%u")) filename = ReplaceCI(filename, "%u", timestamp.ToString());
            if (ContainsCI(filename, "%b")) filename = ReplaceCI(filename, "%b", blogName);
            if (ContainsCI(filename, "%i"))
            {
                if (type == "photo" && Blog.GroupPhotoSets && index != -1) id = $"{id}_{index}";
                filename = ReplaceCI(filename, "%i", id);
            }
            else if (type == "photo" && Blog.GroupPhotoSets && index != -1)
            {
                filename = $"{id}_{index}_{filename}";
            }
            if (ContainsCI(filename, "%n"))
            {
                if (type != "photo" || index == -1)
                {
                    string charBefore = "";
                    string charAfter = "";
                    if (filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) > 0)
                        charBefore = filename.Substring(filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) - 1, 1);
                    if (filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) < filename.Length - 2)
                        charAfter = filename.Substring(filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) + 2, 1);
                    if (charBefore == charAfter)
                        filename = filename.Remove(filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) - 1, 1);
                    filename = ReplaceCI(filename, "%n", "");
                }
                else
                {
                    filename = ReplaceCI(filename, "%n", index.ToString());
                }
            }
            if (ContainsCI(filename, "%t")) filename = ReplaceCI(filename, "%t", string.Join(",", tags).Replace(" ", "+"));
            if (ContainsCI(filename, "%r"))
            {
                if (rebloggedFromName.Length == 0 && filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) > 0 &&
                    filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) < filename.Length - 2 &&
                    filename.Substring(filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) - 1, 1) == filename.Substring(filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) + 2, 1))
                {
                    filename = filename.Remove(filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase), 3);
                }
                filename = ReplaceCI(filename, "%r", (rebloggedFromName.Length == 0 ? "" : "reblog"));
            }
            if (ContainsCI(filename, "%o"))
            {
                filename = ReplaceCI(filename, "%o", string.IsNullOrEmpty(rebloggedRootName) ? rebloggedFromName : rebloggedRootName);
            }
            if (ContainsCI(filename, "%q"))
            {
                var reblogOrigin = string.IsNullOrEmpty(rebloggedRootName) ? rebloggedFromName : rebloggedRootName;
                filename = ReplaceCI(filename, "%q", string.IsNullOrEmpty(reblogOrigin) ? blogName : reblogOrigin);
            }
            if (ContainsCI(filename, "%s")) filename = ReplaceCI(filename, "%s", slug);
            if (ContainsCI(filename, "%k")) filename = ReplaceCI(filename, "%k", reblogKey);
            if (ContainsCI(filename, "%l")) filename = ReplaceCI(filename, "%l", noteCount.ToString());
            int neededCharactersForNumbering = 0;
            if (ContainsCI(filename, "%x"))
            {
                neededCharactersForNumbering = 6;
                Downloader.AppendTemplate = "_<0>";
                filename = ReplaceCI(filename, "%x", "");
            }
            if (ContainsCI(filename, "%y"))
            {
                neededCharactersForNumbering = 8;
                Downloader.AppendTemplate = " (<0>)";
                filename = ReplaceCI(filename, "%y", "");
            }

            int tokenLength = ContainsCI(filename, "%p") ? 2 : 0;
            int maxCharacters = (ShellService.IsLongPathSupported ? MaximumComponentLength : MAX_PATH - 1) - (extraReserve ? 4 : 0);
            int intendedLength = ShellService.IsLongPathSupported ? filename.Length : Path.Combine(Blog.DownloadLocation(), filename).Length;
            // without long path support: 259 (max path minus NULL) - current filename length + 2 chars (%p) - chars for numbering
            int charactersLeft = maxCharacters - intendedLength + tokenLength - neededCharactersForNumbering;
            if (charactersLeft < 0) throw new PathTooLongException($"{Blog.Name}: filename for post id {id} is too long");
            if (ContainsCI(filename, "%p"))
            {
                string _title = charactersLeft == 0 ? "" : (charactersLeft < title.Length) ? title.Substring(0, charactersLeft - 1) + "…" : title;
                filename = ReplaceCI(filename, "%p", _title);
            }

            return Sanitize(filename);
        }

        protected void UpdateBlogStats(bool add)
        {
            if (add)
            {
                Blog.TotalCount += StatisticsBag.Count;
                Blog.Photos += StatisticsBag.Count(post => post.GetType() == typeof(PhotoPost));
                Blog.Videos += StatisticsBag.Count(post => post.GetType() == typeof(VideoPost));
                Blog.Audios += StatisticsBag.Count(post => post.GetType() == typeof(AudioPost));
                Blog.Texts += StatisticsBag.Count(post => post.GetType() == typeof(TextPost));
                Blog.Answers += StatisticsBag.Count(post => post.GetType() == typeof(AnswerPost));
                Blog.Conversations += StatisticsBag.Count(post => post.GetType() == typeof(ConversationPost));
                Blog.Quotes += StatisticsBag.Count(post => post.GetType() == typeof(QuotePost));
                Blog.NumberOfLinks += StatisticsBag.Count(post => post.GetType() == typeof(LinkPost));
                Blog.PhotoMetas += StatisticsBag.Count(post => post.GetType() == typeof(PhotoMetaPost));
                Blog.VideoMetas += StatisticsBag.Count(post => post.GetType() == typeof(VideoMetaPost));
                Blog.AudioMetas += StatisticsBag.Count(post => post.GetType() == typeof(AudioMetaPost));
            }
            else
            {
                Blog.TotalCount = StatisticsBag.Count;
                Blog.Photos = StatisticsBag.Count(post => post.GetType() == typeof(PhotoPost));
                Blog.Videos = StatisticsBag.Count(post => post.GetType() == typeof(VideoPost));
                Blog.Audios = StatisticsBag.Count(post => post.GetType() == typeof(AudioPost));
                Blog.Texts = StatisticsBag.Count(post => post.GetType() == typeof(TextPost));
                Blog.Answers = StatisticsBag.Count(post => post.GetType() == typeof(AnswerPost));
                Blog.Conversations = StatisticsBag.Count(post => post.GetType() == typeof(ConversationPost));
                Blog.Quotes = StatisticsBag.Count(post => post.GetType() == typeof(QuotePost));
                Blog.NumberOfLinks = StatisticsBag.Count(post => post.GetType() == typeof(LinkPost));
                Blog.PhotoMetas = StatisticsBag.Count(post => post.GetType() == typeof(PhotoMetaPost));
                Blog.VideoMetas = StatisticsBag.Count(post => post.GetType() == typeof(VideoMetaPost));
                Blog.AudioMetas = StatisticsBag.Count(post => post.GetType() == typeof(AudioMetaPost));
            }
        }

        protected int DetermineDuplicates<T>() => StatisticsBag.Where(url => url.GetType() == typeof(T))
                                .GroupBy(url => url.Url)
                                .Where(g => g.Count() > 1)
                                .Sum(g => g.Count() - 1);

        protected void CleanCollectedBlogStatistics() => StatisticsBag = null;

        protected bool CheckIfShouldStop() => Ct.IsCancellationRequested;

        protected void CheckIfShouldPause()
        {
            if (Pt.IsPaused)
            {
                Pt.WaitWhilePausedWithResponseAsync().Wait();
            }
        }

        protected void HandleTimeoutException(TimeoutException timeoutException, string duringAction)
        {
            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.TimeoutReached, duringAction, Blog.Name), timeoutException?.Message);
            ShellService.ShowError(timeoutException, Resources.TimeoutReached, duringAction, Blog.Name);
        }

        protected bool HandleServiceUnavailableWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || !(resp.StatusCode == HttpStatusCode.ServiceUnavailable || resp.StatusCode == HttpStatusCode.Unauthorized))
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.NotLoggedIn, Blog.Name), webException.Message);
            ShellService.ShowError(webException, Resources.NotLoggedIn, Blog.Name);
            return true;
        }

        protected bool HandleNotFoundWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.BlogIsOffline, Blog.Name), webException.Message);
            ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
            return true;
        }

        protected enum LimitExceededSource
        {
            tumblr,
            twitter,
            bluesky
        }

        protected bool HandleLimitExceededWebException(WebException webException, LimitExceededSource source = LimitExceededSource.tumblr)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || (int)resp.StatusCode != 429)
            {
                return false;
            }

            var resource = source == LimitExceededSource.tumblr ? Resources.LimitExceeded : Resources.LimitExceededWaitPeriod;
            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, resource, Blog.Name), webException.Message);
            ShellService.ShowError(webException, resource, Blog.Name);
            return true;
        }

        protected bool HandleUnauthorizedWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || resp.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.PasswordProtected, Blog.Name), webException.Message);
            ShellService.ShowError(webException, Resources.PasswordProtected, Blog.Name);
            return true;
        }

        protected bool HandleUnauthorizedWebException2(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || resp.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.NotLoggedInX, Blog.Name), webException.Message);
            ShellService.ShowError(webException, Resources.NotLoggedInX, Blog.Name);
            return true;
        }

        protected bool HandleUnauthorizedWebExceptionRetry(WebException webException)
        {
            if (!ShellService.Settings.TumblrAuthErrorAutoRetry)
            {
                return false;
            }

            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || resp.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.AuthErrorRetrying, Blog.Name), webException.Message);
            ShellService.ShowError(webException, Resources.AuthErrorRetrying, Blog.Name);
            return true;
        }
    }
}
