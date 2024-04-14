using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblrSvcJson = TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Applications.Downloader;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractTumblrCrawler : AbstractCrawler
    {
        private static readonly Regex extractJsonFromPage = new Regex("window\\['___INITIAL_STATE___'] = ({.*});");
        private static readonly Regex extractJsonFromPage2 = new Regex("id=\"___INITIAL_STATE___\">\\s*?({.*})\\s*?</script>", RegexOptions.Singleline);
        private static readonly Regex extractImageLink = new Regex("<img class=\"\\w+?\" src=\"([^\"]+?)\" alt=\"[^\"]+?\"/>");
        private static readonly Regex extractImageSize = new Regex("/s(\\d+?)x(\\d+?)[^/]*?/");

        protected readonly ICrawlerDataDownloader crawlerDataDownloader;
        protected readonly IEnvironmentService environmentService;
        protected readonly ILoginService loginService;

        public ITumblrParser TumblrParser { get; }

        public IImgurParser ImgurParser { get; }

        public IGfycatParser GfycatParser { get; }

        public IWebmshareParser WebmshareParser { get; }

        public IUguuParser UguuParser { get; }

        public ICatBoxParser CatboxParser { get; }

        protected AbstractTumblrCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService,
            ITumblrParser tumblrParser, IImgurParser imgurParser, IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IUguuParser uguuParser,
            ICatBoxParser catboxParser, IPostQueue<AbstractPost> postQueue, IBlog blog, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader,
            IProgress<DownloadProgress> progress, IEnvironmentService environmentService, ILoginService loginService, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, progress, webRequestFactory, cookieService, postQueue, blog, downloader, pt, ct)
        {
            this.crawlerDataDownloader = crawlerDataDownloader;
            this.crawlerDataDownloader?.ChangeCancellationToken(Ct);
            this.TumblrParser = tumblrParser;
            this.ImgurParser = imgurParser;
            this.GfycatParser = gfycatParser;
            this.WebmshareParser = webmshareParser;
            this.UguuParser = uguuParser;
            this.CatboxParser = catboxParser;
            this.environmentService = environmentService;
            this.loginService = loginService;
        }

        protected async Task<string> GetRequestAsync(string url)
        {
            var headers = new Dictionary<string, string>();
            string username = Blog.Name + ".tumblr.com";
            string password = Blog.Password;
            string encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            headers.Add("Authorization", "Basic " + encoded);
            string[] cookieHosts = { "https://www.tumblr.com/" };
            return await RequestDataAsync(url, headers, cookieHosts);
        }

        protected async Task<string> UpdateTumblrKeyAsync(string url)
        {
            try
            {
                AcquireTimeconstraintSvc();
                string document = await GetRequestAsync(url);
                return ExtractTumblrKey(document);
            }
            catch (WebException webException) when (webException.Status == WebExceptionStatus.RequestCanceled)
            {
                return string.Empty;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                return string.Empty;
            }
        }

        protected static string ExtractTumblrKey(string document)
        {
            return Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;
        }

        protected string ImageSize()
        {
            return ShellService.Settings.ImageSize;
        }

        protected string ImageSizeForSearching()
        {
            if (ShellService.Settings.ImageSize == "raw" || ShellService.Settings.ImageSize == "best")
                return "1280";
            return ShellService.Settings.ImageSize;
        }

        protected string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                   .Replace("_raw", "_" + ImageSizeForSearching())
                   .Replace("_best", "_" + ImageSizeForSearching())
                   .Replace("_1280", "_" + ImageSizeForSearching())
                   .Replace("_540", "_" + ImageSizeForSearching())
                   .Replace("_500", "_" + ImageSizeForSearching())
                   .Replace("_400", "_" + ImageSizeForSearching())
                   .Replace("_250", "_" + ImageSizeForSearching())
                   .Replace("_100", "_" + ImageSizeForSearching())
                   .Replace("_75sq", "_" + ImageSizeForSearching())
                   .ToString();
        }

        protected bool CheckIfSkipGif(string imageUrl)
        {
            return Blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv"));
        }

        protected void AddWebmshareUrl(string post, string timestamp)
        {
            foreach (string imageUrl in WebmshareParser.SearchForWebmshareUrl(post, Blog.WebmshareType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(imageUrl, WebmshareParser.GetWebmshareId(imageUrl), timestamp));
            }
        }

        protected void AddUguuUrl(string post, string timestamp)
        {
            foreach (string imageUrl in UguuParser.SearchForUguuUrl(post, Blog.UguuType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(imageUrl, UguuParser.GetUguuId(imageUrl), timestamp));
            }
        }

        protected void AddCatBoxUrl(string post, string timestamp)
        {
            foreach (string imageUrl in CatboxParser.SearchForCatBoxUrl(post, Blog.CatBoxType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(imageUrl, CatboxParser.GetCatBoxId(imageUrl), timestamp));
            }
        }

        protected async Task AddGfycatUrlAsync(string post, string timestamp)
        {
            foreach (string videoUrl in await GfycatParser.SearchForGfycatUrlAsync(post, Blog.GfycatType))
            {
                if (CheckIfSkipGif(videoUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(videoUrl, GfycatParser.GetGfycatId(videoUrl), timestamp));
            }
        }

        protected void AddImgurUrl(string post, string timestamp)
        {
            foreach (string imageUrl in ImgurParser.SearchForImgurUrl(post))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalPhotoPost(imageUrl, ImgurParser.GetImgurId(imageUrl), timestamp));
            }
        }

        protected async Task AddImgurAlbumUrlAsync(string post, string timestamp)
        {
            foreach (string imageUrl in await ImgurParser.SearchForImgurUrlFromAlbumAsync(post))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalPhotoPost(imageUrl, ImgurParser.GetImgurId(imageUrl), timestamp));
            }
        }

        protected void AddTumblrPhotoUrl(string text, Post post)
        {
            TumblrPhotoLookup photosToDownload = new TumblrPhotoLookup();

            foreach (string imageUrl in TumblrParser.SearchForTumblrPhotoUrl(text))
            {
                if (CheckIfShouldStop()) { return; }
                CheckIfShouldPause();

                string url = imageUrl;
                if (CheckIfSkipGif(url)) { continue; }
                if (!Blog.DownloadVideoThumbnail && (url.Contains("_frame1.") || url.Contains("_smart1."))) { continue; }

                var matchesNewFormat = Regex.Match(url, "media.tumblr.com/([A-Za-z0-9_/:.-]*)/s([0-9]*)x([0-9]*)");
                if (matchesNewFormat.Success)
                {
                    var postedUrl = url;
                    if (!Downloader.CheckIfPostedUrlIsDownloaded(url))
                        url = RetrieveOriginalImageUrl(url, 2000, 3000, true);
                    url = CheckPnjUrl(url);
                    matchesNewFormat = Regex.Match(url, "media.tumblr.com/([A-Za-z0-9_/:.-]*)/s([0-9]*)x([0-9]*)");
                    string id = matchesNewFormat.Groups[1].Value;
                    int width = int.Parse(matchesNewFormat.Groups[2].Value);
                    int height = int.Parse(matchesNewFormat.Groups[3].Value);
                    int resolution = width * height;

                    photosToDownload.AddOrReplace(id, url, postedUrl, resolution);
                }
                else
                {
                    url = ResizeTumblrImageUrl(url);
                    var postedUrl = url;
                    if (!Downloader.CheckIfPostedUrlIsDownloaded(url))
                        url = RetrieveOriginalImageUrl(url, 2000, 3000, true);
                    url = CheckPnjUrl(url);
                    AddPhotoToDownloadList(url, postedUrl, post);
                }
            }

            foreach((string, string) urls in photosToDownload.GetUrls())
            {
                AddPhotoToDownloadList(urls.Item1, urls.Item2, post);
            }
        }

        protected string CheckPnjUrl(string url)
        {
            if (url.EndsWith(".pnj", StringComparison.OrdinalIgnoreCase) &&
                Blog.PnjDownloadFormat == nameof(PnjDownloadType.png))
            {
                url = url.Substring(0, url.Length - 3) + "png";
            }
            return url;
        }

        protected void AddPhotoToDownloadList(string url, string postedUrl, Post post)
        {
            var filename = BuildFileName(url, post, -1);
            AddDownloadedMedia(url, filename, post);
            AddToDownloadList(new PhotoPost(url, postedUrl, post.Id, post.UnixTimestamp.ToString(), filename));
        }

        protected string[] AddTumblrVideoUrl(string text, Post post)
        {
            if (!Blog.DownloadVideo) return Array.Empty<string>();

            var list = new List<string>();

            foreach (string videoUrl in TumblrParser.SearchForTumblrVideoUrl(text))
            {
                string url = videoUrl;
                if (ShellService.Settings.VideoSize == 480)
                {
                    url += "_480";
                }

                url = "https://vtt.tumblr.com/" + url + ".mp4";
                var filename = BuildFileName(url, post, -1);
                AddDownloadedMedia(url, filename, post);
                AddToDownloadList(new VideoPost(url, post.Id, post.UnixTimestamp.ToString(), filename));
                list.Add(url);
            }

            return list.ToArray();
        }

        protected void AddInlineTumblrVideoUrl(string post, Regex regexVideo, Regex regexThumbnail)
        {
            if (Blog.DownloadVideo)
            {
                foreach (Match match in regexVideo.Matches(post))
                {
                    string videoUrl = match.Groups[1].Value;

                    if (ShellService.Settings.VideoSize == 480)
                    {
                        videoUrl += "_480";
                    }

                    AddToDownloadList(new VideoPost(videoUrl + ".mp4", Guid.NewGuid().ToString("N"), FileName(videoUrl + ".mp4")));
                }
            }

            if (Blog.DownloadVideoThumbnail)
            {
                foreach (Match match in regexThumbnail.Matches(post))
                {
                    string thumbnailUrl = match.Groups[1].Value;
                    AddToDownloadList(new VideoPost(thumbnailUrl, Guid.NewGuid().ToString("N"), FileName(thumbnailUrl)));
                }
            }
        }

        protected string[] AddInlineTumblrVideoUrl(string text, Post post)
        {
            var list = new List<string>();

            if (Blog.DownloadVideo)
            {
                foreach (string videoUrl in TumblrParser.SearchForTumblrInlineVideoUrl(text))
                {
                    string url = videoUrl;
                    if (ShellService.Settings.VideoSize == 480)
                    {
                        url += "_480";
                    }

                    url += ".mp4";
                    var filename = BuildFileName(url, post, -1);
                    AddDownloadedMedia(url, filename, post);
                    AddToDownloadList(new VideoPost(url, post.Id, post.UnixTimestamp.ToString(), filename));
                    list.Add(url);
                }
            }

            if (Blog.DownloadVideoThumbnail)
            {
                foreach (string thumbnailUrl in TumblrParser.SearchForTumblrVideoThumbnailUrl(text))
                {
                    var filename = BuildFileName(thumbnailUrl, post, "photo", -1);
                    AddDownloadedMedia(thumbnailUrl, filename, post);
                    AddToDownloadList(new PhotoPost(thumbnailUrl, "", post.Id, post.UnixTimestamp.ToString(), filename));
                }
            }

            return list.ToArray();
        }

        protected void AddGenericPhotoUrl(string text, Post post)
        {
            foreach (string imageUrl in TumblrParser.SearchForGenericPhotoUrl(text))
            {
                if (TumblrParser.IsTumblrUrl(imageUrl)) { continue; }
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new PhotoPost(imageUrl, "", post.Id, post.UnixTimestamp.ToString(), FileName(imageUrl)));
            }
        }

        protected void AddGenericVideoUrl(string text, Post post)
        {
            foreach (string videoUrl in TumblrParser.SearchForGenericVideoUrl(text))
            {
                if (TumblrParser.IsTumblrUrl(videoUrl)) { continue; }

                AddToDownloadList(new VideoPost(videoUrl, post.Id, post.UnixTimestamp.ToString(), FileName(videoUrl)));
            }
        }

        protected static Post ConvertTumblrApiJson(TumblrSvcJson.Post p)
        {
            return new Post()
            {
                DateGmt = p.Date,
                UnixTimestamp = p.Timestamp,
                Type = p.Type,
                Id = p.Id,
                Tags = new List<string>(p.Tags),
                Slug = p.Slug,
                RegularTitle = p.Title,
                RebloggedFromName = p.RebloggedFromName,
                RebloggedRootName = p.RebloggedRootName,
                ReblogKey = p.ReblogKey,
                Tumblelog = new TumbleLog2() { Name = p.Tumblelog },
                // deep copies here prevent inline media from being listed at the end
                DownloadedFilenames = p.DownloadedFilenames,  //?.Select(s => string.Copy(s)).ToList(),
                DownloadedUrls = p.DownloadedUrls  //?.Select(s => string.Copy(s)).ToList()
            };
        }

        private static ImageResponse DeserializeImageResponse(string s)
        {
            var list = new List<Image>();
            JObject o = JObject.Parse(s);
            if (o["ImageUrlPage"]["photo"]["imageResponse"].Type is JTokenType.Array)
            {
                for (int i = 0; i < o["ImageUrlPage"]["photo"]["imageResponse"].Count(); i++)
                {
                    if (o["ImageUrlPage"]["photo"]["imageResponse"][i] != null)
                        list.Add(JsonConvert.DeserializeObject<Image>(o["ImageUrlPage"]["photo"]["imageResponse"][i].ToString()));
                }
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    if (o["ImageUrlPage"]["photo"]["imageResponse"][i.ToString()] != null)
                        list.Add(JsonConvert.DeserializeObject<Image>(o["ImageUrlPage"]["photo"]["imageResponse"][i.ToString()].ToString()));
                }
            }
            return new ImageResponse() { Images = list };
        }

        protected string RetrieveOriginalImageUrl(string url, int width, int height, bool isInline)
        {
            if (width > height) { (width, height) = (height, width); }
            if (ShellService.Settings.ImageSize != "best"
                || !isInline && !url.Contains("/s1280x1920/")
                || (width <= 1280 && height <= 1920)
                || isInline && !new Regex(@"\/s[\d]{2,4}x[\d]{2,4}\/").IsMatch(url)) { return url; }

            if (isInline)
            {
                var re = new Regex(@"\/s[\d]{2,4}x[\d]{2,4}\/");
                url = re.Replace(url, "/s2048x3072/");
            }
            else
            {
                url = url.Replace("/s1280x1920/", (width <= 2048 && height <= 3072) ? "/s2048x3072/" : "/s99999x99999/");
            }
            string pageContent = "";
            int errCnt = 0;
            Exception lastError = null;
            do
            {
                try
                {
                    Thread.Sleep(700);
                    HttpWebRequest request = WebRequestFactory.CreateGetRequest(url, "",
                        new Dictionary<string, string>() { { "Accept-Language", "en-US" }, { "Accept-Encoding", "gzip, deflate" } }, false);
                    request.Accept = "text/html, application/xhtml+xml, */*";
                    request.UserAgent = ShellService.Settings.UserAgent;
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    using (Ct.Register(() => request.Abort()))
                    {
                        pageContent = WebRequestFactory.ReadRequestToEndAsync(request).GetAwaiter().GetResult();
                    }
                    errCnt = 9;
                }
                catch (WebException we)
                {
                    errCnt++;
                    if (we.Status == WebExceptionStatus.RequestCanceled)
                        throw new NullReferenceException("RetrieveOriginalImageUrl request cancelled");
                    if (we.Response != null && ((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.NotFound)
                        return url;
                    lastError = we;
                    Logger.Verbose("AbstractTumblrCrawler:RetrieveOriginalImageUrl, WebExcetion ({0}): {1}", errCnt, we);
                    if (errCnt < 3) Thread.Sleep(errCnt * 10000);
                }
                catch (Exception e)
                {
                    errCnt++;
                    Logger.Error("AbstractTumblrCrawler:RetrieveOriginalImageUrl ({0}): {1}", errCnt, e);
                    lastError = e;
                    if (errCnt < 3) Thread.Sleep(errCnt * 10000);
                }
            } while (errCnt < 3);
            if (errCnt == 3)
            {
                if ((lastError is WebException we) && we.Response != null && (int)((HttpWebResponse)we.Response).StatusCode == 429)
                {
                    throw new LimitExceededWebException(lastError);
                }
                ShellService.ShowError(lastError, Resources.ImageSizeNotRetrievable, Blog.Name, ShellService.Settings.GetCollection(Blog.CollectionId).Name);
                throw new NullReferenceException("RetrieveOriginalImageUrl download", lastError);
            }
            try
            {
                var extracted = extractJsonFromPage.Match(pageContent).Groups[1].Value;
                extracted = new Regex("/.*/").Replace(extracted, "\"\"");
                if (string.IsNullOrEmpty(extracted))
                {
                    extracted = extractJsonFromPage2.Match(pageContent).Groups[1].Value;
                }
                ImageResponse imgRsp = DeserializeImageResponse(extracted);
                int maxWidth = imgRsp.Images.Max(x => x.Width);
                Image img = imgRsp.Images.FirstOrDefault(x => x.Width == maxWidth);
                if (string.IsNullOrEmpty(img?.MediaKey))
                {
                    return url;
                }
                else
                {
                    var matchSizesImgUrl = extractImageSize.Match(img.Url);
                    var sizesImgUrl = (int.Parse(matchSizesImgUrl.Groups[1].Value), int.Parse(matchSizesImgUrl.Groups[2].Value));

                    if (sizesImgUrl.Item1 >= 2048) { return img.Url; }

                    var parsedImgLink = extractImageLink.Match(pageContent).Groups[1].Value;
                    var matchSizesParsedImgLink = extractImageSize.Match(parsedImgLink);
                    var sizesParsedImgLink = (int.Parse(matchSizesParsedImgLink.Groups[1].Value), int.Parse(matchSizesParsedImgLink.Groups[2].Value));

                    return sizesParsedImgLink.Item1 > sizesImgUrl.Item1 ? parsedImgLink : img.Url;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("AbstractTumblrCrawler:RetrieveOriginalImageUrl: {0}", ex);
                ShellService.ShowError(ex, Resources.ImageSizeNotRetrievable, Blog.Name, Blog.Name, ShellService.Settings.GetCollection(Blog.CollectionId).Name);
                throw new NullReferenceException("RetrieveOriginalImageUrl parsing", ex);
            }
        }

        protected void UpdateBlogDuplicates()
        {
            if (GetLastPostId() == 0)
            {
                Blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
                Blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
                Blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
                Blog.TotalCount = Blog.TotalCount - Blog.DuplicatePhotos - Blog.DuplicateAudios - Blog.DuplicateVideos;
            }
            else
            {
                var dupPhoto = DetermineDuplicates<PhotoPost>();
                var dupVideo = DetermineDuplicates<VideoPost>();
                var dupAudio = DetermineDuplicates<AudioPost>();
                Blog.DuplicatePhotos += dupPhoto;
                Blog.DuplicateVideos += dupVideo;
                Blog.DuplicateAudios += dupAudio;
                Blog.TotalCount = Blog.TotalCount - dupPhoto - dupVideo - dupAudio;
            }
        }

        protected static string ConvertContentTypeToPostType(string contentType)
        {
            if (string.Equals(contentType, "image", StringComparison.CurrentCultureIgnoreCase))
                return "photo";
            return contentType?.ToLower() ?? "";
        }

        protected string BuildFileName(string url, Post post, string type, int index)
        {
            if (post == null)
            {
                post = new Post() { Date = DateTime.MinValue.ToString("R"), DateGmt = DateTime.MinValue.ToString("R"), Type = "", Id = "", Tags = new List<string>(),
                    Slug = "", RegularTitle = "", RebloggedFromName = "", RebloggedRootName = "", ReblogKey = "", Tumblelog = new TumbleLog2() { Name = "" } };
            }
            return BuildFileNameCore(url, post.Tumblelog.Name, GetDate(post), post.UnixTimestamp, index, type, post.Id, post.Tags, post.Slug, post.RegularTitle, post.RebloggedFromName, post.RebloggedRootName, post.ReblogKey);
        }

        protected string BuildFileName(string url, Post post, int index)
        {
            var type = post?.Type ?? "";
            return BuildFileName(url, post, type, index);
        }

        protected string BuildFileName(string url, TumblrSvcJson.Post post, string type, int index)
        {
            if (post == null)
            {
                post = new TumblrSvcJson.Post() { Date = DateTime.MinValue.ToString("R"), Type = "", Id = "", Tags = new List<string>(),
                    Slug = "", Title = "", RebloggedFromName = "", RebloggedRootName = "", ReblogKey = "", Tumblelog = "" };
            }
            return BuildFileNameCore(url, post.Tumblelog, GetDate(post), post.Timestamp, index, type, post.Id, post.Tags, post.Slug, post.Title, post.RebloggedFromName, post.RebloggedRootName, post.ReblogKey);
        }

        protected string BuildFileName(string url, TumblrSvcJson.Post post, int index)
        {
            var type = post?.Type ?? "";
            return BuildFileName(url, post, type, index);
        }

        protected static void AddDownloadedMedia(string url, string filename, Post post)
        {
            if (post == null) throw new ArgumentNullException(nameof(post));
            post.DownloadedFilenames.Add(filename);
            post.DownloadedUrls.Add(url);
        }

        protected static void AddDownloadedMedia(string url, string filename, TumblrSvcJson.Post post)
        {
            if (post == null) throw new ArgumentNullException(nameof(post));
            post.DownloadedFilenames.Add(filename);
            post.DownloadedUrls.Add(url);
        }

        protected async Task<bool> FetchCookiesAgainAsync()
        {
            var appSettingsPath = Path.GetFullPath(Path.Combine(environmentService.AppSettingsPath, ".."));
            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, appSettingsPath);
            using (WebView2 browser = new WebView2())
            {
                await browser.EnsureCoreWebView2Async(env);
                var cookieManager = browser.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.tumblr.com/");
                CookieCollection cookieCollection = GetCookies(cookies);
                loginService.AddCookies(cookieCollection);
            }
            Logger.Warning("Reloaded Tumblr cookies");
            ShellService.ShowError(null, "Warning: Reloaded Tumblr cookies");
            return true;
        }

        private static CookieCollection GetCookies(List<CoreWebView2Cookie> cookies)
        {
            CookieCollection cookieCollection = new CookieCollection();
            foreach (var cookie in cookies)
            {
                var transferCookie = new System.Net.Cookie(cookie.Name, WebUtility.UrlEncode(cookie.Value), cookie.Path, cookie.Domain);
                transferCookie.Expires = cookie.Expires;
                transferCookie.HttpOnly = cookie.IsHttpOnly;
                transferCookie.Secure = cookie.IsSecure;
                cookieCollection.Add(transferCookie);
            }
            return cookieCollection;
        }

        protected void AcquireTimeconstraintApi()
        {
            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }
        }

        protected void AcquireTimeconstraintSvc()
        {
            if (ShellService.Settings.LimitConnectionsSvc)
            {
                CrawlerService.TimeconstraintSvc.Acquire();
            }
        }

        protected void AcquireTimeconstraintSearchApi()
        {
            if (ShellService.Settings.LimitConnectionsSearchApi)
            {
                CrawlerService.TimeconstraintSearchApi.Acquire();
            }
        }

        private static DateTime GetDate(Post post)
        {
            return DateTime.Parse(post.DateGmt);
        }

        private static DateTime GetDate(TumblrSvcJson.Post post)
        {
            return DateTime.Parse(post.Date);
        }
    }
}
