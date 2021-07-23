using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrLikedByBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrLikedByCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private readonly IDownloader downloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        private int numberOfPagesCrawled;

        public TumblrLikedByCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser,
            IPostQueue<AbstractPost> postQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue, blog, downloader,
                progress, pt, ct)
        {
            this.downloader = downloader;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrLikedByCrawler.Crawl:Start");

            Task grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            Blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            Blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            Blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            Blog.TotalCount = (Blog.TotalCount - Blog.DuplicatePhotos - Blog.DuplicateAudios - Blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await download;

            if (!Ct.IsCancellationRequested)
            {
                Blog.LastCompleteCrawl = DateTime.Now;
            }

            Blog.Save();

            UpdateProgressQueueInformation(string.Empty);
        }

        private async Task GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);
            trackedTasks = new List<Task>();

            if (!await CheckIfLoggedInAsync())
            {
                Logger.Error("TumblrLikedByCrawler:GetUrlsAsync: {0}", "User not logged in");
                ShellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, Blog.Name);
                PostQueue.CompleteAdding();
                return;
            }

            long pagination = CreateStartPagination();

            // TODO: find way to parallelize without losing content.
            foreach (int crawlerNumber in Enumerable.Range(0, 1))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(CrawlPageAsync(pagination, crawlerNumber));
            }

            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();

            UpdateBlogStats(true);
        }

        private async Task CrawlPageAsync(long pagination, int crawlerNumber)
        {
            try
            {
                await AddUrlsToDownloadListAsync(pagination, crawlerNumber);
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (Exception e)
            {
                Logger.Verbose("TumblrLikedByCrawler:CrawlPageAsync: {0}", e.ToString());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                await GetRequestAsync(Blog.Url);
                Blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                Logger.Error("TumblrLikedByCrawler:IsBlogOnlineAsync:WebException {0}", webException);
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

        private long CreateStartPagination()
        {
            if (string.IsNullOrEmpty(Blog.DownloadTo))
            {
                return DateTimeOffset.Now.ToUnixTimeSeconds();
            }

            DateTime downloadTo = DateTime.ParseExact(Blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            var dateTimeOffset = new DateTimeOffset(downloadTo);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        private bool CheckIfPageCountReached(int pageCount)
        {
            int numberOfPages = RangeToSequence(Blog.DownloadPages).Count();
            return pageCount >= numberOfPages;
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetRequestAsync(Blog.Url + "/page/1");
                return !document.Contains("<div class=\"signup_view account login\"");
            }
            catch (WebException webException) when (webException.Status == WebExceptionStatus.RequestCanceled)
            {
                return true;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return false;
            }
        }

        private async Task AddUrlsToDownloadListAsync(long pagination, int crawlerNumber)
        {
            long prevPagination = long.MaxValue;
            
            while (true)
            {
                if (CheckIfShouldStop())
                {
                    return;
                }

                CheckIfShouldPause();

                string document;

                if (!TumblrLikedByBlog.IsLikesUrl(Blog.Url))
                {
                    document = Regex.Unescape(await GetRequestAsync(Blog.Url + "/page/" + crawlerNumber + "/" + pagination));
                }
                else
                {
                    if (pagination >= prevPagination) return;
                    prevPagination = pagination;

                    document = Regex.Unescape(await GetRequestAsync(Blog.Url + "?before=" + pagination));
                }

                if (document.Length == 0)
                {
                    throw new Exception("TumblrLikedByCrawler:AddUrlsToDownloadListAsync: empty document");
                }
                if (document.Contains("<div class=\"no_posts_found\""))
                {
                    return;
                }

                try
                {
                    AddPhotoUrlToDownloadList(document);
                    AddVideoUrlToDownloadList(document);
                }
                catch (NullReferenceException e)
                {
                    System.Diagnostics.Debug.WriteLine($"TumblrLikedByCrawler.AddUrlsToDownloadListAsync(): {e}");
                }

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                pagination = ExtractNextPageLink(document);
                crawlerNumber++;
                if (!CheckIfWithinTimespan(pagination))
                {
                    return;
                }
            }
        }

        private static long ExtractNextPageLink(string document)
        {
            // Example pagination:
            //
            // <div id="pagination" class="pagination "><a id="previous_page_link" href="/liked/by/wallpaperfx/page/3/-1457140452" class="previous button chrome">Previous</a>
            // <a id="next_page_link" href="/liked/by/wallpaperfx/page/5/1457139681" class="next button chrome blue">Next</a></div></div>

            const string htmlPagination = "(id=\"next_page_link\" href=\"[A-Za-z0-9_/:.-]+/([0-9]+)/([A-Za-z0-9]+))\"";
            const string jsonPagination = "&before=([0-9]*)";

            long.TryParse(Regex.Match(document, htmlPagination).Groups[3].Value, out var unixTime);
            
            if(unixTime == 0)
            {
                var r = Regex.Match(document, jsonPagination);
                long.TryParse(r.Groups[1].Value, out unixTime);
            }

            return unixTime;
        }

        private bool CheckIfWithinTimespan(long pagination)
        {
            if (string.IsNullOrEmpty(Blog.DownloadFrom))
            {
                return true;
            }

            DateTime downloadFrom = DateTime.ParseExact(Blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            var dateTimeOffset = new DateTimeOffset(downloadFrom);
            return pagination >= dateTimeOffset.ToUnixTimeSeconds();
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (!Blog.DownloadPhoto)
            {
                return;
            }

            AddTumblrPhotoUrl(document, new DataModels.TumblrApiJson.Post());

            if (Blog.RegExPhotos)
            {
                AddGenericPhotoUrl(document, null);
            }
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (!Blog.DownloadVideo)
            {
                return;
            }

            AddTumblrVideoUrl(document, null);
            AddInlineTumblrVideoUrl(document, TumblrParser.GetTumblrVVideoUrlRegex());

            if (Blog.RegExVideos)
            {
                AddGenericVideoUrl(document, null);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                semaphoreSlim?.Dispose();
                downloader.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
