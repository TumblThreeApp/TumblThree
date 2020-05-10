using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.TumblrSearchJson;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrSearchBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrSearchCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private readonly IDownloader downloader;
        private string tumblrKey = string.Empty;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        private int numberOfPagesCrawled;

        public TumblrSearchCrawler(IShellService shellService, ICrawlerService crawlerService, IHttpRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser,
            IPostQueue<TumblrPost> postQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue, blog, progress, pt,
                ct)
        {
            this.downloader = downloader;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrSearchCrawler.Crawl:Start");

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
            tumblrKey = await UpdateTumblrKeyAsync("https://www.tumblr.com/search/" + Blog.Name);

            GenerateTags();

            foreach (int pageNumber in GetPageNumbers())
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(CrawlPageAsync(pageNumber));
            }

            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();

            UpdateBlogStats();
        }

        private async Task CrawlPageAsync(int pageNumber)
        {
            try
            {
                string document = await GetSearchPageAsync(pageNumber);
                await AddUrlsToDownloadListAsync(document, pageNumber);
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch
            {
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async Task<string> GetSearchPageAsync(int pageNumber)
        {
            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }

            return await RequestPostAsync(pageNumber);
        }

        protected virtual async Task<string> RequestPostAsync(int pageNumber)
        {
            string url = "https://www.tumblr.com/search/" + Blog.Name + "/post_page/" + pageNumber;
            string referer = @"https://www.tumblr.com/search/" + Blog.Name;
            var headers = new Dictionary<string, string> { { "X-tumblr-form-key", tumblrKey }, { "DNT", "1" } };
            var request = HttpRequestFactory.PostXhrReqeustMessage(url, referer, headers);
            //CookieService.FillUriCookie(new Uri("https://www.tumblr.com/"));

            //Example request body, searching for cars:
            //q=cars&sort=top&post_view=masonry&blogs_before=8&num_blogs_shown=8&num_posts_shown=20&before=24&blog_page=2&safe_mode=true&post_page=2&filter_nsfw=true&filter_post_type=&next_ad_offset=0&ad_placement_id=0&more_posts=true

            string requestBody = "q=" + Blog.Name + "&sort=top&post_view=masonry&num_posts_shown=" +
                                    ((pageNumber - 1) * Blog.PageSize) + "&before=" + ((pageNumber - 1) * Blog.PageSize) +
                                    "&safe_mode=false&post_page=" + pageNumber +
                                    "&filter_nsfw=false&filter_post_type=&next_ad_offset=0&ad_placement_id=0&more_posts=true";
            var res = await HttpRequestFactory.PostXHRReqeustAsync(request, requestBody);
            return await res.Content.ReadAsStringAsync();
        }

        private async Task AddUrlsToDownloadListAsync(string response, int crawlerNumber)
        {
            while (true)
            {
                if (CheckIfShouldStop())
                {
                    return;
                }

                CheckIfShouldPause();

                var result = ConvertJsonToClass<TumblrSearchJson>(response);
                if (string.IsNullOrEmpty(result.response.posts_html))
                {
                    return;
                }

                try
                {
                    string html = result.response.posts_html;
                    html = Regex.Unescape(html);
                    AddPhotoUrlToDownloadList(html);
                    AddVideoUrlToDownloadList(html);
                }
                catch (NullReferenceException)
                {
                }

                if (!string.IsNullOrEmpty(Blog.DownloadPages))
                {
                    return;
                }

                Interlocked.Increment(ref numberOfPagesCrawled);
                UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                response = await GetSearchPageAsync((crawlerNumber + ShellService.Settings.ConcurrentScans));
                crawlerNumber += ShellService.Settings.ConcurrentScans;
            }
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (!Blog.DownloadPhoto)
            {
                return;
            }

            AddTumblrPhotoUrl(document);

            if (Blog.RegExPhotos)
            {
                AddGenericPhotoUrl(document);
            }
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (!Blog.DownloadVideo)
            {
                return;
            }

            AddTumblrVideoUrl(document);
            AddInlineTumblrVideoUrl(document, TumblrParser.GetTumblrVVideoUrlRegex());

            if (Blog.RegExVideos)
            {
                AddGenericVideoUrl(document);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                semaphoreSlim?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
