using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.TumblrTaggedSearchJson;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrTagSearchBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrTagSearchCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private static readonly Regex extractJsonFromSearch = new Regex("window\\['___INITIAL_STATE___'\\] = (.*);");

        private readonly IDownloader downloader;
        private readonly IPostQueue<TumblrCrawlerData<Datum>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        private int numberOfPagesCrawled;

        public TumblrTagSearchCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser,
            IPostQueue<TumblrPost> postQueue, IPostQueue<TumblrCrawlerData<Datum>> jsonQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue, blog, progress, pt, ct)
        {
            this.downloader = downloader;
            this.jsonQueue = jsonQueue;
            this.crawlerDataDownloader = crawlerDataDownloader;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrTagSearchCrawler.Crawl:Start");

            Task grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (Blog.DumpCrawlerData)
            {
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();
            }

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            Blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            Blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            Blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            Blog.TotalCount = (Blog.TotalCount - Blog.DuplicatePhotos - Blog.DuplicateAudios - Blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
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
                Logger.Error("TumblrTagSearchCrawler:GetUrlsAsync: {0}", "User not logged in");
                ShellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, Blog.Name);
                PostQueue.CompleteAdding();
                return;
            }

            GenerateTags();

            await semaphoreSlim.WaitAsync();
            trackedTasks.Add(CrawlPageAsync());
            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats(true);
        }

        private async Task CrawlPageAsync()
        {
            try
            {
                string document = await GetTaggedSearchPageAsync();
                string json = extractJsonFromSearch.Match(document).Groups[1].Value;
                TagSearch result = ConvertJsonToClass<TagSearch>(json);
                string nextUrl = result.ApiUrl + result.Tagged.Timeline.Links.Next.Href;
                string bearerToken = result.ApiFetchStore.APITOKEN;

                DownloadMedia(result);
                while (true)
                {
                    if (CheckIfShouldStop())
                    {
                        return;
                    }

                    CheckIfShouldPause();

                    document = await GetRequestAsync(nextUrl, bearerToken);
                    TumblrTaggedSearchApi apiresult = ConvertJsonToClass<TumblrTaggedSearchApi>(document);
                    nextUrl = result.ApiUrl + apiresult.Response.Timeline.Links.Next.Href;

                    DownloadMedia(apiresult);
                }
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

        protected async Task<string> GetRequestAsync(string url, string bearerToken)
        {
            if (ShellService.Settings.LimitConnectionsSearchApi)
            {
                CrawlerService.TimeconstraintSearchApi.Acquire();
            }
            string[] cookieHosts = { "https://www.tumblr.com/" };
            return await RequestApiDataAsync(url, bearerToken, null, cookieHosts);
        }

        private void DownloadMedia(TumblrTaggedSearchApi page)
        {
            try
            {
                foreach (var post in page.Response.Timeline.Elements)
                {
                    if (!CheckIfWithinTimespan(post.Timestamp))
                    {
                        continue;
                    }
                    foreach (var content in post.Content)
                    {
                        DownloadMedia(content, post.Id, post.Timestamp, post.Tags);
                    }
                    AddToJsonQueue(new TumblrCrawlerData<Datum>(Path.ChangeExtension(post.Id, ".json"), post));
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch
            {
            }
        }

        private void DownloadMedia(TagSearch page)
        {
            try
            {
                foreach (var data in page.Tagged.Timeline.Elements)
                {
                    if (data.ObjectType != "Post") continue;
                    if (!CheckIfWithinTimespan(data.Timestamp))
                    {
                        continue;
                    }
                    foreach (var content in data.Content)
                    {
                        DownloadMedia(content, data.Id, data.Timestamp, data.Tags);
                    }
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch
            {
            }
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

        private void DownloadMedia(Content content, String id, long timestamp, IList<string> tags)
        {
            string type = content.Type;
            string url = string.Empty;
            if (type == "video")
                url = content.Url;
            else
                url = content.Media?[0].Url;
            if (url == null)
                return;
            if (!CheckIfContainsTaggedPost(tags))
            {
                return;
            }
            if (CheckIfSkipGif(url))
            {
                return;
            }
            if (type == "video")
            {
                if (Blog.DownloadVideo)
                    AddToDownloadList(new VideoPost(url, id, timestamp.ToString()));
            }
            else
            {
                if (Blog.DownloadPhoto)
                {
                    url = RetrieveOriginalImageUrl(url, 2000, 3000);
                    AddToDownloadList(new PhotoPost(url, id, timestamp.ToString()));
                }
            }
        }

        private bool CheckIfContainsTaggedPost(IList<string> tags)
        {
            return !Tags.Any() || tags.Any(x => Tags.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        private void AddToJsonQueue(TumblrCrawlerData<Datum> addToList)
        {
            if (Blog.DumpCrawlerData)
                jsonQueue.Add(addToList);
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetTaggedSearchPageAsync();
                return document.Contains("API_TOKEN");
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

        private async Task<string> GetTaggedSearchPageAsync()
        {
            if (ShellService.Settings.LimitConnectionsSearchApi)
            {
                CrawlerService.TimeconstraintSearchApi.Acquire();
            }

            return await GetRequestAsync("https://www.tumblr.com/tagged/" + Blog.Name);
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
