using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractTumblrCrawler : AbstractCrawler
    {
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
            if (ShellService.Settings.ImageSize == "raw" || ShellService.Settings.ImageSize == "best")
                return "1280";
            return ShellService.Settings.ImageSize;
        }

        protected string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                   .Replace("_raw", "_" + ImageSize())
                   .Replace("_best", "_" + ImageSize())
                   .Replace("_1280", "_" + ImageSize())
                   .Replace("_540", "_" + ImageSize())
                   .Replace("_500", "_" + ImageSize())
                   .Replace("_400", "_" + ImageSize())
                   .Replace("_250", "_" + ImageSize())
                   .Replace("_100", "_" + ImageSize())
                   .Replace("_75sq", "_" + ImageSize())
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
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new VideoPost(imageUrl, WebmshareParser.GetWebmshareId(imageUrl),
                    timestamp));
            }
        }

        protected void AddMixtapeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in MixtapeParser.SearchForMixtapeUrl(post, Blog.MixtapeType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, MixtapeParser.GetMixtapeId(imageUrl),
                    timestamp));
            }
        }

        protected void AddUguuUrl(string post, string timestamp)
        {
            foreach (string imageUrl in UguuParser.SearchForUguuUrl(post, Blog.UguuType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, UguuParser.GetUguuId(imageUrl),
                    timestamp));
            }
        }

        protected void AddSafeMoeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in SafemoeParser.SearchForSafeMoeUrl(post, Blog.SafeMoeType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, SafemoeParser.GetSafeMoeId(imageUrl),
                    timestamp));
            }
        }

        protected void AddLoliSafeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in LolisafeParser.SearchForLoliSafeUrl(post, Blog.LoliSafeType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, LolisafeParser.GetLoliSafeId(imageUrl),
                    timestamp));
            }
        }

        protected void AddCatBoxUrl(string post, string timestamp)
        {
            foreach (string imageUrl in CatboxParser.SearchForCatBoxUrl(post, Blog.CatBoxType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, CatboxParser.GetCatBoxId(imageUrl),
                    timestamp));
            }
        }

        protected async Task AddGfycatUrlAsync(string post, string timestamp)
        {
            foreach (string videoUrl in await GfycatParser.SearchForGfycatUrlAsync(post, Blog.GfycatType))
            {
                if (CheckIfSkipGif(videoUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(videoUrl, GfycatParser.GetGfycatId(videoUrl), timestamp));
            }
        }

        protected void AddImgurUrl(string post, string timestamp)
        {
            foreach (string imageUrl in ImgurParser.SearchForImgurUrl(post))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalPhotoPost(imageUrl, ImgurParser.GetImgurId(imageUrl), timestamp));
            }
        }

        protected async Task AddImgurAlbumUrlAsync(string post, string timestamp)
        {
            foreach (string imageUrl in await ImgurParser.SearchForImgurUrlFromAlbumAsync(post))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalPhotoPost(imageUrl, ImgurParser.GetImgurId(imageUrl), timestamp));
            }
        }

        protected void AddTumblrPhotoUrl(string post)
        {
            foreach (string imageUrl in TumblrParser.SearchForTumblrPhotoUrl(post))
            {
                string url = imageUrl;
                if (CheckIfSkipGif(url))
                {
                    continue;
                }

                url = ResizeTumblrImageUrl(url);
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
                if (TumblrParser.IsTumblrUrl(imageUrl))
                {
                    continue;
                }

                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new PhotoPost(imageUrl, Guid.NewGuid().ToString("N")));
            }
        }

        protected void AddGenericVideoUrl(string post)
        {
            foreach (string videoUrl in TumblrParser.SearchForGenericVideoUrl(post))
            {
                if (TumblrParser.IsTumblrUrl(videoUrl))
                {
                    continue;
                }

                AddToDownloadList(new VideoPost(videoUrl, Guid.NewGuid().ToString("N")));
            }
        }
    }
}
