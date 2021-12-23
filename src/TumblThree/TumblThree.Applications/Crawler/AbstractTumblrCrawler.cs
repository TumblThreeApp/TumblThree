using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblrSvcJson = TumblThree.Applications.DataModels.TumblrSvcJson;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using System.IO;
using TumblThree.Applications.Downloader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TumblThree.Applications.Crawler
{
    public abstract class AbstractTumblrCrawler : AbstractCrawler
    {
        private static readonly Regex extractJsonFromPage = new Regex("window\\['___INITIAL_STATE___'] = ({.*});");

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
            ICatBoxParser catboxParser, IPostQueue<AbstractPost> postQueue, IBlog blog, IDownloader downloader, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, progress, webRequestFactory, cookieService, postQueue, blog, downloader, pt, ct)
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

        protected void AddTumblrPhotoUrl(string text, Post post)
        {
            TumblrPhotoLookup photosToDownload = new TumblrPhotoLookup();

            foreach (string imageUrl in TumblrParser.SearchForTumblrPhotoUrl(text))
            {
                string url = imageUrl;
                if (CheckIfSkipGif(url)) { continue; }

                url = RetrieveOriginalImageUrl(url, 2000, 3000, true);

                var matchesNewFormat = Regex.Match(url, "media.tumblr.com/([A-Za-z0-9_/:.-]*)/s([0-9]*)x([0-9]*)");
                if (matchesNewFormat.Success)
                {
                    string id = matchesNewFormat.Groups[1].Value;
                    int width = int.Parse(matchesNewFormat.Groups[2].Value);
                    int height = int.Parse(matchesNewFormat.Groups[3].Value);
                    int resolution = width * height;

                    photosToDownload.AddOrReplace(id, url, resolution);
                }
                else
                {
                    url = ResizeTumblrImageUrl(url);
                    AddPhotoToDownloadList(url, post);
                }

            }

            foreach(string url in photosToDownload.GetUrls())
            {
                AddPhotoToDownloadList(url, post);
            }
        }

        protected void AddPhotoToDownloadList(string url, Post post)
        {
            AddToDownloadList(new PhotoPost(url, post.Id, post.UnixTimestamp.ToString(), BuildFileName(url, post, -1)));
        }

        protected void AddTumblrVideoUrl(string text, Post post)
        {
            foreach (string videoUrl in TumblrParser.SearchForTumblrVideoUrl(text))
            {
                string url = videoUrl;
                if (ShellService.Settings.VideoSize == 480)
                {
                    url += "_480";
                }

                AddToDownloadList(new VideoPost("https://vtt.tumblr.com/" + url + ".mp4", post.Id, post.UnixTimestamp.ToString(), BuildFileName("https://vtt.tumblr.com/" + url + ".mp4", post, -1)));
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

                AddToDownloadList(new VideoPost(videoUrl + ".mp4", Guid.NewGuid().ToString("N"), FileName(videoUrl + ".mp4")));
            }
        }

        protected void AddInlineTumblrVideoUrl(string text, Post post)
        {
            foreach (string videoUrl in TumblrParser.SearchForTumblrInlineVideoUrl(text))
            {
                string url = videoUrl;
                if (ShellService.Settings.VideoSize == 480)
                {
                    url += "_480";
                }

                AddToDownloadList(new VideoPost(url + ".mp4", post.Id, post.UnixTimestamp.ToString(), BuildFileName(url + ".mp4", post, -1)));
            }
        }

        protected void AddGenericPhotoUrl(string text, Post post)
        {
            foreach (string imageUrl in TumblrParser.SearchForGenericPhotoUrl(text))
            {
                if (TumblrParser.IsTumblrUrl(imageUrl)) { continue; }
                if (CheckIfSkipGif(imageUrl)) { continue; }

                AddToDownloadList(new PhotoPost(imageUrl, post.Id, post.UnixTimestamp.ToString(), FileName(imageUrl)));
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

        protected static Post ConvertTumblrApiJson(DataModels.TumblrSvcJson.Post p)
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
                ReblogKey = p.ReblogKey,
                Tumblelog = new TumbleLog2() { Name = p.Tumblelog}
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
                || (width <= 1280 && height <= 1920)) { return url; }

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
                    HttpWebRequest request = WebRequestFactory.CreateGetRequest(url, "",
                        new Dictionary<string, string>() { { "Accept-Language", "en-US" }, { "Accept-Encoding", "gzip, deflate" } }, false);
                    request.Accept = "text/html, application/xhtml+xml, */*";
                    request.UserAgent = ShellService.Settings.UserAgent;
                    request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                    pageContent = WebRequestFactory.ReadRequestToEndAsync(request).GetAwaiter().GetResult();
                    errCnt = 9;
                }
                catch (WebException we)
                {
                    if (we.Response != null && ((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.NotFound)
                        return url;
                }
                catch (Exception e)
                {
                    errCnt++;
                    Logger.Error("AbstractTumblrCrawler:RetrieveOriginalImageUrl: {0}", e);
                    lastError = e;
                    if (errCnt < 3) Thread.Sleep(errCnt * 10000);
                }
            } while (errCnt < 3);
            if (errCnt == 3)
            {
                ShellService.ShowError(lastError, Resources.PostNotParsable, Blog.Name);
                throw new NullReferenceException("RetrieveOriginalImageUrl download", lastError);
            }
            try
            {
                var extracted = extractJsonFromPage.Match(pageContent).Groups[1].Value;
                extracted = new Regex("/.*/").Replace(extracted, "\"\"");
                ImageResponse imgRsp = DeserializeImageResponse(extracted);
                int maxWidth = imgRsp.Images.Max(x => x.Width);
                Image img = imgRsp.Images.FirstOrDefault(x => x.Width == maxWidth);
                return string.IsNullOrEmpty(img?.MediaKey) ? url : img.Url;
            }
            catch (Exception ex)
            {
                Logger.Error("AbstractTumblrCrawler:RetrieveOriginalImageUrl: {0}", ex);
                ShellService.ShowError(ex, Resources.PostNotParsable, Blog.Name);
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

        protected static string FileName(string url)
        {
            return url.Split('/').Last();
        }

        private static string Sanitize(string filename)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return String.Join("-", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        protected string BuildFileName(string url, Post post, int index)
        {
            if (post == null)
            {
                post = new Post() { Date = DateTime.MinValue.ToString("R"), Type = "", Id = "", Tags = new List<string>(),
                    Slug = "", RegularTitle = "", RebloggedFromName = "", ReblogKey = "", Tumblelog = new TumbleLog2() { Name = "" } };
            }
            return BuildFileNameCore(url, post.Tumblelog.Name, post.DateGmt, post.UnixTimestamp, index, post.Type, post.Id, post.Tags, post.Slug, post.RegularTitle, post.RebloggedFromName, post.ReblogKey);
        }

        protected string BuildFileName(string url, TumblrSvcJson.Post post, int index)
        {
            if (post == null)
            {
                post = new TumblrSvcJson.Post() { Date = DateTime.MinValue.ToString("R"), Type = "", Id = "", Tags = new List<string>(),
                    Slug = "", Title = "", RebloggedFromName = "", ReblogKey = "", Tumblelog = "" };
            }
            return BuildFileNameCore(url, post.Tumblelog, post.Date, post.Timestamp, index, post.Type, post.Id, post.Tags, post.Slug, post.Title, post.RebloggedFromName, post.ReblogKey);
        }

        private static string ReplaceCI(string input, string search, string replacement)
        {
            string result = Regex.Replace(
                input,
                Regex.Escape(search),
                replacement.Replace("$", "$$"),
                RegexOptions.IgnoreCase
            );
            return result;
        }

        private static bool ContainsCI(string input, string search)
        {
            return input.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        private string BuildFileNameCore(string url, string blogName, string date, int timestamp, int index, string type, string id, List<string> tags, string slug, string title, string rebloggedFromName, string reblog_key)
        {
            /*
             * Replaced are:
             *  %f  original filename (default)
                %b  blog name
                %d  post date (yyyyMMddHHmmss)
                %u  post timestamp (number)
                %p  post title (shorted if needed…)
                %i  post id
                %n  image index (of photo sets)
                %t  for all tags (cute+cats,big+dogs)
                %r  for reblog ("" / "reblog")
                %s  slug (last part of a post's url)
                %k  reblog-key
               Tokens to make filenames unique:
                %x  "_{number}" ({number}: 2..n)
                %y  " ({number})" ({number}: 2..n)
             */
            string filename = Blog.FilenameTemplate;

            url = url.IndexOf('?') > 0 ? url.Substring(0, url.IndexOf('?')) : url;

            filename += Path.GetExtension(FileName(url));
            if (ContainsCI(filename, "%f")) filename = ReplaceCI(filename, "%f", Path.GetFileNameWithoutExtension(FileName(url)));
            if (ContainsCI(filename, "%d")) filename = ReplaceCI(filename, "%d", DateTime.Parse(date).ToString("yyyyMMdd"));
            if (ContainsCI(filename, "%u")) filename = ReplaceCI(filename, "%u", timestamp.ToString());
            if (ContainsCI(filename, "%b")) filename = ReplaceCI(filename, "%b", blogName);
            if (ContainsCI(filename, "%i"))
            {
                if (type == "photo" && Blog.GroupPhotoSets && index != -1) id = $"{id}_{index}";
                filename = ReplaceCI(filename, "%i", id);
            }
            else if (type == "photo" && Blog.GroupPhotoSets && index != -1)
            {
                filename = $"{id}_{index}_{filename}";
            }
            if (ContainsCI(filename, "%n"))
            {
                if (type != "photo" || index == -1)
                {
                    string charBefore = "";
                    string charAfter = "";
                    if (filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) > 0)
                        charBefore = filename.Substring(filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) - 1, 1);
                    if (filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) < filename.Length - 2)
                        charAfter = filename.Substring(filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) + 2, 1);
                    if (charBefore == charAfter)
                        filename = filename.Remove(filename.IndexOf("%n", StringComparison.OrdinalIgnoreCase) - 1, 1);
                    filename = ReplaceCI(filename, "%n", "");
                }
                else
                {
                    filename = ReplaceCI(filename, "%n", index.ToString());
                }
            }
            if (ContainsCI(filename, "%t")) filename = ReplaceCI(filename, "%t", string.Join(",", tags).Replace(" ", "+"));
            if (ContainsCI(filename, "%r"))
            {
                if (rebloggedFromName.Length == 0 && filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) > 0 &&
                    filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) < filename.Length - 2 &&
                    filename.Substring(filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) - 1, 1) == filename.Substring(filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase) + 2, 1))
                {
                    filename = filename.Remove(filename.IndexOf("%r", StringComparison.OrdinalIgnoreCase), 3);
                }
                filename = ReplaceCI(filename, "%r", (rebloggedFromName.Length == 0 ? "" : "reblog"));
            }
            if (ContainsCI(filename, "%s")) filename = ReplaceCI(filename, "%s", slug);
            if (ContainsCI(filename, "%k")) filename = ReplaceCI(filename, "%k", reblog_key);
            int neededChars = 0;
            if (ContainsCI(filename, "%x"))
            {
                neededChars = 6;
                Downloader.AppendTemplate = "_<0>";
                filename = ReplaceCI(filename, "%x", "");
            }
            if (ContainsCI(filename, "%y"))
            {
                neededChars = 8;
                Downloader.AppendTemplate = " (<0>)";
                filename = ReplaceCI(filename, "%y", "");
            }
            if (ContainsCI(filename, "%p"))
            {
                string _title = title;
                if (!ShellService.IsLongPathSupported)
                {
                    string filepath = Path.Combine(Blog.DownloadLocation(), filename);
                    // 260 (max path minus NULL) - current filename length + 2 chars (%p) - chars for numbering
                    int charactersLeft = 259 - filepath.Length + 2 - neededChars;
                    if (charactersLeft < 0) throw new PathTooLongException($"{Blog.Name}: filename for post id {id} is too long");
                    if (charactersLeft < _title.Length) _title = _title.Substring(0, charactersLeft - 1) + "…";
                }
                filename = ReplaceCI(filename, "%p", _title);
            }
            else if (!ShellService.IsLongPathSupported)
            {
                string filepath = Path.Combine(Blog.DownloadLocation(), filename);
                // 260 (max path minus NULL) - current filename length - chars for numbering
                int charactersLeft = 259 - filepath.Length - neededChars;
                if (charactersLeft < 0) throw new PathTooLongException($"{Blog.Name}: filename for post id {id} is too long");
            }

            return Sanitize(filename);
        }
    }
}
