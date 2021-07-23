using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.Converter;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.DataModels.CrawlerData;
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
        private static readonly Regex extractJsonFromSearch = new Regex("window\\['___INITIAL_STATE___'\\] = (.*);");

        private readonly IDownloader downloader;
        private readonly IPostQueue<CrawlerData<Datum>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrSearchCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser, IPostQueue<AbstractPost> postQueue,
            IPostQueue<CrawlerData<Datum>> jsonQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue, blog, downloader, progress, pt,
                ct)
        {
            this.downloader = downloader;
            this.jsonQueue = jsonQueue;
            this.crawlerDataDownloader = crawlerDataDownloader;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrSearchCrawler.Crawl:Start");

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
                string document = await GetSearchPageAsync();
                string json = extractJsonFromSearch.Match(document).Groups[1].Value;
                SearchJson result = ConvertJsonToClassNew<SearchJson>(json);
                string nextUrl = result.ApiUrl + result.SearchRoute.SearchApiResponse.Response.Posts.Links.Next.Href;
                string bearerToken = result.ApiFetchStore.APITOKEN;

                DownloadMedia(result);
                while (true)
                {
                    if (CheckIfShouldStop()) return;
                    CheckIfShouldPause();

                    document = await GetRequestAsync(nextUrl, bearerToken);
                    TumblrSearchApi apiresult = ConvertJsonToClassNew<TumblrSearchApi>(document);
                    DownloadMedia(apiresult);

                    if (apiresult.Response.Posts.Links == null) return;
                    nextUrl = result.ApiUrl + apiresult.Response.Posts.Links.Next.Href;
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (Exception e)
            {
                Logger.Error("TumblrSearchCrawler.CrawlPageAsync: {0}", e);
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

        private void DownloadMedia(TumblrSearchApi page)
        {
            try
            {
                foreach (var post in page.Response.Posts.Data)
                {
                    try
                    {
                        if (!CheckIfWithinTimespan(post.Timestamp))
                        {
                            continue;
                        }
                        int index = -1;
                        foreach (var content in post.Content)
                        {
                            Post data = new Post()
                            {
                                Date = DateTimeOffset.FromUnixTimeSeconds(post.Timestamp).DateTime.ToString("yyyyMMddHHmmss"),
                                Type = ConvertContentTypeToPostType(content.Type),
                                Id = post.Id,
                                Tags = new List<string>(post.Tags),
                                Slug = post.Slug,
                                RegularTitle = post.Summary,
                                RebloggedFromName = "",
                                ReblogKey = post.ReblogKey,
                                UnixTimestamp = post.Timestamp,
                                Submitter = post.BlogName
                            };
                            index += (post.Content.Count > 1) ? 1 : 0;
                            DownloadMedia(content, data, index);
                        }
                        AddToJsonQueue(new CrawlerData<Datum>(Path.ChangeExtension(post.Id, ".json"), post));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TumblrSearchCrawler.DownloadMedia: {0}", ex);
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

        private void DownloadMedia(SearchJson page)
        {
            try
            {
                foreach (var post in page.SearchRoute.SearchApiResponse.Response.Posts.Data)
                {
                    if (post.ObjectType != "post") continue;
                    if (!CheckIfWithinTimespan(post.Timestamp)) continue;
                    int index = -1;
                    foreach (var content in post.Content)
                    {
                        Post data = new Post()
                        {
                            Date = DateTimeOffset.FromUnixTimeSeconds(post.Timestamp).DateTime.ToString("yyyyMMddHHmmss"),
                            Type = ConvertContentTypeToPostType(content.Type),
                            Id = post.Id,
                            Tags = new List<string>(post.Tags),
                            Slug = post.Slug,
                            RegularTitle = post.Summary,
                            RebloggedFromName = "",
                            ReblogKey = post.ReblogKey,
                            UnixTimestamp = post.Timestamp,
                            Submitter = post.BlogName
                        };
                        index += (post.Content.Count > 1) ? 1 : 0;
                        DownloadMedia(content, data, index);
                    }
                    AddToJsonQueue(new CrawlerData<DataModels.TumblrSearchJson.Data>(Path.ChangeExtension(post.Id, ".json"), post));
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

        private void DownloadMedia(Content content, Post post, int index)
        {
            string type = content.Type;
            string url = string.Empty;
            url = type == "video" || type == "audio" ? content.Url : content.Media?[0].Url;
            if (url == null)
                return;
            if (CheckIfSkipGif(url))
                return;
            if (type == "video")
            {
                if (content.Provider != "tumblr") return;
                if (Blog.DownloadPhoto)
                {
                    var thumbnailUrl = content.Poster?[0].Url;
                    AddToDownloadList(new PhotoPost(thumbnailUrl, post.Id, post.UnixTimestamp.ToString(), BuildFileName(thumbnailUrl, post, index)));
                }
                if (Blog.DownloadVideo)
                    AddToDownloadList(new VideoPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, index)));
            }
            else if (type == "audio")
            {
                if (Blog.DownloadAudio)
                    AddToDownloadList(new AudioPost(url, post.Id, post.UnixTimestamp.ToString()));
            }
            else
            {
                if (Blog.DownloadPhoto)
                {
                    url = RetrieveOriginalImageUrl(url, 2000, 3000);
                    AddToDownloadList(new PhotoPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, index)));
                }
            }
        }

        private async Task<string> GetSearchPageAsync()
        {
            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }

            string[] cookieHosts = { "https://www.tumblr.com/" };
            var headers = new Dictionary<string, string>();
            headers.Add("sec-ch-ua", "\"Chromium\";v=\"88\", \"Google Chrome\";v=\"88\", \"; Not A Brand\";v=\"99\"");
            headers.Add("sec-ch-ua-mobile", "?0");
            headers.Add("sec-fetch-dest", "document");
            headers.Add("sec-fetch-mode", "navigate");
            headers.Add("sec-fetch-site", "none");
            headers.Add("sec-fetch-user", "?1");
            headers.Add("accept-encoding", "gzip, deflate");
            return await RequestDataAsync(Blog.Url, headers, cookieHosts);
        }

        private void AddToJsonQueue(CrawlerData<DataModels.TumblrSearchJson.Data> addToList)
        {
            if (Blog.DumpCrawlerData)
            {
                var datum = new Datum();
                PropertyCopier<DataModels.TumblrSearchJson.Data, Datum>.Copy(addToList.Data, datum);
                jsonQueue.Add(new CrawlerData<Datum>(addToList.Filename, datum));
            }
        }

        private void AddToJsonQueue(CrawlerData<Datum> addToList)
        {
            if (Blog.DumpCrawlerData)
                jsonQueue.Add(addToList);
        }

        /*
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
                if (string.IsNullOrEmpty(result.Response.PostsHtml))
                {
                    return;
                }

                try
                {
                    string html = result.Response.PostsHtml;
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
                response = await GetSearchPageAsync(crawlerNumber + ShellService.Settings.ConcurrentScans);
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
        */

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
