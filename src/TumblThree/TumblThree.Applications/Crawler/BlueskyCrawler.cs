using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.Bluesky;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Applications.Extensions;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(BlueskyCrawler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class BlueskyCrawler : AbstractCrawler, ICrawler, IDisposable
    {
        private const string blueskyDateTemplate = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private const string BearerToken = "";

        private readonly IDownloader downloader;
        private readonly IPostQueue<CrawlerData<FeedEntry>> jsonQueue;
        private readonly ICrawlerDataDownloader crawlerDataDownloader;
        private readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings() { MetadataPropertyHandling = MetadataPropertyHandling.Ignore };

        private bool completeGrab = true;
        private bool incompleteCrawl;

        private SemaphoreSlim semaphoreSlim;

        private int numberOfPostsCrawled;

        private Profile bsUser;
        private SemaphoreSlim bsUserMutex = new SemaphoreSlim(1);
        private string guestToken;
        private ulong highestId;
        private string cursor;

        public BlueskyCrawler(IShellService shellService, ICrawlerService crawlerService, IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IPostQueue<AbstractPost> postQueue, IPostQueue<CrawlerData<FeedEntry>> jsonQueue, IBlog blog, IDownloader downloader,
            ICrawlerDataDownloader crawlerDataDownloader, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, progress, webRequestFactory, cookieService, postQueue, blog, downloader, pt, ct)
        {
            this.downloader = downloader;
            this.downloader.ChangeCancellationToken(Ct);
            this.jsonQueue = jsonQueue;
            this.crawlerDataDownloader = crawlerDataDownloader;
            this.crawlerDataDownloader.ChangeCancellationToken(Ct);
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                bsUser = await GetBsUser();
                //if (!string.IsNullOrEmpty(bsUser.Errors?.FirstOrDefault()?.Message))
                //{
                //    Logger.Warning("BlueskyCrawler.IsBlogOnlineAsync: {0} ({1}): {2}", Blog.Name, GetCollectionName(Blog), bsUser.Errors?[0]?.Message);
                //    ShellService.ShowError(null, (bsUser.Errors?[0]?.Code == 63 ? $"{Blog.Name} ({GetCollectionName(Blog)}): " : "") + bsUser.Errors?[0]?.Message);
                //    Blog.Online = false;
                //}
                //else if (bsUser.Data.User.Typename != "User")
                //{
                //    Logger.Warning("BlueskyCrawler.IsBlogOnlineAsync: {0}: {1}, {2}", Blog.Name, bsUser.Data.User.Typename, bsUser.Data.User.Reason);
                //    ShellService.ShowError(null, "{0}: {1}, {2}", Blog.Name, bsUser.Data.User.Typename, bsUser.Data.User.Reason);
                //    Blog.Online = false;
                //}
                //else
                {
                    Blog.Online = true;
                }
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                if (HandleUnauthorizedWebException2(webException))
                {
                    Blog.Online = true;
                }
                else if (HandleLimitExceededWebException(webException, LimitExceededSource.twitter))
                {
                    Blog.Online = true;
                }
                else
                {
                    Logger.Error("BlueskyCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                    ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
                    Blog.Online = false;
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                Blog.Online = false;
            }
            catch (Exception ex)
            {
                Logger.Error("BlueskyCrawler:IsBlogOnlineAsync: {0}", ex);
                throw;
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

                HandleLimitExceededWebException(webException, LimitExceededSource.twitter);
            }
        }

        private async Task UpdateMetaInformationCoreAsync()
        {
            if (!Blog.Online)
            {
                return;
            }

            bsUser = await GetBsUser();

            Blog.Title = bsUser.DisplayName;
            Blog.Description = bsUser.Description;
            Blog.TotalCount = bsUser.PostsCount;
            Blog.Posts = bsUser.PostsCount;
        }

        private void UpdateBlogDuplicates()
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

        public async Task CrawlAsync()
        {
            Logger.Verbose("BlueskyCrawler.Crawl:Start");

            Task crawlerDownloader = Task.CompletedTask;
            if (Blog.DumpCrawlerData)
            {
                await crawlerDataDownloader.GetAlreadyExistingCrawlerDataFilesAsync(Progress);
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();
            }

            Task<bool> grabber = GetUrlsAsync();

            Task<bool> download = downloader.DownloadBlogAsync();

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
                    if (highestId != 0) Blog.LatestPost = DateTime.FromBinary((long)highestId);
                }
            }

            Blog.Save();

            UpdateProgressQueueInformation(string.Empty);
        }

        private async Task<string> GetApiUrl(string url, byte type, string cursor, int pageSize)
        {
            switch (type)
            {
                //case 0:
                //    url = "https://api.bsky.app/1.1/guest/activate.json";
                //    break;
                case 1:
                    url = string.Format("https://public.api.bsky.app/xrpc/app.bsky.actor.getProfile?actor=" + url.Split('/').Last());
                    break;
                case 2:
                    if (!string.IsNullOrEmpty(cursor)) cursor = $"&cursor={cursor}";
                    var filter = Blog.DownloadReplies ? "posts_with_replies" : "posts_no_replies";
                    url = string.Format("https://public.api.bsky.app/xrpc/app.bsky.feed.getAuthorFeed?actor={0}&limit={1}&filter={2}{3}", bsUser.Did, pageSize, filter, cursor);
                    break;
                case 3:
                    url = string.Format("https://public.api.bsky.app/xrpc/app.bsky.feed.getPosts?uris={0}", cursor);
                    break;
            }
            return await Task.FromResult(url);
        }

        private async Task<string> GetRequestAsync(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            string[] cookieHosts = { "https://bsky.app/" };
            return await RequestApiDataAsync(url, BearerToken, referer, headers, cookieHosts);
        }

        private async Task<string> RequestApiDataAsync(string url, string bearerToken, string referer = "",
            Dictionary<string, string> headers = null, IEnumerable<string> cookieHosts = null)
        {
            var requestRegistration = new CancellationTokenRegistration();
            try
            {
                HttpWebRequest request = WebRequestFactory.CreateGetXhrRequest(url, referer, headers);
                cookieHosts = cookieHosts ?? new List<string>();
                foreach (string cookieHost in cookieHosts)
                {
                    CookieService.GetUriCookie(request.CookieContainer, new Uri(cookieHost));
                }

                //request.PreAuthenticate = true;
                //request.Headers.Add("Authorization", "Bearer " + bearerToken);
                request.Accept = "application/json";

                requestRegistration = Ct.Register(() => request.Abort());
                return await WebRequestFactory.ReadRequestToEndAsync(request, true);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        private async Task<Profile> GetBsUser()
        {
            if (bsUser == null)
            {
                await bsUserMutex.WaitAsync().ConfigureAwait(false);
                if (bsUser != null) return bsUser;
                try
                {
                    var data = await GetUserByHandleAsync();
                    try
                    {
                        bsUser = JsonConvert.DeserializeObject<Profile>(data, jsonSerializerSettings);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("BlueskyCrawler.GetBsUser: {0}", e);
                        throw;
                    }
                }
                finally
                {
                    bsUserMutex.Release();
                }
            }
            return bsUser;
        }

        private async Task<string> GetGuestToken()
        {
            if (guestToken == null)
            {
                var requestRegistration = new CancellationTokenRegistration();
                try
                {
                    string url = await GetApiUrl(Blog.Url, 0, "", 0);
                    if (ShellService.Settings.LimitConnectionsBlueskyApi)
                    {
                        CrawlerService.TimeconstraintBlueskyApi.Acquire();
                    }
                    var headers = new Dictionary<string, string>();
                    headers.Add("Origin", "https://bsky.app");
                    headers.Add("Authorization", "Bearer " + BearerToken);
                    headers.Add("Accept-Language", "en-US,en;q=0.5");
                    HttpWebRequest request = WebRequestFactory.CreatePostRequest(url, "https://bsky.app", headers);
                    CookieService.GetUriCookie(request.CookieContainer, new Uri("https://bsky.app"));
                    requestRegistration = Ct.Register(() => request.Abort());
                    var content = await WebRequestFactory.ReadRequestToEndAsync(request);
                    guestToken = ((JValue)((JObject)JsonConvert.DeserializeObject(content, jsonSerializerSettings))["guest_token"]).Value<string>();
                }
                catch (Exception e)
                {
                    Logger.Error("GetGuestToken: {0}", e);
                }
                finally
                {
                    requestRegistration.Dispose();
                }
            }
            return guestToken;
        }

        private async Task<string> GetApiPageAsync(byte type, string cursor)
        {
            string url = await GetApiUrl(Blog.Url, type, cursor, Blog.PageSize);

            if (ShellService.Settings.LimitConnectionsBlueskyApi)
            {
                CrawlerService.TimeconstraintBlueskyApi.Acquire();
            }

            //var referer = type < 3 ? Blog.Url : $"https://bsky.app/search?q=from%3A{Blog.Name}%20until%3A{oldestApiPost}&src=typed_query&f=latest";

            //var headers = new Dictionary<string, string>();
            //headers.Add("Origin", "https://bsky.app");
            //if (type > 0)
            //{
            //    //var token = await GetGuestToken();
            //    //headers.Add("x-guest-token", token);
            //    headers.Add("x-twitter-active-user", "yes");
            //    headers.Add("x-twitter-client-language", "en");
            //    var cookie = CookieService.GetAllCookies().FirstOrDefault(c => c.Name == "ct0");
            //    if (cookie != null) headers.Add("x-csrf-token", cookie.Value);
            //}

            return await GetRequestAsync(url);
        }

        private async Task<string> GetUserByHandleAsync()
        {
            string page;
            var attemptCount = 0;

            do
            {
                page = await GetApiPageAsync(1, "");
                attemptCount++;
            }
            while (string.IsNullOrEmpty(page) && (attemptCount < ShellService.Settings.MaxNumberOfRetries));

            return page;
        }

        private async Task<string> GetUserPostsAsync(byte type, string cursor)
        {
            string page;
            var attemptCount = 0;

            do
            {
                page = await GetApiPageAsync(type, cursor);
                attemptCount++;
            }
            while (string.IsNullOrEmpty(page) && (attemptCount < ShellService.Settings.MaxNumberOfRetries));

            return page;
        }

        protected override IEnumerable<int> GetPageNumbers()
        {
            if (string.IsNullOrEmpty(Blog.DownloadPages))
            {
                int totalPosts = Blog.Posts;
                if (!TestRange(Blog.PageSize, 1, 100))
                {
                    Blog.PageSize = 100;
                }

                int totalPages = (totalPosts / Blog.PageSize) + 1;

                return Enumerable.Range(0, totalPages);
            }

            return RangeToSequence(Blog.DownloadPages);
        }

        private async Task<bool> GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);

            GenerateTags();

            bool error = false;
            try
            {
                await IsBlogOnlineAsync();
            }
            catch (Exception e)
            {
                Logger.Error("BlueskyCrawler:GetUrlsAsync", e);
                error = true;
            }
            if (error || !Blog.Online)
            {
                PostQueue.CompleteAdding();
                jsonQueue.CompleteAdding();
                return true;
            }

            Blog.Posts = bsUser.PostsCount;
            if (!TestRange(Blog.PageSize, 1, 100)) Blog.PageSize = 100;

            int expectedPages = Blog.Posts / Blog.PageSize + 1;
            int pageNo = 1;

            while (true)
            {
                await semaphoreSlim.WaitAsync();

                if (!completeGrab) break;
                if (CheckIfShouldStop()) break;
                CheckIfShouldPause();

                await CrawlPageAsync(pageNo);

                if (expectedPages > 0)
                {
                    expectedPages--;
                    pageNo++;
                }
                else
                {
                    break;
                }
            }

            PostQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats(GetLastPostId() != 0);

            return incompleteCrawl;
        }

        private async Task<Post> GetPinnedPost()
        {
            if (bsUser.PinnedPost == null) return null;
            var document = await GetApiPageAsync(3, bsUser.PinnedPost.Uri);
            var postList = ConvertJsonToClassNew<PostsList>(document, true);

            return postList.Posts?.FirstOrDefault();
        }

        private async Task CrawlPageAsync(int pageNo)
        {
            const int maxRetries = 2;
            int retries = 0;
            do
            {
                string handle429 = null;
                try
                {
                    string document = await GetUserPostsAsync(2, cursor);

                    var feed = ConvertJsonToClassNew<AuthorFeed>(document, true);

                    if (highestId == 0)
                    {
                        var pinnedPost = await GetPinnedPost();
                        Post post = feed.FeedEntries.FirstOrDefault(x => (pinnedPost?.Cid ?? "") != x.Post.Cid)?.Post;
                        if (post != null)
                        {
                            highestId = (ulong)post.IndexedAt.ToBinary();
                        }
                    }

                    completeGrab = CheckPostAge(feed);
                              
                    var newCursor = feed.Cursor.ToString(blueskyDateTemplate);
                    if (cursor == newCursor) completeGrab = false;
                    cursor = newCursor;

                    await AddUrlsToDownloadListAsync(feed.FeedEntries);

                    numberOfPostsCrawled += Blog.PageSize;
                    UpdateProgressQueueInformation(Resources.ProgressGetUrl2Long, Math.Min(numberOfPostsCrawled, Blog.Posts), Blog.Posts);
                    retries = 200;
                }
                catch (WebException webException) when (webException.Response != null)
                {
                    if (HandleLimitExceededWebException(webException, LimitExceededSource.bluesky))
                    {
                        //incompleteCrawl = true;
                        retries++;
                        handle429 = ((HttpWebResponse)webException?.Response).Headers["RateLimit-Reset"];
                    }
                    if (((HttpWebResponse)webException?.Response).StatusCode == HttpStatusCode.Forbidden)
                    {
                        Logger.Error("BlueskyCrawler.CrawlPageAsync: {0}", string.Format(CultureInfo.CurrentCulture, Resources.ProtectedBlog, $"{Blog.Name} ({GetCollectionName(Blog)})"));
                        ShellService.ShowError(webException, Resources.ProtectedBlog, $"{Blog.Name} ({GetCollectionName(Blog)})");
                        if (pageNo > 1 && retries + 1 < maxRetries)
                        {
                            retries++;
                            Thread.Sleep(2000);
                        }
                        else
                        {
                            completeGrab = false;
                            retries = 403;
                        }
                    }
                }
                catch (TimeoutException timeoutException)
                {
                    //incompleteCrawl = true;
                    retries++;
                    HandleTimeoutException(timeoutException, Resources.Crawling);
                    Thread.Sleep(3000);
                }
                catch (Exception e) when (e.Message == "UserUnavailable")
                {
                    Logger.Error("BlueskyCrawler.CrawlPageAsync: {0}", string.Format(CultureInfo.CurrentCulture, Resources.ProtectedBlog, Blog.Name));
                    ShellService.ShowError(e, Resources.ProtectedBlog, Blog.Name);
                    completeGrab = false;
                    retries = 403;
                }
                catch (Exception e) when (e.Message == "NoPostsYet")
                {
                    Logger.Information("{0}: No posts yet.", Blog.Name);
                    completeGrab = false;
                    retries = 404;
                }
                catch (Exception e) when (e.Source == "TwitterError")
                {
                    Logger.Error("BlueskyCrawler.CrawlPageAsync: {0}: {1}", Blog.Name, e.InnerException.Message);
                    ShellService.ShowError(e, "{0}: Twitter Error: {1}", Blog.Name, e.Message);
                    retries++;
                    Thread.Sleep(2000);
                }
                catch (Exception e) when (e is FormatException)
                {
                    Logger.Error("BlueskyCrawler.CrawlPageAsync: {0}", e);
                    ShellService.ShowError(e, "{0}: {1}", Blog.Name, e.Message);
                    retries = 450;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    retries = 400;
                }
                finally
                {
                    semaphoreSlim.Release();
                }
                if (!string.IsNullOrEmpty(handle429))
                {
                    try
                    {
                        DateTimeOffset dto = DateTimeOffset.FromUnixTimeSeconds(long.Parse(handle429));
                        var waitTime = (int)dto.Subtract(DateTime.Now).TotalMilliseconds;
                        if (waitTime > 0)
                        {
                            Progress.Report(new DownloadProgress() { Progress = string.Format("waiting until {0}", dto.ToLocalTime().ToString()) });
                            var cancelled = Ct.WaitHandle.WaitOne(waitTime);
                            if (cancelled) retries = 400;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("BlueskyCrawler.CrawlPageAsync: error while handling 429: {0}", e);
                        retries = 400;
                    }
                }
            } while (retries < maxRetries);

            if (retries <= maxRetries || retries >= 400)
            {
                incompleteCrawl = true;
            }
        }

        private bool PostWithinTimeSpan(Post post)
        {
            if (string.IsNullOrEmpty(Blog.DownloadFrom) && string.IsNullOrEmpty(Blog.DownloadTo))
            {
                return true;
            }

            try
            {
                DateTime downloadFrom = DateTime.MinValue;
                DateTime downloadTo = DateTime.MaxValue;
                if (!string.IsNullOrEmpty(Blog.DownloadFrom))
                {
                    downloadFrom = DateTime.ParseExact(Blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                }
                if (!string.IsNullOrEmpty(Blog.DownloadTo))
                {
                    downloadTo = DateTime.ParseExact(Blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None).AddDays(1);
                }

                return downloadFrom <= post.IndexedAt && post.IndexedAt < downloadTo;
            }
            catch (System.FormatException)
            {
                throw new FormatException(Resources.BlogValueHasWrongFormat);
            }
        }

        private bool CheckPostAge(AuthorFeed feed)
        {
            var entry = feed.FeedEntries.FirstOrDefault();
            if (entry == null) return false;

            ulong highestPostId = (ulong)entry.Post.IndexedAt.ToBinary();

            return highestPostId >= GetLastPostId();
        }

        private void AddToJsonQueue(CrawlerData<FeedEntry> addToList)
        {
            if (!Blog.DumpCrawlerData) { return; }

            if (Blog.ForceRescan || !crawlerDataDownloader.ExistingCrawlerDataContainsOrAdd(addToList.Filename))
            {
                jsonQueue.Add(addToList);
            }
        }

        private async Task AddUrlsToDownloadListAsync(List<FeedEntry> entries)
        {
            var lastPostId = GetLastPostId();

            foreach (var entry in entries)
            {
                try
                {
                    //                  if (entry.Post.Cid != "bafyreigew3rdind4yfwovdkko4pecn6q45zur77ylyeg2ea5necwfzjymq") continue;
                    if (CheckIfShouldStop()) { break; }
                    CheckIfShouldPause();
                    if (lastPostId > 0 && entry.Post.IndexedAt.ToBinary() < (long)lastPostId) { continue; }
                    if (!PostWithinTimeSpan(entry.Post)) { continue; }
                    if (!CheckIfContainsTaggedPost(entry.Post)) { continue; }
                    if (!CheckIfDownloadRebloggedPosts(entry)) { continue; }
                    if (!CheckIfDownloadReplies(entry)) { continue; }

                    AddPhotoUrlToDownloadList(entry);
                    AddVideoUrlToDownloadList(entry);
                    AddTextUrlToDownloadList(entry);
                }
                catch (Exception e)
                {
                    Logger.Error("BlueskyCrawler.AddUrlsToDownloadListAsync: {0}", e);
                    ShellService.ShowError(e, "{0}: Error parsing post!", Blog.Name);
                }
            }
            await Task.CompletedTask;
        }

        private bool CheckIfContainsTaggedPost(Post post)
        {
            List<string> postedTags = post.Record.Facets.Where(x => x.Features?.FirstOrDefault()?.Type == "app.bsky.richtext.facet#tag").Select(s => s.Features.FirstOrDefault()?.Tag).ToList();
            if (postedTags.Count > 0) System.Diagnostics.Debug.WriteLine("");
            return Tags.Count == 0 || postedTags.Any(tag => Tags.Contains(tag));
        }

        private bool CheckIfDownloadRebloggedPosts(FeedEntry feedEntry)
        {
            bool isRepost = feedEntry.Reason?.Type == "app.bsky.feed.defs#reasonRepost";
            return Blog.DownloadRebloggedPosts || !isRepost;
        }

        private bool CheckIfDownloadReplies(FeedEntry feedEntry)
        {
            return Blog.DownloadReplies || feedEntry.Reply == null;
        }

        private void AddPhotoUrlToDownloadList(FeedEntry feedEntry)
        {
            if (!Blog.DownloadPhoto || feedEntry.Post.Embed == null) return;

            AddPhotoUrl(feedEntry);
        }

        private void AddVideoUrlToDownloadList(FeedEntry feedEntry)
        {
            if (!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail) return;

            AddVideoUrl(feedEntry);
        }

        private void AddTextUrlToDownloadList(FeedEntry feedEntry)
        {
            if (!Blog.DownloadText) return;

            var post = feedEntry.Post;
            var body = GetPostText(feedEntry);
            if (string.IsNullOrEmpty(body)) return;
            var postId = PostId(post);
            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{postId}.txt", post, "text", -1) : null;
            AddToDownloadList(new TextPost(body, postId, filename));
            if (post.Embed == null  || !(Blog.DownloadPhoto || Blog.DownloadVideo))
            {
                AddToJsonQueue(new CrawlerData<FeedEntry>($"{postId}.json", feedEntry));
            }
        }

        private void AddPhotoUrl(FeedEntry feedEntry)
        {
            var post = feedEntry.Post;
            List<ImageItem> images = new List<ImageItem>();
            // images in images post
            if (post.Embed?.Type == "app.bsky.embed.images#view" && post.Embed?.Images != null)
            {
                images.AddRange(post.Embed?.Images);
            }
            // images in quoted posts with added images
            if (post.Embed?.Type == "app.bsky.embed.recordWithMedia#view" && post.Embed?.Media?.Images != null)
            {
                images.AddRange(post.Embed?.Media?.Images);
            }
            // images in quoted posts
            if (post.Embed?.Type == "app.bsky.embed.record#view" )
            {
                // a quoted images post
                if (post.Embed.Record.Embeds?.Where(x => x.Type == "app.bsky.embed.images#view").Any() ?? false)
                {
                    images.AddRange(post.Embed.Record.Embeds.Where(x => x.Type == "app.bsky.embed.images#view").Select(s => s.Images).SelectMany(_ => _).ToList());
                }
                // a quoted external/URL post
                var embeds = post.Embed.Record.Embeds?.Where(x => x.Type == "app.bsky.embed.external#view");
                if (embeds?.Any() ?? false)
                {
                    // and its file URL
                    images.AddRange(embeds.Where(x => !Blog.SkipGif && ((x.External?.Uri ?? "").EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) ||
                                                                        (x.External?.Uri ?? "").ContainsCI(".gif?")))
                                          .Select(s => new ImageItem() { Fullsize = s.External.Uri }).ToList());
                    // and its preview thumb
                    images.AddRange(embeds.Where(x => !(x.External?.Uri ?? "").EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) &&
                                                        !(x.External?.Uri ?? "").ContainsCI(".gif?") && x.External?.Thumb != null)
                                            .Select(s => new ImageItem() { Fullsize = s.External.Thumb.ToString() }).ToList());
                }
            }
            // thumb of embeded external/URL
            if (post.Embed?.Type == "app.bsky.embed.external#view" )
            {
                // GIF post
                if (post.Embed.External?.Uri != null &&
                    !Blog.SkipGif && (post.Embed.External.Uri.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) || post.Embed.External.Uri.ContainsCI(".gif?")))
                {
                    images.Add(new ImageItem() { Fullsize = post.Embed.External.Uri });
                }
                // embeded link post
                //else if (post.Record.Facets?.Where(x => x.Features?[0].Type == "app.bsky.richtext.facet#link").Any() ?? false)
                else if (post.Embed.External?.Uri != null && post.Embed.External?.Thumb != null &&
                    !post.Embed.External.Uri.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) && !post.Embed.External.Uri.ContainsCI(".gif?"))
                {
                    images.Add(new ImageItem() { Fullsize = post.Embed.External?.Thumb.ToString() });
                }
            }
           
            for (int i = 0; i < images.Count; i++)
            {
                var imageUrl = images[i].Fullsize;
                var index = images.Count > 1 ? i + 1 : -1;
                var filename = BuildFileName(imageUrl, post, "photo", index);
                AddToDownloadList(new PhotoPost(imageUrl, "", PostId(post), UnixTimestamp(post).ToString(), filename));
                if (i == 0)
                {
                    var urlPrepared = CorrectUrlFileExtension(imageUrl.IndexOf('?') > 0 ? imageUrl.Substring(0, imageUrl.IndexOf('?')) : imageUrl);
                    AddToJsonQueue(new CrawlerData<FeedEntry>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), feedEntry));
                }
            }
        }

        private void AddVideoUrl(FeedEntry feedEntry)
        {
            var post = feedEntry.Post;

            if (post.Record.Type != "app.bsky.embed.video" && post.Record.Embed?.Type != "app.bsky.embed.video") return;

            var embededVideo = post.Embed;
            if (embededVideo.Type != "app.bsky.embed.video#view") { System.Diagnostics.Debug.WriteLine(""); }
            var filenamePrepared = FileNameSecondLast(embededVideo.Playlist) + "." + (post.Record.Embed?.Video?.MimeType ?? "").Split('/').Last();
            if (post.Record.Embed?.Video?.MimeType != "video/mp4") { System.Diagnostics.Debug.WriteLine(""); }

            if (Blog.DownloadVideo)
            {
                var urlParts = embededVideo.Playlist.Split('/');
                urlParts[urlParts.Length - 2] = filenamePrepared;
                var postedUrl = string.Join("/", urlParts.Take(urlParts.Length - 1));
                AddToDownloadList(new VideoPost(embededVideo.Playlist, postedUrl, PostId(post), UnixTimestamp(post).ToString(), BuildFileName(filenamePrepared, post, "video", -1)));
                AddToJsonQueue(new CrawlerData<FeedEntry>(Path.ChangeExtension(filenamePrepared, ".json"), feedEntry));
            }

            if (Blog.DownloadVideoThumbnail)
            {
                var imageUrl = post.Embed.Thumbnail;
                var filename = FileNameSecondLast(imageUrl);
                if (!string.Equals(Path.GetFileNameWithoutExtension(filenamePrepared), Path.GetFileNameWithoutExtension(filename), StringComparison.OrdinalIgnoreCase))
                {
                    filename = Path.GetFileNameWithoutExtension(filenamePrepared) + "_" + filename;
                }
                filename = BuildFileName(filename, post, "photo", -1);
                AddToDownloadList(new PhotoPost(imageUrl, "", PostId(post), UnixTimestamp(post).ToString(), filename));
                if (!Blog.DownloadVideo)
                {
                    AddToJsonQueue(new CrawlerData<FeedEntry>(Path.ChangeExtension(filenamePrepared, ".json"), feedEntry));
                }
            }
        }

        private static string PostId(Post post)
        {
            return post.Uri.Split('/').Last();
        }

        private string BuildFileName(string url, Post post, string type, int index)
        {
            var reblogged = false;
            //DataModels.Twitter.TimelineTweets.User user = null;
            //if (post.Legacy.RetweetedStatusResult != null)
            //{
            //    user = GetRetweetedUser(post);
            //    reblogged = user.RestId != post.Legacy.UserIdStr;
            //}
            //var userId = post.Legacy.Entities.Media[0].SourceUserIdStr;
            var reblogName = "";
            var reblogId = "";
            //if (reblogged && !Users.ContainsKey(userId))
            //{
            //    if (post.FullText.StartsWith("RT @", StringComparison.InvariantCulture) && post.FullText.Contains(':'))
            //    {
            //        reblogName = post.FullText.Substring(4, post.FullText.IndexOf(':', 4) - 4);
            //    }
            //    reblogId = "123";
            //}
            //else 
            //if (reblogged)
            //{
            //    reblogName = user.Legacy.ScreenName;
            //    reblogId = GetRetweetedTweet(post).Legacy.IdStr;
            //}
            var tags = GetTags(post);
            url = CorrectUrlFileExtension(url);
            return BuildFileNameCore(url, bsUser.Handle, GetDate(post), UnixTimestamp(post), index, type, PostId(post),
                tags, "", GetTitle(post.Record.Text, tags), reblogName, "", reblogId, post.LikeCount, type == "video");
        }

        private static string CorrectUrlFileExtension(string url)
        {
            return url.EndsWith("@jpeg", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".jpg" :
                url.EndsWith("@png", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".png":
                url.EndsWith("@webp", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".webp" :
                url.EndsWith("@heic", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".heic" :
                url.EndsWith("@heif", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".heif" :
                url.EndsWith("@mp4", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".mp4" :
                url.EndsWith("@mpeg", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".mpeg" :
                url.EndsWith("@webm", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".webm" :
                url.EndsWith("@mov", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".mov" : url;
        }

        private static int UnixTimestamp(Post post)
        {
            long postTime = ((DateTimeOffset)GetDate(post)).ToUnixTimeSeconds();
            return (int)postTime;
        }

        private static DateTime GetDate(Post post)
        {
            return post.IndexedAt;
        }

        private static List<string> GetTags(Post post)
        {
            List<string> postedTags = post.Record.Facets.Where(x => x.Features?.FirstOrDefault()?.Type == "app.bsky.richtext.facet#tag").Select(s => s.Features.FirstOrDefault()?.Tag).ToList();
            if (postedTags?.Count > 0) System.Diagnostics.Debug.WriteLine("");
            return postedTags ?? new List<string>();
        }

        private static string GetPostUrl(Post post)
        {
            return $"https://bsky.app/profile/{post.Author.Handle}/post/{PostId(post)}";
        }

        private static string GetTitle(string text, List<string> tags)
        {
            string title = text ?? "";
            if (title.StartsWith("via: ")) title = title.Substring(5);
            var regexUrls = new Regex(@"https://t.co/[\d\w]+");
            title = regexUrls.Replace(title, "");
            var regexRT = new Regex(@"RT @[^:]+:");
            title = regexRT.Replace(title, "");
            tags.ForEach(t => title = title.Replace("#" + t, ""));
            title = WebUtility.HtmlDecode(title).Replace("\\n", "").Trim();
            return title;
        }

        protected static string FileNameSecondLast(string url)
        {
            return url.Split('/').Reverse().Skip(1).Take(1).ToArray()[0];
        }

        private static string GetPostText(FeedEntry feedEntry)
        {
            var post = feedEntry.Post;
            var dateString = GetDate(post).ToString("u");

            // shortened FullText can happen for foreign and own retweets
            var reblogged = feedEntry.Reason?.Type == "app.bsky.feed.defs#reasonRepost";
            var text = (reblogged ? $"Repost @{post.Author.Handle}:\n" : "") + post.Record.Text;
            var links = new Dictionary<string, string>();
            var matches = Regex.Match(text.Replace("\n"," "), " (.+)...");
            for (int i = 1; i < matches.Groups.Count; i++)
            {
                if ((post.Embed?.External?.Uri ?? "").Contains(matches.Groups[i].Value))
                {
                    links.Add(matches.Groups[i].Value, post.Embed.External.Uri);
                }
            }
            var dict = new Dictionary<string, object>()
            {
                { "id", PostId(post) },
                { "date", dateString },
                { "text", text },
                { "url", GetPostUrl(post) }
            };
            if (links.Count > 0) dict.Add("links", links);
            var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            return json;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                semaphoreSlim?.Dispose();
                downloader.Dispose();
                bsUserMutex.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
