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
using System.Web;

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

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrBlogCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IPostQueue<CrawlerData<Post>> jsonQueue;

        private bool completeGrab = true;
        private bool incompleteCrawl = false;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        private int numberOfPagesCrawled;

        public TumblrBlogCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader,
            ITumblrToTextParser<Post> tumblrJsonParser, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IUguuParser uguuParser, ICatBoxParser catboxParser,
            IPostQueue<AbstractPost> postQueue, IPostQueue<CrawlerData<Post>> jsonQueue, IBlog blog,
            IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, uguuParser, catboxParser, postQueue, blog, downloader, crawlerDataDownloader, progress, pt, ct)
        {
            this.downloader = downloader;
            this.downloader.ChangeCancellationToken(Ct);
            this.tumblrJsonParser = tumblrJsonParser;
            this.jsonQueue = jsonQueue;
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                await GetApiPageWithRetryAsync(0);
                Blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                if (HandleUnauthorizedWebException(webException))
                {
                    Blog.Online = true;
                }
                else if (HandleLimitExceededWebException(webException))
                {
                    Blog.Online = true;
                }
                else
                {
                    Logger.Error("TumblrBlogCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                    ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
                    Blog.Online = false;
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                Blog.Online = false;
            }
            catch (Exception ex) when (ex.Message == "Acceptance of privacy consent needed!")
            {
                Blog.Online = false;
            }
        }

        public override async Task UpdateMetaInformationAsync()
        {
            try
            {
                await UpdateMetaInformationCoreAsync();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                HandleLimitExceededWebException(webException);
            }
        }

        private async Task UpdateMetaInformationCoreAsync()
        {
            if (!Blog.Online)
            {
                return;
            }

            string document = await GetApiPageWithRetryAsync(0);
            var response = ConvertJsonToClass<TumblrApiJson>(document);

            Blog.Title = response.TumbleLog?.Title;
            Blog.Description = response.TumbleLog?.Description;
            Blog.TotalCount = response.PostsTotal;
            Blog.Posts = response.PostsTotal;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrBlogCrawler.Crawl:Start");

            ulong highestId = await GetHighestPostIdAsync();
            Task<bool> grabber = GetUrlsAsync();

            // FIXME: refactor downloader out of class
            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (Blog.DumpCrawlerData)
            {
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();
            }

            bool apiLimitHit = await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);

            UpdateBlogDuplicates();

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
            bool finishedDownloading = await download;

            if (!Ct.IsCancellationRequested)
            {
                Blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !apiLimitHit)
                {
                    Blog.LastId = highestId;
                }
            }

            Blog.Save();

            UpdateProgressQueueInformation(string.Empty);
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

        private async Task<string> GetApiPageAsync(int pageId)
        {
            string url = GetApiUrl(Blog.Url, (Blog.PageSize == 0 ? 1 : Blog.PageSize), pageId * Blog.PageSize);

            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }

            return await GetRequestAsync(url);
        }

        private async Task<string> GetApiPageWithRetryAsync(int pageId)
        {
            string page;
            var attemptCount = 0;

            do
            {
                try
                {
                    attemptCount++;
                    page = await GetApiPageAsync(pageId);
                }
                catch (Exception ex)
                {
                    Logger.Error("TumblrBlogCrawler:GetApiPageWithRetryAsync: {0}", ex);
                    if (attemptCount > ShellService.Settings.MaxNumberOfRetries)
                        throw;
                    page = string.Empty;
                }
            }
            while (string.IsNullOrEmpty(page) && (attemptCount <= ShellService.Settings.MaxNumberOfRetries));

            return page;
        }

        private async Task UpdateTotalPostCountAsync()
        {
            try
            {
                await UpdateTotalPostCountCoreAsync();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                HandleLimitExceededWebException(webException);
                Blog.Posts = 0;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                Blog.Posts = 0;
            }
        }

        private async Task UpdateTotalPostCountCoreAsync()
        {
            string document = await GetApiPageWithRetryAsync(0);
            var response = ConvertJsonToClass<TumblrApiJson>(document);
            Blog.Posts = response.PostsTotal;
        }

        private async Task<ulong> GetHighestPostIdAsync()
        {
            try
            {
                return await GetHighestPostIdCoreAsync();
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return 0;
                }

                HandleLimitExceededWebException(webException);
                return 0;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return 0;
            }
        }

        private async Task<ulong> GetHighestPostIdCoreAsync()
        {
            string document = await GetApiPageWithRetryAsync(0);
            var response = ConvertJsonToClass<TumblrApiJson>(document);

            Blog.Posts = response.PostsTotal;
            Post post = response.Posts?.FirstOrDefault();
            if (DateTime.TryParse(post?.DateGmt, out var latestPost)) Blog.LatestPost = latestPost;
            _ = ulong.TryParse(post?.Id, out var highestId);
            return highestId;
        }

        protected override IEnumerable<int> GetPageNumbers()
        {
            if (string.IsNullOrEmpty(Blog.DownloadPages))
            {
                int totalPosts = Blog.Posts;
                if (!TestRange(Blog.PageSize, 1, 50))
                {
                    Blog.PageSize = 50;
                }

                int totalPages = (totalPosts / Blog.PageSize) + 1;

                return Enumerable.Range(0, totalPages);
            }

            return RangeToSequence(Blog.DownloadPages);
        }

        private async Task<bool> GetUrlsAsync()
        {
            trackedTasks = new List<Task>();
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);

            GenerateTags();

            // page already loaded in GetHighestPostIdCoreAsync(), so retrieve new number of posts already there
            await Task.Run(() => Task.CompletedTask);
            //await UpdateTotalPostCountAsync();

            foreach (int pageNumber in GetPageNumbers())
            {
                await semaphoreSlim.WaitAsync();

                if (!completeGrab)
                {
                    break;
                }

                if (CheckIfShouldStop())
                {
                    break;
                }

                CheckIfShouldPause();

                trackedTasks.Add(CrawlPageAsync(pageNumber));
            }

            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats(GetLastPostId() != 0);

            return incompleteCrawl;
        }

        private async Task CrawlPageAsync(int pageNumber)
        {
            try
            {
                string document = await GetApiPageWithRetryAsync(pageNumber);
                var response = ConvertJsonToClass<TumblrApiJson>(document);

                completeGrab = CheckPostAge(response);

                await AddUrlsToDownloadListAsync(response);

                numberOfPagesCrawled += Blog.PageSize;
                UpdateProgressQueueInformation(Resources.ProgressGetUrlLong, numberOfPagesCrawled, Blog.Posts);
            }
            catch (WebException webException)
            {
                if (HandleLimitExceededWebException(webException))
                {
                    incompleteCrawl = true;
                }
            }
            catch (TimeoutException timeoutException)
            {
                incompleteCrawl = true;
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (Exception e)
            {
                Logger.Error("TumblrBlogCrawler.CrawlPageAsync: {0}", e);
                ShellService.ShowError(e, "{0}: Error parsing post!", Blog.Name);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private bool PostWithinTimeSpan(Post post)
        {
            if (string.IsNullOrEmpty(Blog.DownloadFrom) && string.IsNullOrEmpty(Blog.DownloadTo))
            {
                return true;
            }

            long downloadFromUnixTime = 0;
            long downloadToUnixTime = long.MaxValue;
            if (!string.IsNullOrEmpty(Blog.DownloadFrom))
            {
                DateTime downloadFrom = DateTime.ParseExact(Blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                downloadFromUnixTime = new DateTimeOffset(downloadFrom).ToUnixTimeSeconds();
            }

            if (!string.IsNullOrEmpty(Blog.DownloadTo))
            {
                DateTime downloadTo = DateTime.ParseExact(Blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);
                downloadToUnixTime = new DateTimeOffset(downloadTo).ToUnixTimeSeconds();
            }

            long postTime = 0;
            postTime = post.UnixTimestamp;
            return downloadFromUnixTime < postTime && postTime < downloadToUnixTime;
        }

        private bool CheckPostAge(TumblrApiJson response)
        {
            ulong highestPostId = 0;
            var post = response.Posts.FirstOrDefault();
            if (post == null) return false;
            _ = ulong.TryParse(post.Id, out highestPostId);

            return highestPostId >= GetLastPostId();
        }

        private void AddToJsonQueue(CrawlerData<Post> addToList)
        {
            if (Blog.DumpCrawlerData)
            {
                jsonQueue.Add(addToList);
            }
        }

        private void AddToJsonQueue(string[] urls, Post post)
        {
            if (urls == null || urls.Length == 0) return;
            AddToJsonQueue(new CrawlerData<Post>(FileName(urls[0]), post));
        }

        private async Task AddUrlsToDownloadListAsync(TumblrApiJson document)
        {
            var lastPostId = GetLastPostId();
            foreach (Post post in document.Posts)
            {
                if (CheckIfShouldStop()) { break; }
                CheckIfShouldPause();
                if (lastPostId > 0 && ulong.TryParse(post.Id, out var postId) && postId < lastPostId) { continue; }
                if (!PostWithinTimeSpan(post)) { continue; }
                if (!CheckIfContainsTaggedPost(post)) { continue; }
                if (!CheckIfDownloadRebloggedPosts(post)) { continue; }

                try
                {
                    AddPhotoUrlToDownloadList(post);
                    AddVideoUrlToDownloadList(post);
                    AddAudioUrlToDownloadList(post);
                    AddTextUrlToDownloadList(post);
                    AddQuoteUrlToDownloadList(post);
                    AddLinkUrlToDownloadList(post);
                    AddConversationUrlToDownloadList(post);
                    AddAnswerUrlToDownloadList(post);
                    AddPhotoMetaUrlToDownloadList(post);
                    AddVideoMetaUrlToDownloadList(post);
                    AddAudioMetaUrlToDownloadList(post);
                    await AddExternalPhotoUrlToDownloadListAsync(post);
                }
                catch (NullReferenceException e)
                {
                    Logger.Verbose("TumblrBlogCrawler.AddUrlsToDownloadListAsync: {0}", e);
                }
            }
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            return Blog.DownloadRebloggedPosts || string.IsNullOrEmpty(post.RebloggedFromName) || post.RebloggedFromName == Blog.Name;
        }

        private bool CheckIfContainsTaggedPost(Post post)
        {
            return !Tags.Any() || post.Tags.Any(x => Tags.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        private void AddPhotoUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadPhoto)
            {
                return;
            }

            if (post.Type == "photo")
            {
                AddPhotoUrl(post);
                AddPhotoSetUrl(post);
            }

            AddInlinePhotoUrl(post);

            if (Blog.RegExPhotos)
            {
                AddGenericInlinePhotoUrl(post);
            }
        }

        private void AddVideoUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail)
            {
                return;
            }

            Post postCopy;
            if (post.Type == "video")
            {
                AddVideoUrl(post);

                postCopy = (Post)post.Clone();
                postCopy.VideoPlayer = string.Empty;
            }

            var urls = AddTumblrVideoUrl(InlineSearch(post), post);
            AddToJsonQueue(urls, post);
            urls = AddInlineTumblrVideoUrl(InlineSearch(post), post);
            AddToJsonQueue(urls, post);

            if (Blog.DownloadVideo && Blog.RegExVideos)
            {
                AddGenericInlineVideoUrl(post);
            }
        }

        private void AddAudioUrlToDownloadList(Post post)
        {
            if (Blog.DownloadAudio)
            {
                if (post.Type == "audio")
                {
                    AddAudioUrl(post);
                }
            }
        }

        private void AddTextUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadText || post.Type != "regular")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseText(post);
            AddToDownloadList(new TextPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddQuoteUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadQuote || post.Type != "quote")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseQuote(post);
            AddToDownloadList(new QuotePost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddLinkUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadLink || post.Type != "link")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseLink(post);
            AddToDownloadList(new LinkPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddConversationUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadConversation || post.Type != "conversation")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseConversation(post);
            AddToDownloadList(new ConversationPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddAnswerUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadAnswer || post.Type != "answer")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseAnswer(post);
            AddToDownloadList(new AnswerPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddPhotoMetaUrlToDownloadList(Post post)
        {
            if (!Blog.CreatePhotoMeta || post.Type != "photo")
            {
                return;
            }
            
            string textBody = tumblrJsonParser.ParsePhotoMeta(post);
            AddToDownloadList(new PhotoMetaPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddVideoMetaUrlToDownloadList(Post post)
        {
            if (!Blog.CreateVideoMeta || post.Type != "video")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseVideoMeta(post);
            AddToDownloadList(new VideoMetaPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private void AddAudioMetaUrlToDownloadList(Post post)
        {
            if (!Blog.CreateAudioMeta || post.Type != "audio")
            {
                return;
            }

            string textBody = tumblrJsonParser.ParseAudioMeta(post);
            AddToDownloadList(new AudioMetaPost(textBody, post.Id));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));
        }

        private string ParseImageUrl(Post post)
        {
            // TODO: Not use reflection here? We know the types...
            var imageUrl = (string)post.GetType().GetProperty("PhotoUrl" + ImageSizeForSearching()).GetValue(post, null) ?? post.PhotoUrl1280;
            return RetrieveOriginalImageUrl(imageUrl, (int)post.Width, (int)post.Height, false);
        }

        private string ParseImageUrl(Photo post)
        {
            // TODO: Not use reflection here? We know the types...
            var imageUrl = (string)post.GetType().GetProperty("PhotoUrl" + ImageSizeForSearching()).GetValue(post, null) ?? post.PhotoUrl1280;
            return RetrieveOriginalImageUrl(imageUrl, post.Width, post.Height, false);
        }

        private static string InlineSearch(Post post)
        {
            return string.Join(" ", post.PhotoCaption, post.VideoCaption, post.AudioCaption, post.ConversationText, post.RegularBody, post.Answer,
                                    string.Join(" ", post.Photos.Select(photo => photo.Caption)),
                                    string.Join(" ", post.Conversation.Select(conversation => conversation.Phrase)));
        }

        private void AddInlinePhotoUrl(Post post)
        {
            AddTumblrPhotoUrl(InlineSearch(post), post);
        }

        private void AddGenericInlineVideoUrl(Post post)
        {
            AddGenericVideoUrl(InlineSearch(post), post);
        }

        private void AddPhotoUrl(Post post)
        {
            string imageUrl = ParseImageUrl(post);
            if (CheckIfSkipGif(imageUrl)) return;

            int index = -1;
            if (post.Photos?.Count > 0 && post.PhotoUrl1280 == post.Photos[0].PhotoUrl1280 && !post.Photos[0].PhotoUrl1280.Split('/').Last().StartsWith("tumblr_")) index = 1;

            AddToDownloadList(new PhotoPost(imageUrl, post.Id, post.UnixTimestamp.ToString(), BuildFileName(imageUrl, post, index)));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
        }

        private void AddPhotoSetUrl(Post post)
        {
            if (!post.Photos.Any())
            {
                return;
            }

            bool jsonSaved = false;
            int i = 1;
            if (post.Photos[0].PhotoUrl1280.Split('/').Last().StartsWith("tumblr_")) i = -1;
            foreach (string imageUrl in post.Photos.Select(ParseImageUrl).Where(imgUrl => !CheckIfSkipGif(imgUrl)))
            {
                AddToDownloadList(new PhotoPost(imageUrl, post.Id, post.UnixTimestamp.ToString(), BuildFileName(imageUrl, post, i)));
                if (!jsonSaved || !Blog.GroupPhotoSets && !(string.Equals(Blog.FilenameTemplate, "%f", StringComparison.OrdinalIgnoreCase) && i == -1))
                {
                    jsonSaved = true;
                    AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
                if (i != -1) i++;
            }
        }

        private void AddGenericInlinePhotoUrl(Post post)
        {
            AddGenericPhotoUrl(InlineSearch(post), post);
        }

        private void AddVideoUrl(Post post)
        {
            string videoUrl = null;

            if (Blog.DownloadVideo)
            {
                videoUrl = Regex.Match(post.VideoPlayer, "\"url\":\"([\\S]*/(tumblr_[\\S]*)_filmstrip[\\S]*)\"").Groups[2].ToString();

                if (!string.IsNullOrEmpty(videoUrl))
                {
                    if (ShellService.Settings.VideoSize == 480)
                    {
                        videoUrl += "_480";
                    }

                    AddToDownloadList(new VideoPost("https://vtt.tumblr.com/" + videoUrl + ".mp4", post.Id, post.UnixTimestamp.ToString(), BuildFileName("https://vtt.tumblr.com/" + videoUrl + ".mp4", post, -1)));
                    AddToJsonQueue(new CrawlerData<Post>(videoUrl + ".json", post));
                }
            }

            if (Blog.DownloadVideoThumbnail)
            {
                string thumbnailUrl = Regex.Match(post.VideoPlayer, "poster='([\\S]*/(tumblr_[\\S]*_(frame1|smart1))[\\S]*)'").Groups[1].ToString();
                if (!string.IsNullOrEmpty(thumbnailUrl))
                {
                    AddToDownloadList(new PhotoPost(thumbnailUrl, post.Id, post.UnixTimestamp.ToString(), BuildFileName(thumbnailUrl, post, "photo", -1)));
                    if (string.IsNullOrEmpty(videoUrl))
                    {
                        thumbnailUrl = Regex.Replace(thumbnailUrl, "_(frame1|smart1)", "");
                        AddToJsonQueue(new CrawlerData<Post>(thumbnailUrl + ".json", post));
                    }
                }
            }
        }

        private void AddAudioUrl(Post post)
        {
            string audioUrl = Regex.Match(post.AudioEmbed, "audio_file=([\\S]*)\"").Groups[1].ToString();
            audioUrl = HttpUtility.UrlDecode(audioUrl);
            if (!audioUrl.EndsWith(".mp3"))
            {
                audioUrl = audioUrl + ".mp3";
            }

            AddToDownloadList(new AudioPost(WebUtility.UrlDecode(audioUrl), post.Id, post.UnixTimestamp.ToString(), BuildFileName(audioUrl, post, -1)));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"), post));
        }

        private async Task AddExternalPhotoUrlToDownloadListAsync(Post post)
        {
            string searchableText = InlineSearch(post);
            string timestamp = post.UnixTimestamp.ToString();

            if (Blog.DownloadImgur)
            {
                AddImgurUrl(searchableText, timestamp);
            }

            if (Blog.DownloadImgur)
            {
                await AddImgurAlbumUrlAsync(searchableText, timestamp);
            }

            if (Blog.DownloadGfycat)
            {
                await AddGfycatUrlAsync(searchableText, timestamp);
            }

            if (Blog.DownloadWebmshare)
            {
                AddWebmshareUrl(searchableText, timestamp);
            }

            if (Blog.DownloadUguu)
            {
                AddUguuUrl(searchableText, timestamp);
            }

            if (Blog.DownloadCatBox)
            {
                AddCatBoxUrl(searchableText, timestamp);
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
