using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractTumblrCrawler : AbstractCrawler
    {
        private static readonly Regex extractJsonFromPage = new Regex("window\\['___INITIAL_STATE___'] = .*({\"imageResponse\":\\[[^]]*]})");

        public ITumblrParser TumblrParser { get; }

        public IImgurParser ImgurParser { get; }

        public IGfycatParser GfycatParser { get; }

        public IWebmshareParser WebmshareParser { get; }

        public IMixtapeParser MixtapeParser { get; }

        public IUguuParser UguuParser { get; }

        public ISafeMoeParser SafemoeParser { get; }

        public ILoliSafeParser LolisafeParser { get; }

        public ICatBoxParser CatboxParser { get; }

        protected AbstractTumblrCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService,
            ITumblrParser tumblrParser, IImgurParser imgurParser, IGfycatParser gfycatParser, IWebmshareParser webmshareParser,
            IMixtapeParser mixtapeParser, IUguuParser uguuParser, ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser,
            ICatBoxParser catboxParser, IPostQueue<TumblrPost> postQueue, IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, progress, webRequestFactory, cookieService, postQueue, blog, pt, ct)
        {
            this.TumblrParser = tumblrParser;
            this.ImgurParser = imgurParser;
            this.GfycatParser = gfycatParser;
            this.WebmshareParser = webmshareParser;
            this.MixtapeParser = mixtapeParser;
            this.UguuParser = uguuParser;
            this.SafemoeParser = safemoeParser;
            this.LolisafeParser = lolisafeParser;
            this.CatboxParser = catboxParser;
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

        protected void GenerateTags()
        {
            if (!string.IsNullOrWhiteSpace(Blog.Tags))
            {
                Tags = Blog.Tags.Split(',').Select(x => x.Trim()).ToList();
            }
        }

        protected bool CheckIfSkipGif(string imageUrl)
        {
            return Blog.SkipGif && imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv");
        }

        protected void AddWebmshareUrl(string post, string timestamp)
        {
            foreach (string imageUrl in WebmshareParser.SearchForWebmshareUrl(post, Blog.WebmshareType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new VideoPost(imageUrl, WebmshareParser.GetWebmshareId(imageUrl),
                    timestamp));
            }
        }

        protected void AddMixtapeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in MixtapeParser.SearchForMixtapeUrl(post, Blog.MixtapeType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(imageUrl, MixtapeParser.GetMixtapeId(imageUrl), timestamp));
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

        protected void AddSafeMoeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in SafemoeParser.SearchForSafeMoeUrl(post, Blog.SafeMoeType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(imageUrl, SafemoeParser.GetSafeMoeId(imageUrl), timestamp));
            }
        }

        protected void AddLoliSafeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in LolisafeParser.SearchForLoliSafeUrl(post, Blog.LoliSafeType))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new ExternalVideoPost(imageUrl, LolisafeParser.GetLoliSafeId(imageUrl), timestamp));
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

        protected void AddTumblrPhotoUrl(string post)
        {
            foreach (string imageUrl in TumblrParser.SearchForTumblrPhotoUrl(post))
            {
                string url = imageUrl;
                if (CheckIfSkipGif(url)) { continue; }

                url = ResizeTumblrImageUrl(url);
                url = RetrieveOriginalImageUrl(url, 2000, 3000);
                // TODO: postID
                AddToDownloadList(new PhotoPost(url, Guid.NewGuid().ToString("N")));
            }
        }

        protected void AddTumblrVideoUrl(string post)
        {
            foreach (string videoUrl in TumblrParser.SearchForTumblrVideoUrl(post))
            {
                string url = videoUrl;
                if (ShellService.Settings.VideoSize == 480)
                {
                    url += "_480";
                }

                AddToDownloadList(new VideoPost("https://vtt.tumblr.com/" + url + ".mp4", Guid.NewGuid().ToString("N")));
            }
        }

        protected void AddInlineTumblrVideoUrl(string post, Regex regex)
        {
            foreach (Match match in regex.Matches(post))
            {
                string videoUrl = match.Groups[1].Value;

                if (ShellService.Settings.VideoSize == 480)
                {
                    videoUrl += "_480";
                }

                AddToDownloadList(new VideoPost(videoUrl + ".mp4", Guid.NewGuid().ToString("N")));
            }
        }

        protected void AddGenericPhotoUrl(string post)
        {
            foreach (string imageUrl in TumblrParser.SearchForGenericPhotoUrl(post))
            {
                if (TumblrParser.IsTumblrUrl(imageUrl)) { continue; }
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new PhotoPost(imageUrl, Guid.NewGuid().ToString("N")));
            }
        }

        protected void AddGenericVideoUrl(string post)
        {
            foreach (string videoUrl in TumblrParser.SearchForGenericVideoUrl(post))
            {
                if (TumblrParser.IsTumblrUrl(videoUrl)) { continue; }

                AddToDownloadList(new VideoPost(videoUrl, Guid.NewGuid().ToString("N")));
            }
        }

        protected string RetrieveOriginalImageUrl(string url, int width, int height)
        {
            if (width > height) { (width, height) = (height, width); }
            if (ShellService.Settings.ImageSize != "best"
                || !url.Contains("/s1280x1920/")
                || (width <= 1280 && height <= 1920)) { return url; }

            url = url.Replace("/s1280x1920/", (width <= 2048 && height <= 3072) ? "/s2048x3072/" : "/s99999x99999/");
            string pageContent = "";
            try
            {
                HttpWebRequest request = WebRequestFactory.CreateGetRequest(url, "",
                    new Dictionary<string, string>() { { "Accept-Language", "en-US" }, { "Accept-Encoding", "gzip, deflate" } }, false);
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.UserAgent = ShellService.Settings.UserAgent;
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                pageContent = WebRequestFactory.ReadRequestToEndAsync(request).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Logger.Error("TumblrBlogCrawler:RetrieveRawImageUrl:Exception {0}", e);
                return url;
            }
            pageContent = extractJsonFromPage.Match(pageContent).Groups[1].Value;
            ImageResponse imgRsp = ConvertJsonToClass<ImageResponse>(pageContent);
            Image img = imgRsp.Images.FirstOrDefault(x => x.HasOriginalDimensions = true);

            return string.IsNullOrEmpty(img.MediaKey) ? url + Environment.NewLine : img.Url + Environment.NewLine;
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

        protected async Task<ulong> GetHighestPostIdApiCoreAsync()
        {
            var document = await GetApiPageWithRetryAsync(0, 1);
            var response = ConvertJsonToClass<TumblrApiJson>(document);

            Post post = response.Posts?.FirstOrDefault();
            Blog.Posts = response.PostsTotal;
            if (DateTime.TryParse(post?.DateGmt, out var latestPost)) Blog.LatestPost = latestPost;
            _ = ulong.TryParse(post?.Id, out var highestPostId);
            return highestPostId;
        }

        public override T ConvertJsonToClass<T>(string json)
        {
            if (json.Contains("tumblr_api_read"))
            {
                int jsonStart = json.IndexOf("{", StringComparison.Ordinal);
                json = json.Substring(jsonStart);
                json = json.Remove(json.Length - 2);
            }

            return base.ConvertJsonToClass<T>(json);
        }

        protected async Task<string> GetApiPageWithRetryAsync(int pageId, int count)
        {
            string page;
            var attemptCount = 0;

            do
            {
                page = await GetApiPageAsync(pageId, count);
                attemptCount++;
            }
            while (string.IsNullOrEmpty(page) && (attemptCount < ShellService.Settings.MaxNumberOfRetries));

            return page;
        }

        private async Task<string> GetApiPageAsync(int pageId, int count)
        {
            string url = GetApiUrl(Blog.Url, (count == 0 ? 1 : count), pageId * count);

            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }

            return await GetRequestAsync(url);
        }

        private static string GetApiUrl(string url, int count, int start = 0)
        {
            if (url.Last() != '/')
            {
                url += "/";
            }

            url += "api/read/json?debug=1&";

            var parameters = new Dictionary<string, string>
            {
                { "num", count.ToString() }
            };
            if (start > 0)
            {
                parameters["start"] = start.ToString();
            }

            return url + UrlEncode(parameters);
        }
    }
}
