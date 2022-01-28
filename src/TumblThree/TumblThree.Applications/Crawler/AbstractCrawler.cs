using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
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
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractCrawler
    {
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
                    foreach (string cookieHost in cookieHosts)
                    {
                        CookieService.GetUriCookie(request.CookieContainer, new Uri(cookieHost));
                    }

                    requestRegistration = Ct.Register(() => request.Abort());
                    responseDetails = await WebRequestFactory.ReadRequestToEnd2Async(request);

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
                        Uri uri = new Uri(url);
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
                Logger.Error("AbstractCrawler:ConvertJsonToClass<T>: {0}", "Could not parse data");
                ShellService.ShowError(serializationException, Resources.PostNotParsable, Blog.Name);
                return new T();
            }
        }

        public virtual T ConvertJsonToClassNew<T>(string json) where T : new()
        {
            try
            {
                json = json.Replace(":undefined", ":null");
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var deserializer = new Newtonsoft.Json.JsonSerializer();
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

        protected void UpdateBlogStats(bool add)
        {
            if (add)
            {
                Blog.TotalCount += StatisticsBag.Count;
                Blog.Photos += StatisticsBag.Count(url => url.GetType() == typeof(PhotoPost));
                Blog.Videos += StatisticsBag.Count(url => url.GetType() == typeof(VideoPost));
                Blog.Audios += StatisticsBag.Count(url => url.GetType() == typeof(AudioPost));
                Blog.Texts += StatisticsBag.Count(url => url.GetType() == typeof(TextPost));
                Blog.Answers += StatisticsBag.Count(url => url.GetType() == typeof(AnswerPost));
                Blog.Conversations += StatisticsBag.Count(url => url.GetType() == typeof(ConversationPost));
                Blog.Quotes += StatisticsBag.Count(url => url.GetType() == typeof(QuotePost));
                Blog.NumberOfLinks += StatisticsBag.Count(url => url.GetType() == typeof(LinkPost));
                Blog.PhotoMetas += StatisticsBag.Count(url => url.GetType() == typeof(PhotoMetaPost));
                Blog.VideoMetas += StatisticsBag.Count(url => url.GetType() == typeof(VideoMetaPost));
                Blog.AudioMetas += StatisticsBag.Count(url => url.GetType() == typeof(AudioMetaPost));
            }
            else
            {
                Blog.TotalCount = StatisticsBag.Count;
                Blog.Photos = StatisticsBag.Count(url => url.GetType() == typeof(PhotoPost));
                Blog.Videos = StatisticsBag.Count(url => url.GetType() == typeof(VideoPost));
                Blog.Audios = StatisticsBag.Count(url => url.GetType() == typeof(AudioPost));
                Blog.Texts = StatisticsBag.Count(url => url.GetType() == typeof(TextPost));
                Blog.Answers = StatisticsBag.Count(url => url.GetType() == typeof(AnswerPost));
                Blog.Conversations = StatisticsBag.Count(url => url.GetType() == typeof(ConversationPost));
                Blog.Quotes = StatisticsBag.Count(url => url.GetType() == typeof(QuotePost));
                Blog.NumberOfLinks = StatisticsBag.Count(url => url.GetType() == typeof(LinkPost));
                Blog.PhotoMetas = StatisticsBag.Count(url => url.GetType() == typeof(PhotoMetaPost));
                Blog.VideoMetas = StatisticsBag.Count(url => url.GetType() == typeof(VideoMetaPost));
                Blog.AudioMetas = StatisticsBag.Count(url => url.GetType() == typeof(AudioMetaPost));
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
                Pt.WaitWhilePausedWithResponseAsyc().Wait();
            }
        }

        protected void HandleTimeoutException(TimeoutException timeoutException, string duringAction)
        {
            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.TimeoutReached, duringAction, Blog.Name),
                timeoutException);
            ShellService.ShowError(timeoutException, Resources.TimeoutReached, duringAction, Blog.Name);
        }

        protected bool HandleServiceUnavailableWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || !(resp.StatusCode == HttpStatusCode.ServiceUnavailable || resp.StatusCode == HttpStatusCode.Unauthorized))
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.NotLoggedIn, Blog.Name), webException);
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

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.BlogIsOffline, Blog.Name), webException);
            ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
            return true;
        }

        protected bool HandleLimitExceededWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || (int)resp.StatusCode != 429)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.LimitExceeded, Blog.Name), webException);  //TODO: 2nd resource
            ShellService.ShowError(webException, Resources.LimitExceeded, Blog.Name);
            return true;
        }

        protected bool HandleUnauthorizedWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException?.Response;
            if (resp == null || resp.StatusCode != HttpStatusCode.Unauthorized)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.PasswordProtected, Blog.Name),
                webException);
            ShellService.ShowError(webException, Resources.PasswordProtected, Blog.Name);
            return true;
        }
    }
}
