using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
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

        protected IHttpRequestFactory HttpRequestFactory { get; }
        protected object LockObjectDb { get; } = new object();
        protected object LockObjectDirectory { get; } = new object();
        protected object LockObjectDownload { get; } = new object();
        protected object LockObjectProgress { get; } = new object();
        protected ICrawlerService CrawlerService { get; }
        protected IShellService ShellService { get; }
        protected PauseToken Pt { get; }
        protected CancellationToken Ct { get; }
        protected IPostQueue<TumblrPost> PostQueue { get; }
        protected ConcurrentBag<TumblrPost> StatisticsBag { get; set; } = new ConcurrentBag<TumblrPost>();
        protected List<string> Tags { get; set; } = new List<string>();

        protected AbstractCrawler(IShellService shellService, ICrawlerService crawlerService, IProgress<DownloadProgress> progress, IHttpRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IPostQueue<TumblrPost> postQueue, IBlog blog,
            PauseToken pt, CancellationToken ct)
        {
            ShellService = shellService;
            CrawlerService = crawlerService;

            //_httpClientHandler = httpClientHandler;
            //_httpClient = httpClient;
            HttpRequestFactory = webRequestFactory;
            CookieService = cookieService;
            PostQueue = postQueue;
            Blog = blog;
            Progress = progress;
            Pt = pt;
            Ct = ct;
        }

        public virtual async Task UpdateMetaInformationAsync()
        {
            await Task.FromResult<object>(null);
        }

        public virtual async Task IsBlogOnlineAsync()
        {
            try
            {
                var res = await HttpRequestFactory.GetReqeust(Blog.Url);
                if (res.IsSuccessStatusCode)
                    Blog.Online = true;
                else
                {
                    Logger.Warning("AbstractCrawler:IsBlogOnlineAsync not success: {0}", res.ReasonPhrase);
                    //ShellService.ShowError(res.ReasonPhrase, Resources.BlogIsOffline, Blog.Name);
                    Blog.Online = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("AbstractCrawler:IsBlogOnlineAsync: Exception {0}", ex);
                ShellService.ShowError(ex, Resources.BlogIsOffline, Blog.Name);
                Blog.Online = false;

                throw;
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
            var res = await HttpRequestFactory.GetReqeust(url, string.Empty, headers);
            /*cookieHosts = cookieHosts ?? new List<string>();
            foreach (string cookieHost in cookieHosts)
            {
                CookieService.FillUriCookie(new Uri(cookieHost));
            }*/

            return await res.Content.ReadAsStringAsync();
        }

        public virtual T ConvertJsonToClass<T>(string json) where T : new()
        {
            try
            {
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer((typeof(T)));
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
            ulong lastId = Blog.LastId;
            if (Blog.ForceRescan)
            {
                return 0;
            }

            return !string.IsNullOrEmpty(Blog.DownloadPages) ? 0 : lastId;
        }

        protected void UpdateBlogStats()
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
            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.TimeoutReached, Blog.Name),
                timeoutException);
            ShellService.ShowError(timeoutException, Resources.TimeoutReached, duringAction, Blog.Name);
        }

        protected bool HandleServiceUnavailableWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException.Response;
            if (!(resp.StatusCode == HttpStatusCode.ServiceUnavailable || resp.StatusCode == HttpStatusCode.Unauthorized))
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.NotLoggedIn, Blog.Name), webException);
            ShellService.ShowError(webException, Resources.NotLoggedIn, Blog.Name);
            return true;
        }

        protected bool HandleNotFoundWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException.Response;
            if (resp.StatusCode != HttpStatusCode.NotFound)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.BlogIsOffline, Blog.Name), webException);
            ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
            return true;
        }

        protected bool HandleLimitExceededWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException.Response;
            if ((int)resp.StatusCode != 429)
            {
                return false;
            }

            Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.LimitExceeded, Blog.Name), webException);
            ShellService.ShowError(webException, Resources.LimitExceeded, Blog.Name);
            return true;
        }

        protected bool HandleUnauthorizedWebException(WebException webException)
        {
            var resp = (HttpWebResponse)webException.Response;
            if (resp.StatusCode != HttpStatusCode.Unauthorized)
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
