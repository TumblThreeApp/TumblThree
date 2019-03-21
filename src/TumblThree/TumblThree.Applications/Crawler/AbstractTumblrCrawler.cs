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
        protected readonly ITumblrParser tumblrParser;
        protected readonly IImgurParser imgurParser;
        protected readonly IGfycatParser gfycatParser;
        protected readonly IWebmshareParser webmshareParser;
        protected readonly IMixtapeParser mixtapeParser;
        protected readonly IUguuParser uguuParser;
        protected readonly ISafeMoeParser safemoeParser;
        protected readonly ILoliSafeParser lolisafeParser;
        protected readonly ICatBoxParser catboxParser;

        protected AbstractTumblrCrawler(IShellService shellService, ICrawlerService crawlerService, PauseToken pt,
            IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService,
            ITumblrParser tumblrParser, IImgurParser imgurParser, IGfycatParser gfycatParser, IWebmshareParser webmshareParser,
            IMixtapeParser mixtapeParser, IUguuParser uguuParser, ISafeMoeParser safemoeParser, ILoliSafeParser lolisafeParser,
            ICatBoxParser catboxParser, IPostQueue<TumblrPost> postQueue, IBlog blog, CancellationToken ct)
            : base(shellService, crawlerService, progress, webRequestFactory, cookieService, postQueue, blog, pt, ct)
        {
            this.tumblrParser = tumblrParser;
            this.imgurParser = imgurParser;
            this.gfycatParser = gfycatParser;
            this.webmshareParser = webmshareParser;
            this.mixtapeParser = mixtapeParser;
            this.uguuParser = uguuParser;
            this.safemoeParser = safemoeParser;
            this.lolisafeParser = lolisafeParser;
            this.catboxParser = catboxParser;
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
            return ShellService.Settings.ImageSize == "raw" ? "1280" : ShellService.Settings.ImageSize;
        }

        protected string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                   .Replace("_raw", "_" + ImageSize())
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
            foreach (string imageUrl in webmshareParser.SearchForWebmshareUrl(post, Blog.WebmshareType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new VideoPost(imageUrl, webmshareParser.GetWebmshareId(imageUrl),
                    timestamp));
            }
        }

        protected void AddMixtapeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in mixtapeParser.SearchForMixtapeUrl(post, Blog.MixtapeType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, mixtapeParser.GetMixtapeId(imageUrl),
                    timestamp));
            }
        }

        protected void AddUguuUrl(string post, string timestamp)
        {
            foreach (string imageUrl in uguuParser.SearchForUguuUrl(post, Blog.UguuType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, uguuParser.GetUguuId(imageUrl),
                    timestamp));
            }
        }

        protected void AddSafeMoeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in safemoeParser.SearchForSafeMoeUrl(post, Blog.SafeMoeType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, safemoeParser.GetSafeMoeId(imageUrl),
                    timestamp));
            }
        }

        protected void AddLoliSafeUrl(string post, string timestamp)
        {
            foreach (string imageUrl in lolisafeParser.SearchForLoliSafeUrl(post, Blog.LoliSafeType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, lolisafeParser.GetLoliSafeId(imageUrl),
                    timestamp));
            }
        }

        protected void AddCatBoxUrl(string post, string timestamp)
        {
            foreach (string imageUrl in catboxParser.SearchForCatBoxUrl(post, Blog.CatBoxType))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(imageUrl, catboxParser.GetCatBoxId(imageUrl),
                    timestamp));
            }
        }

        protected async Task AddGfycatUrlAsync(string post, string timestamp)
        {
            foreach (string videoUrl in await gfycatParser.SearchForGfycatUrlAsync(post, Blog.GfycatType))
            {
                if (CheckIfSkipGif(videoUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalVideoPost(videoUrl, gfycatParser.GetGfycatId(videoUrl), timestamp));
            }
        }

        protected void AddImgurUrl(string post, string timestamp)
        {
            foreach (string imageUrl in imgurParser.SearchForImgurUrl(post))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurParser.GetImgurId(imageUrl), timestamp));
            }
        }

        protected async Task AddImgurAlbumUrlAsync(string post, string timestamp)
        {
            foreach (string imageUrl in await imgurParser.SearchForImgurUrlFromAlbumAsync(post))
            {
                if (CheckIfSkipGif(imageUrl))
                {
                    continue;
                }

                AddToDownloadList(new ExternalPhotoPost(imageUrl, imgurParser.GetImgurId(imageUrl), timestamp));
            }
        }

        protected void AddTumblrPhotoUrl(string post)
        {
            foreach (string imageUrl in tumblrParser.SearchForTumblrPhotoUrl(post))
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
            foreach (string videoUrl in tumblrParser.SearchForTumblrVideoUrl(post))
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
            foreach (string imageUrl in tumblrParser.SearchForGenericPhotoUrl(post))
            {
                if (tumblrParser.IsTumblrUrl(imageUrl))
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
            foreach (string videoUrl in tumblrParser.SearchForGenericVideoUrl(post))
            {
                if (tumblrParser.IsTumblrUrl(videoUrl))
                {
                    continue;
                }

                AddToDownloadList(new VideoPost(videoUrl, Guid.NewGuid().ToString("N")));
            }
        }
    }
}
