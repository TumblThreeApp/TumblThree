using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
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
using System.Web;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrSearchBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrSearchCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private static readonly Regex extractJsonFromSearch = new Regex("window\\['___INITIAL_STATE___'\\] = (.*);");

        private readonly IShellService shellService;
        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IPostQueue<CrawlerData<string>> jsonQueue;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        public TumblrSearchCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, 
            ITumblrToTextParser<Post> tumblrJsonParser, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IUguuParser uguuParser, ICatBoxParser catboxParser, IPostQueue<AbstractPost> postQueue,
            IPostQueue<CrawlerData<string>> jsonQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, uguuParser, catboxParser, postQueue, blog, downloader, crawlerDataDownloader, progress, pt,
                ct)
        {
            this.shellService = shellService;
            this.downloader = downloader;
            this.tumblrJsonParser = tumblrJsonParser;
            this.jsonQueue = jsonQueue;
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
                string nextUrl = "";
                string bearerToken = "";
                if (!HasProperty(result.SearchRoute, "timelines"))
                {
                    if (result.SearchRoute.searchApiResponse.meta.status != 200)
                    {
                        Logger.Error(Resources.ErrorDownloadingBlog, Blog.Name, (string)result.SearchRoute.searchApiResponse.meta.msg, (long)result.SearchRoute.searchApiResponse.meta.status);
                        shellService.ShowError(new Exception(), string.Format(Resources.ErrorDownloadingBlog, Blog.Name, (string)result.SearchRoute.searchApiResponse.meta.msg, (long)result.SearchRoute.searchApiResponse.meta.status));
                        return;
                    }
                    if (!HasProperty(result.SearchRoute.searchApiResponse.response.posts, "links"))
                    {
                        Logger.Error(Resources.SearchTermNotFound, Blog.Url.Replace("/recent", "").Split('/').Last());
                        shellService.ShowError(new Exception(), Resources.SearchTermNotFound, Blog.Url.Replace("/recent", "").Split('/').Last());
                        return;
                    }

                    nextUrl = result.apiUrl + result.SearchRoute.searchApiResponse.response.posts.links.next.href;
                    bearerToken = result.apiFetchStore.API_TOKEN;

                    DownloadPage(result.SearchRoute.searchApiResponse);
                }
                else
                {
                    if (result.SearchRoute.timelines.post.meta.status != 200)
                    {
                        Logger.Error(Resources.ErrorDownloadingBlog, Blog.Name, (string)result.SearchRoute.timelines.post.meta.msg, (long)result.SearchRoute.timelines.post.meta.status);
                        shellService.ShowError(new Exception(), string.Format(Resources.ErrorDownloadingBlog, Blog.Name, (string)result.SearchRoute.timelines.post.meta.msg, (long)result.SearchRoute.timelines.post.meta.status));
                        return;
                    }
                    if (!HasProperty(result.SearchRoute.timelines.post.response.timeline, "links"))
                    {
                        Logger.Error(Resources.SearchTermNotFound, (string)result.SearchRoute.searchParams.searchTerm);
                        shellService.ShowError(new Exception(), Resources.SearchTermNotFound, (string)result.SearchRoute.searchParams.searchTerm);
                        return;
                    }

                    nextUrl = result.apiUrl + result.SearchRoute.timelines.post.response.timeline.links.next.href;
                    bearerToken = result.apiFetchStore.API_TOKEN;

                    DownloadPage(result.SearchRoute.timelines.post);
                }
                while (true)
                {
                    if (CheckIfShouldStop()) return;
                    CheckIfShouldPause();

                    document = await GetRequestAsync(nextUrl, bearerToken);
                    dynamic apiresult = JsonConvert.DeserializeObject<ExpandoObject>(document, new ExpandoObjectConverter());
                    DownloadPage(apiresult);

                    if (!HasProperty(apiresult.response, "timeline"))
                    {
                        if (!HasProperty(apiresult.response.posts, "_links") || apiresult.response.posts._links == null) return;
                        nextUrl = result.apiUrl + apiresult.response.posts._links.next.href;
                    }
                    else
                    {
                        if (!HasProperty(apiresult.response.timeline, "_links") || apiresult.response.timeline._links == null) return;
                        nextUrl = result.apiUrl + apiresult.response.timeline._links.next.href;
                    }
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

        private static string GetValue(dynamic obj, string name)
        {
            Type objType = obj.GetType();

            if (objType == typeof(ExpandoObject))
            {
                var dic = ((IDictionary<string, object>)obj);
                return dic.ContainsKey(name) ? (string)dic[name] : "";
            }

            var prop = objType.GetProperty(name);
            return prop != null ? (string)prop.GetValue(obj) : "";
        }

        private void DownloadPage(dynamic page)
        {
            try
            {
                dynamic list;
                if (!HasProperty(page.response, "timeline"))
                    list = page.response.posts.data;
                else
                    list = page.response.timeline.elements;
                foreach (var post in (IEnumerable<dynamic>)list)
                {
                    if (CheckIfShouldStop()) return;
                    CheckIfShouldPause();
                    try
                    {
                        var objectType = HasProperty(post, "object_type") ? post.object_type : post.objectType;
                        if (objectType != "post" ||
                            !CheckIfWithinTimespan(post.timestamp))
                        {
                            continue;
                        }
                        try
                        {
                            Post data = null;
                            var countImagesVideos = CountImagesAndVideos((IEnumerable<dynamic>)post.content);
                            int index = -1;
                            foreach (var content in (IEnumerable<dynamic>)post.content)
                            {
                                data = new Post()
                                {
                                    Date = DateTimeOffset.FromUnixTimeSeconds(post.timestamp).DateTime.ToString("R"),
                                    DateGmt = DateTimeOffset.FromUnixTimeSeconds(post.timestamp).DateTime.ToString("R"),
                                    Type = ConvertContentTypeToPostType(content.type),
                                    Id = post.id,
                                    Tags = new List<string>(((IEnumerable<object>)post.tags).Select(i => i.ToString())),
                                    Slug = post.slug,
                                    RegularTitle = post.summary,
                                    RebloggedFromName = "",
                                    RebloggedRootName = "",
                                    ReblogKey = HasProperty(post, "reblog_key") ? post.reblog_key : post.reblogKey,
                                    UnixTimestamp = (int)post.timestamp,
                                    Tumblelog = new TumbleLog2() { Name = HasProperty(post, "blog_name") ? post.blog_name : post.blogName },
                                    UrlWithSlug = HasProperty(post, "post_url") ? post.post_url : post.postUrl
                                };
                                index += (countImagesVideos > 1) ? 1 : 0;
                                DownloadMedia(content, data, index);
                                AddInlinePhotoUrl(post, content, data);
                                AddInlineVideoUrl(post, content, data);
                            }
                            DownloadText(post, data);
                            string postData = JsonConvert.SerializeObject(post);
                            AddToJsonQueue(new CrawlerData<string>(Path.ChangeExtension(post.id, ".json"), postData));
                        }
                        catch (NullReferenceException e)
                        {
                            Logger.Verbose("TumblrSearchCrawler.DownloadPage: {0}", e);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TumblrSearchCrawler.DownloadMedia: {0}", ex);
                        ShellService.ShowError(ex, "{0}: Error parsing post!", Blog.Name);
                    }
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (Exception e)
            {
                Logger.Error("TumblrSearchCrawler.DownloadPage: {0}", e);
            }
        }

        private static string InlineSearch(dynamic post, dynamic content)
        {
            string text = GetValue(post, "summary") + " ";

            if (content.type == "video")
            {
                text += HttpUtility.UrlDecode(GetValue(content, "embedHtml"));
            }
            else if (content.type == "text")
            {
                text += content.text;
            }

            return text;
        }

        private void AddInlinePhotoUrl(dynamic post, dynamic content, Post data)
        {
            if (!Blog.DownloadPhoto) return;

            string text = InlineSearch(post, content);

            AddTumblrPhotoUrl(text, data);

            if (Blog.RegExPhotos)
            {
                AddGenericPhotoUrl(text, data);
            }
        }

        private void AddInlineVideoUrl(dynamic post, dynamic content, Post data)
        {
            if (!Blog.DownloadVideo) return;

            string text = InlineSearch(post, content);

            AddTumblrVideoUrl(text, data);

            if (Blog.RegExVideos)
            {
                AddGenericVideoUrl(text, data);
            }
        }

        private static int CountImagesAndVideos(IEnumerable<dynamic> list)
        {
            var count = 0;
            foreach (var content in list)
            {
                count += (content.type == "image" || content.type == "video") ? 1 : 0;
            }
            return count;
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

        private void DownloadText(dynamic dynPost, Post post)
        {
            if (Blog.DownloadText && (GetValue(dynPost, "originalType") == "regular" || GetValue(dynPost, "original_type") == "regular"))
            {
                string text = "";
                foreach (var content in (IEnumerable<dynamic>)dynPost.content)
                {
                    if (content.type == "text")
                    {
                        text += content.text + Environment.NewLine;
                    }
                }
                post.RegularBody = text;
                string textBody = tumblrJsonParser.ParseText(post);
                AddToDownloadList(new TextPost(textBody, post.Id, post.UnixTimestamp.ToString()));
            }
        }

        private void DownloadMedia(dynamic content, Post post, int index)
        {
            string type = content.type;

            string url = HasProperty(content, "media") ? HasProperty(content.media, "Count") ? content.media[0].url : content.media.url : GetValue(content, "url");
            if (url == null)
                return;
            if (CheckIfSkipGif(url))
                return;
            if (type == "video")
            {
                if (Blog.DownloadPhoto)
                {
                    if (GetValue(content, "provider") == "tumblr" || url.Contains("tumblr.com") || Blog.RegExPhotos)
                    {
                        string thumbnailUrl = HasProperty(content, "poster") ? HasProperty(content.poster, "Count") ? content.poster[0].url : "" : "";
                        AddToDownloadList(new PhotoPost(thumbnailUrl, post.Id, post.UnixTimestamp.ToString(), BuildFileName(thumbnailUrl, post, index)));
                    }
                }
                // can only download preview image for non-tumblr (embedded) video posts
                if (Blog.DownloadVideo && content.provider == "tumblr")
                    AddToDownloadList(new VideoPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, index)));
            }
            else if (type == "audio")
            {
                if (Blog.DownloadAudio && content.provider == "tumblr")
                {
                    url = url.IndexOf("?") > -1 ? url.Substring(0, url.IndexOf("?")) : url;
                    AddToDownloadList(new AudioPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, index)));
                }
            }
            else if (type == "image")
            {
                if (Blog.DownloadPhoto)
                {
                    if (url.Contains("tumblr.com/"))
                    {
                        url = RetrieveOriginalImageUrl(url, 2000, 3000, false);
                    }
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
