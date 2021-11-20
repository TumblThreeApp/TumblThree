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
using System.Dynamic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Linq;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrSearchBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrSearchCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private static readonly Regex extractJsonFromSearch = new Regex("window\\['___INITIAL_STATE___'\\] = (.*);");

        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IPostQueue<CrawlerData<string>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrSearchCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, 
            ITumblrToTextParser<Post> tumblrJsonParser, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IMixtapeParser mixtapeParser, IUguuParser uguuParser,
            ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser, ICatBoxParser catboxParser, IPostQueue<AbstractPost> postQueue,
            IPostQueue<CrawlerData<string>> jsonQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, mixtapeParser, uguuParser, safemoeParser, lolisafeParser, catboxParser, postQueue, blog, downloader, progress, pt,
                ct)
        {
            this.downloader = downloader;
            this.tumblrJsonParser = tumblrJsonParser;
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
            Blog.TotalCount = Blog.TotalCount - Blog.DuplicatePhotos - Blog.DuplicateAudios - Blog.DuplicateVideos;

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
            var completed = await download;

            if (!Ct.IsCancellationRequested && completed)
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
                dynamic result = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
                string nextUrl = result.apiUrl + result.SearchRoute.timelines.post.response.timeline.links.next.href;
                string bearerToken = result.apiFetchStore.API_TOKEN;

                DownloadMedia(result.SearchRoute.timelines.post);
                while (true)
                {
                    if (CheckIfShouldStop()) return;
                    CheckIfShouldPause();

                    document = await GetRequestAsync(nextUrl, bearerToken);
                    dynamic apiresult = JsonConvert.DeserializeObject<ExpandoObject>(document, new ExpandoObjectConverter());
                    DownloadMedia(apiresult);

                    if (!HasProperty(apiresult.response.timeline, "_links") || apiresult.response.timeline._links == null) return;
                    nextUrl = result.apiUrl + apiresult.response.timeline._links.next.href;
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

        private static bool HasProperty(dynamic obj, string name)
        {
            Type objType = obj.GetType();

            if (objType == typeof(ExpandoObject))
            {
                return ((IDictionary<string, object>)obj).ContainsKey(name);
            }

            return objType.GetProperty(name) != null;
        }

        private void DownloadMedia(dynamic page)
        {
            try
            {
                foreach (var post in (IEnumerable<dynamic>)page.response.timeline.elements)
                {
                    try
                    {
                        var objectType = HasProperty(post, "object_type") ? post.object_type : post.objectType;
                        if (objectType != "post" ||
                            !CheckIfWithinTimespan(post.timestamp))
                        {
                            continue;
                        }
                        int index = -1;
                        foreach (var content in (IEnumerable<dynamic>)post.content)
                        {
                            Post data = new Post()
                            {
                                Date = DateTimeOffset.FromUnixTimeSeconds(post.timestamp).DateTime.ToString("yyyyMMddHHmmss"),
                                Type = ConvertContentTypeToPostType(content.type),
                                Id = post.id,
                                Tags = new List<string>(((IEnumerable<object>)post.tags).Select(i => i.ToString())),
                                Slug = post.slug,
                                RegularTitle = post.summary,
                                RebloggedFromName = "",
                                ReblogKey = HasProperty(post, "reblog_key") ? post.reblog_key : post.reblogKey,
                                UnixTimestamp = (int)post.timestamp,
                                Submitter = HasProperty(post, "blog_name") ? post.blog_name : post.blogName,
                                UrlWithSlug = HasProperty(post, "post_url") ? post.post_url : post.postUrl
                            };
                            index += (post.content.Count > 1) ? 1 : 0;
                            DownloadMedia(content, data, index);
                        }
                        string postData = JsonConvert.SerializeObject(post);
                        AddToJsonQueue(new CrawlerData<string>(Path.ChangeExtension(post.id, ".json"), postData));
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
            catch (Exception e)
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

        private void DownloadMedia(dynamic content, Post post, int index)
        {
            string type = content.type;

            string url = HasProperty(content, "media") ? HasProperty(content.media, "Count") ? content.media[0].url : content.media.url : HasProperty(content, "url") ? content.url : "";
            if (url == null)
                return;
            if (CheckIfSkipGif(url))
                return;
            if (type == "text")
            {
                if (Blog.DownloadText)
                {
                    string textBody = tumblrJsonParser.ParseText(post);
                    AddToDownloadList(new TextPost(textBody, post.Id, post.UnixTimestamp.ToString()));
                }
            }
            else if (type == "video")
            {
                if (Blog.DownloadPhoto)
                {
                    var thumbnailUrl = content.poster?[0].url;
                    AddToDownloadList(new PhotoPost(thumbnailUrl, post.Id, post.UnixTimestamp.ToString(), BuildFileName(thumbnailUrl, post, index)));
                }
                // can only download preview image for non-tumblr (embedded) video posts
                if (Blog.DownloadVideo && url.Contains("tumblr.com/"))
                    AddToDownloadList(new VideoPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, index)));
            }
            else if (type == "audio")
            {
                if (Blog.DownloadAudio && url.Contains("tumblr.com/"))
                {
                    AddToDownloadList(new AudioPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, index)));
                }
            }
            else
            {
                if (Blog.DownloadPhoto)
                {
                    url = RetrieveOriginalImageUrl(url, 2000, 3000, false);
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
            headers.Add("sec-ch-ua", "\"Chromium\";v=\"96\", \"Google Chrome\";v=\"96\", \"; Not A Brand\";v=\"99\"");
            headers.Add("sec-ch-ua-mobile", "?0");
            headers.Add("sec-fetch-dest", "document");
            headers.Add("sec-fetch-mode", "navigate");
            headers.Add("sec-fetch-site", "none");
            headers.Add("sec-fetch-user", "?1");
            headers.Add("accept-encoding", "gzip, deflate");
            return await RequestDataAsync(Blog.Url, headers, cookieHosts);
        }

        private void AddToJsonQueue(CrawlerData<string> addToList)
        {
            if (Blog.DumpCrawlerData)
            {
                jsonQueue.Add(addToList);
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
