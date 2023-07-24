using Newtonsoft.Json;
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
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.Twitter.TwitterUser;
using TumblThree.Applications.DataModels.Twitter.TimelineTweets;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Runtime.Serialization.Json;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TwitterCrawler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TwitterCrawler : AbstractCrawler, ICrawler, IDisposable
    {
        private const string twitterDateTemplate = "ddd MMM dd HH:mm:ss +ffff yyyy";
        private const string graphQlTokenUserByScreenName = "xc8f1g7BYqr6VTzTbvNlGw";
        private const string graphQlTokenUserTweets = "2GIWTr7XwadIixZDtyXd4A";
        private const string BearerToken = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

        private readonly IDownloader downloader;
        private readonly IPostQueue<CrawlerData<Tweet>> jsonQueue;
        private readonly IList<string> existingCrawlerData = new List<string>();
        private readonly object existingCrawlerDataLock = new object();
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private bool completeGrab = true;
        private bool incompleteCrawl;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        private int numberOfPostsCrawled;

        private TwitterUser twUser;
        private SemaphoreSlim twUserMutex = new SemaphoreSlim(1);
        private string guestToken;
        private ulong highestId;
        private string cursor;
        private string oldestApiPost;

        // instead of new field reuse DownloadAnswer for replies
        private bool BlogDownloadReplies => Blog.DownloadAnswer;

        public TwitterCrawler(IShellService shellService, ICrawlerService crawlerService, IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IPostQueue<AbstractPost> postQueue, IPostQueue<CrawlerData<Tweet>> jsonQueue, IBlog blog, IDownloader downloader,
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
                twUser = await GetTwUser();
                if (!string.IsNullOrEmpty(twUser.Errors?[0]?.Message))
                {
                    Logger.Warning("TwitterCrawler.IsBlogOnlineAsync: {0}: {1}", Blog.Name, twUser.Errors?[0]?.Message);
                    ShellService.ShowError(null, (twUser.Errors?[0]?.Code == 63 ? Blog.Name + ": " : "") + twUser.Errors?[0]?.Message);
                    Blog.Online = false;
                }
                else
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
                    Logger.Error("TwitterCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                    ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
                    Blog.Online = false;
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
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

            twUser = await GetTwUser();

            if (twUser.Errors != null && twUser.Errors.Count > 0) throw new Exception($"{Blog.Name}: {twUser.Errors[0].Message}");

            Blog.Title = twUser.Data.User.Legacy.ScreenName;
            Blog.Description = twUser.Data.User.Legacy.Description;
            Blog.TotalCount = twUser.Data.User.Legacy.StatusesCount;
            Blog.Posts = twUser.Data.User.Legacy.StatusesCount;
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
            Logger.Verbose("TwitterCrawler.Crawl:Start");

            Task<bool> grabber = GetUrlsAsync();

            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (Blog.DumpCrawlerData)
            {
                await GetAlreadyExistingCrawlerDataFilesAsync();
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

        private async Task<string> GetApiUrl(string url, byte type, string cursor, int pageSize)
        {
            switch (type)
            {
                //case 0:
                //    url = "https://api.twitter.com/1.1/guest/activate.json";
                //    break;
                case 1:
                    url = string.Format("https://twitter.com/i/api/graphql/{0}/UserByScreenName" +
                         "?variables=%7B%22screen_name%22%3A%22{1}%22%2C%22withSafetyModeUserFields%22%3Atrue%7D&features=%7B%22hidden_profile_likes_enabled%22%3Afalse%2C%22hidden_profile_subscriptions_enabled%22%3Afalse%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22subscriptions_verification_info_verified_since_enabled%22%3Atrue%2C%22highlights_tweets_tab_ui_enabled%22%3Atrue%2C%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%7D&fieldToggles=%7B%22withAuxiliaryUserLabels%22%3Afalse%7D",
                         graphQlTokenUserByScreenName, url.Split('/').Last());
                    break;
                case 2:
                    if (!string.IsNullOrEmpty(cursor)) cursor = string.Format("%2C%22cursor%22%3A%22{0}%22", cursor.Replace("+", "%2B"));    //HttpUtility.UrlEncode(cursor)
                    var restId = (await GetTwUser()).Data.User.RestId;
                    var includeReplies = BlogDownloadReplies.ToString().ToLower();
                    url = string.Format("https://twitter.com/i/api/graphql/{0}/UserTweets" +
                        "?variables=%7B%22userId%22%3A%22{1}%22%2C%22count%22%3A{2}{3}%2C%22includePromotedContent%22%3Atrue%2C%22withQuickPromoteEligibilityTweetFields%22%3Atrue%2C%22withVoice%22%3Atrue%2C%22withV2Timeline%22%3Atrue%7D&features=%7B%22rweb_lists_timeline_redesign_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Atrue%2C%22responsive_web_twitter_article_tweet_consumption_enabled%22%3Afalse%2C%22tweet_awards_web_tipping_enabled%22%3Afalse%2C%22freedom_of_speech_not_reach_fetch_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Atrue%2C%22longform_notetweets_rich_text_read_enabled%22%3Atrue%2C%22longform_notetweets_inline_media_enabled%22%3Atrue%2C%22responsive_web_media_download_video_enabled%22%3Afalse%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D&fieldToggles=%7B%22withAuxiliaryUserLabels%22%3Afalse%2C%22withArticleRichContentState%22%3Afalse%7D",
                        graphQlTokenUserTweets, restId, pageSize, cursor);
                    break;
                //case 3:
                //    if (!string.IsNullOrEmpty(cursor)) cursor = string.Format("&cursor={0}", cursor.Replace("+", "%2B"));
                //    url = string.Format("https://twitter.com/i/api/2/search/adaptive.json?include_profile_interstitial_type=1&include_blocking=1&include_blocked_by=1&include_followed_by=1&include_want_retweets=1&include_mute_edge=1&include_can_dm=1&include_can_media_tag=1&skip_status=1&cards_platform=Web-12&include_cards=1&include_ext_alt_text=true&include_quote_count=true&include_reply_count=1&tweet_mode=extended&include_entities=true&include_user_entities=true&include_ext_media_color=true&include_ext_media_availability=true&send_error_codes=true&simple_quoted_tweet=true&q=(from%3A{0})%20until%3A{1}%20since%3A2006-01-01&tweet_search_mode=live&count={2}&query_source=typed_query{3}&pc=1&spelling_corrections=1&ext=mediaStats%2ChighlightedLabel", Blog.Name, oldestApiPost, pageSize, cursor);
                //    break;
            }
            return url;
        }

        private async Task<string> GetRequestAsync(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            string[] cookieHosts = { "https://twitter.com/" };
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

                request.PreAuthenticate = true;
                request.Headers.Add("Authorization", "Bearer " + bearerToken);
                request.Accept = "application/json";

                requestRegistration = Ct.Register(() => request.Abort());
                return await WebRequestFactory.ReadRequestToEndAsync(request, true);
            }
            finally
            {
                requestRegistration.Dispose();
            }
        }

        private async Task<TwitterUser> GetTwUser()
        {
            if (twUser == null)
            {
                await twUserMutex.WaitAsync().ConfigureAwait(false);
                if (twUser != null) return twUser;
                try
                {
                    var data = await GetUserByScreenNameAsync();
                    try
                    {
                        twUser = JsonConvert.DeserializeObject<TwitterUser>(data);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("TwitterCrawler.GetTwUser: {0}", e);
                        throw;
                    }
                }
                finally
                {
                    twUserMutex.Release();
                }
            }
            return twUser;
        }

        private async Task<string> GetGuestToken()
        {
            if (guestToken == null)
            {
                var requestRegistration = new CancellationTokenRegistration();
                try
                {
                    string url = await GetApiUrl(Blog.Url, 0, "", 0);
                    if (ShellService.Settings.LimitConnectionsApi)
                    {
                        CrawlerService.TimeconstraintApi.Acquire();
                    }
                    var headers = new Dictionary<string, string>();
                    headers.Add("Origin", "https://twitter.com");
                    headers.Add("Authorization", "Bearer " + BearerToken);
                    HttpWebRequest request = WebRequestFactory.CreatePostRequest(url, "https://twitter.com", headers);
                    CookieService.GetUriCookie(request.CookieContainer, new Uri("https://twitter.com"));
                    requestRegistration = Ct.Register(() => request.Abort());
                    var content = await WebRequestFactory.ReadRequestToEndAsync(request);
                    guestToken = ((JValue)((JObject)JsonConvert.DeserializeObject(content))["guest_token"]).Value<string>();
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
            string url = await GetApiUrl(Blog.Url, type, cursor, Blog.PageSize == 0 ? 20 : Blog.PageSize);

            if (ShellService.Settings.LimitConnectionsApi)
            {
                CrawlerService.TimeconstraintApi.Acquire();
            }

            var referer = Blog.Url;

            var headers = new Dictionary<string, string>();
            headers.Add("Origin", "https://twitter.com");
            if (type > 0)
            {
                //var token = await GetGuestToken();
                //headers.Add("x-guest-token", token);
                headers.Add("x-twitter-active-user", "yes");
                headers.Add("x-twitter-client-language", "en");
                var cookie = CookieService.GetAllCookies().FirstOrDefault(c => c.Name == "ct0");
                if (cookie != null) headers.Add("x-csrf-token", cookie.Value);
            }

            return await GetRequestAsync(url, referer, headers);
        }

        private async Task<string> GetUserByScreenNameAsync()
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

        private async Task<string> GetUserTweetsAsync(byte type, string cursor)
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
                if (!TestRange(Blog.PageSize, 1, 20))
                {
                    Blog.PageSize = 20;
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

            await IsBlogOnlineAsync();
            if (!Blog.Online)
            {
                PostQueue.CompleteAdding();
                jsonQueue.CompleteAdding();
                return true;
            }

            Blog.Posts = twUser.Data.User.Legacy.StatusesCount;
            if (!TestRange(Blog.PageSize, 1, 20)) Blog.PageSize = 20;

            int currentPage = (Blog.Posts > 3200) ? (Blog.Posts - 3200) / 20 + 3200 / Blog.PageSize + 1 : Blog.Posts / Blog.PageSize + 1;
            if (Blog.Posts > 3200) currentPage += 20;
            int pageNo = 1;

            while (true)
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

                await CrawlPageAsync(pageNo);

                if (currentPage > 0)
                {
                    currentPage--;
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

        private static List<Entry> GetEntries(TimelineTweets response)
        {
            List<Entry> entries;
            int index = (response.Timeline.Instructions[0].Type == "TimelineClearCache" && response.Timeline.Instructions.Count > 1) ? 1 : 0;
            entries = response.Timeline.Instructions[index].Entries;

            foreach (var entry in entries)
            {
                if (entry.Content.ItemContent?.TweetResults?.Result?.Typename == "TweetWithVisibilityResults")
                {
                    entry.Content.ItemContent.TweetResults.Result = entry.Content.ItemContent.TweetResults.Result.TweetWithVisibilityResults;
                }
            } 

            return entries;
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
                    string document = await GetUserTweetsAsync((byte)(oldestApiPost == null ? 2 : 3), cursor);
                    var response = ConvertJsonToClassNew<TimelineTweets>(document);
                    var entries = GetEntries(response);

                    if (highestId == 0)
                    {
                        highestId = ulong.Parse(entries.Where(w => w.Content?.EntryType == "TimelineTimelineItem")
                            .Max(x => x.Content?.ItemContent.TweetResults.Result.Legacy.IdStr) ?? "0");
                        if (highestId > 0)
                            Blog.LatestPost = DateTime.ParseExact(
                                entries.Find(f => f.EntryId == $"tweet-{highestId}").Content.ItemContent.TweetResults.Result.Legacy.CreatedAt, twitterDateTemplate, new CultureInfo("en-US"));
                    }

                    bool noNewCursor = false;
                    //if (response.GlobalObjects.Tweets.Count == 1 ||
                    //    (oldestApiPost == null && pageNo * Blog.PageSize >= 3200 && response.GlobalObjects.Tweets.Count < Blog.PageSize + 2))
                    //{
                    //    DateTime createdAt = response.GlobalObjects.Tweets.Count > 1
                    //        ? DateTime.ParseExact(response.GlobalObjects.Tweets.OrderBy(x => x.Key).First().Value.CreatedAt, twitterDateTemplate, new CultureInfo("en-US"))
                    //        : DateTime.Today;
                    //    oldestApiPost = createdAt.ToString("yyyy-MM-dd", new CultureInfo("en-US"));
                    //    cursor = null;
                    //    noNewCursor = response.GlobalObjects.Tweets.Count > 1;
                    //    if (response.GlobalObjects.Tweets.Count <= 1)
                    //    {
                    //        document = await GetUserTweetsAsync(3, cursor);
                    //        response = ConvertJsonToClassNew<TimelineTweets>(document);
                    //        entries = GetEntries(response);
                    //    }
                    //}

                    completeGrab = CheckPostAge(response);

                    if (response.Timeline.Instructions.Last().Type == "TimelineReplaceEntries") Debug.WriteLine("");

                    //Entry entry = (response.Timeline.Instructions.Last().Type == "TimelineReplaceEntries") ? response.Timeline.Instructions.Last().ReplaceEntry.Entry : entries.Last();
                    Entry entry = (response.Timeline.Instructions.Last().Type == "TimelineReplaceEntries") ? null : entries.Last();
                    var cursorNew = entry.Content.Value;
                    if (cursor == cursorNew) completeGrab = false;
                    if (!noNewCursor) cursor = cursorNew;

                    await AddUrlsToDownloadListAsync(response);

                    numberOfPostsCrawled += oldestApiPost == null ? Blog.PageSize : 20;
                    UpdateProgressQueueInformation(Resources.ProgressGetUrl2Long, Math.Min(numberOfPostsCrawled, Blog.Posts), Blog.Posts);
                    retries = 200;
                }
                catch (WebException webException) when (webException.Response != null)
                {
                    if (HandleLimitExceededWebException(webException))
                    {
                        //incompleteCrawl = true;
                        retries++;
                        handle429 = ((HttpWebResponse)webException?.Response).Headers["x-rate-limit-reset"];
                    }
                    if (((HttpWebResponse)webException?.Response).StatusCode == HttpStatusCode.Forbidden)
                    {
                        Logger.Error("TwitterCrawler.CrawlPageAsync: {0}", string.Format(CultureInfo.CurrentCulture, Resources.ProtectedBlog, Blog.Name));
                        ShellService.ShowError(webException, Resources.ProtectedBlog, Blog.Name);
                        completeGrab = false;
                        retries = 403;
                    }
                }
                catch (TimeoutException timeoutException)
                {
                    //incompleteCrawl = true;
                    retries++;
                    HandleTimeoutException(timeoutException, Resources.Crawling);
                    Thread.Sleep(3000);
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
                        Progress.Report(new DownloadProgress() { Progress = string.Format("waiting until {0}", dto.ToLocalTime().ToString()) });
                        var cancelled = Ct.WaitHandle.WaitOne((int)dto.Subtract(DateTime.Now).TotalMilliseconds);
                        if (cancelled) retries = 400;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("TwitterCrawler.CrawlPageAsync: error while handling 429: {0}", e);
                        retries = 400;
                    }
                }
            } while (retries < maxRetries);

            if (retries <= maxRetries || retries >= 400)
            {
                incompleteCrawl = true;
            }
        }

        private bool PostWithinTimeSpan(Tweet post)
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
                    DateTimeStyles.None).AddDays(1);
                downloadToUnixTime = new DateTimeOffset(downloadTo).ToUnixTimeSeconds();
            }

            DateTime createdAt = DateTime.ParseExact(post.Legacy.CreatedAt, twitterDateTemplate, new CultureInfo("en-US"));
            long postTime = ((DateTimeOffset)createdAt).ToUnixTimeSeconds();
            return downloadFromUnixTime <= postTime && postTime < downloadToUnixTime;
        }

        private bool CheckPostAge(TimelineTweets response)
        {
            var entries = GetEntries(response);
            if (entries == null || entries.Count == 0) return false;
            var id = entries[0]?.SortIndex;
            if (id == null) return false;
            ulong highestPostId;
            _ = ulong.TryParse(id, out highestPostId);

            return highestPostId >= GetLastPostId();
        }

        private async Task GetAlreadyExistingCrawlerDataFilesAsync()
        {
            foreach (var filepath in Directory.GetFiles(Blog.DownloadLocation(), "*.json"))
            {
                existingCrawlerData.Add(Path.GetFileName(filepath));
            }
            await Task.CompletedTask;
        }

        private void AddToJsonQueue(CrawlerData<Tweet> addToList)
        {
            if (!Blog.DumpCrawlerData) { return; }

            lock (existingCrawlerDataLock)
            {
                if (Blog.ForceRescan || !existingCrawlerData.Contains(addToList.Filename))
                {
                    jsonQueue.Add(addToList);
                    existingCrawlerData.Add(addToList.Filename);
                }
            }
        }

        private async Task AddUrlsToDownloadListAsync(TimelineTweets document)
        {
            var lastPostId = GetLastPostId();
            var entries = GetEntries(document);
            foreach (Entry entry in entries)
            {
                var cursorType = entry.Content.CursorType;
                if (cursorType != null) continue;
                if (!entry.EntryId.ToLower().StartsWith("tweet-", StringComparison.InvariantCultureIgnoreCase) &&
                    !entry.EntryId.ToLower().StartsWith("sq-i-t-", StringComparison.InvariantCultureIgnoreCase)) continue;

                Tweet post = entry.Content.ItemContent.TweetResults.Result;
                try
                {
                    if (CheckIfShouldStop()) { break; }
                    CheckIfShouldPause();
                    if (lastPostId > 0 && ulong.TryParse(post.Legacy.IdStr, out var postId) && postId < lastPostId) { continue; }
                    if (!PostWithinTimeSpan(post)) { continue; }
                    if (!CheckIfContainsTaggedPost(post)) { continue; }
                    if (!CheckIfDownloadRebloggedPosts(post)) { continue; }

                    try
                    {
                        AddPhotoUrlToDownloadList(post);
                        AddVideoUrlToDownloadList(post);
                        AddGifUrlToDownloadList(post);
                        AddTextUrlToDownloadList(post);
                    }
                    catch (NullReferenceException e)
                    {
                        Logger.Verbose("TwitterCrawler.AddUrlsToDownloadListAsync: {0}", e);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("TwitterCrawler.AddUrlsToDownloadListAsync: {0}", e);
                    ShellService.ShowError(e, "{0}: Error parsing tweet!", Blog.Name);
                }
            }
            await Task.CompletedTask;
        }

        private bool CheckIfDownloadRebloggedPosts(Tweet post)
        {
            var rsr = post.Legacy.RetweetedStatusResult;
            return Blog.DownloadRebloggedPosts || rsr == null || rsr.Result.Core.UserResults.Result.RestId == post.Core.UserResults.Result.RestId;
        }

        private bool CheckIfContainsTaggedPost(Tweet post)
        {
            return Tags.Count == 0 || post.Legacy.Entities.Hashtags.Any(x => Tags.Contains(x.Text));
        }

        private string ImageSizeForSearching()
        {
            return (ShellService.Settings.ImageSizeCategory == "best") ? "orig" : ShellService.Settings.ImageSizeCategory;
        }

        private string GetUrlForPreferredImageSize(string url)
        {
            // <base_url>?format=<format>&name=<name>
            // https://pbs.twimg.com/media/abcdefghi?format=jpg&name=large

            var filename = url.Split('/').Last();
            var path = url.Replace(filename, "");
            var ext = Path.GetExtension(filename).Replace(".", "");
            if (ext.Length == 0) return url;
            return string.Format("{0}{1}?format={2}&name={3}", path, Path.GetFileNameWithoutExtension(filename), ext, ImageSizeForSearching());
        }

        private static List<Media> GetMedia(Tweet post)
        {
            if (post.Legacy.ExtendedEntities != null)
            {
                foreach (var item in post.Legacy.ExtendedEntities.Media)
                {
                    if (!(item.Type == "photo" || item.Type == "video" || item.Type == "animated_gif"))
                        throw new Exception("unknown new media type: " + item.Type);
                }
                return post.Legacy.ExtendedEntities.Media;
            }
            return post.Legacy.Entities.Media;
        }

        private void AddPhotoUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadPhoto) return;
            if (!BlogDownloadReplies && !string.IsNullOrEmpty(post.Legacy.InReplyToStatusIdStr)) return;

            var media = GetMedia(post);

            if (media?.FirstOrDefault()?.Type == "photo")
            {
                AddPhotoUrl(post, media);
            }
        }

        private void AddVideoUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail) return;
            if (!BlogDownloadReplies && !string.IsNullOrEmpty(post.Legacy.InReplyToStatusIdStr)) return;

            var media = GetMedia(post);

            if (media?.FirstOrDefault()?.Type == "video")
            {
                AddVideoUrl(post, media);
            }
        }

        private void AddTextUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadText) return;
            if (!(post.Legacy.Entities == null || post.Legacy.Entities.Media == null || post.Legacy.Entities.Media.Count == 0)) return;
            if (!BlogDownloadReplies && !string.IsNullOrEmpty(post.Legacy.InReplyToStatusIdStr)) return;

            var body = GetTweetText(post);
            AddToDownloadList(new TextPost(body, post.Legacy.IdStr));
            AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(post.Legacy.IdStr, ".json"), post));
        }

        private static string GetTweetText(Tweet post)
        {
            var dict = new Dictionary<string, string>()
            {
                {"id", post.RestId },
                { "text", post.Legacy.FullText },
                { "url", post.Legacy.Url }
            };
            var json = JsonConvert.SerializeObject(dict);
            return json;
        }

        private void AddGifUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadPhoto || Blog.SkipGif) return;
            if (!BlogDownloadReplies && !string.IsNullOrEmpty(post.Legacy.InReplyToStatusIdStr)) return;

            var media = GetMedia(post);

            if (media?.FirstOrDefault()?.Type == "animated_gif")
            {
                AddGifUrl(post, media[0]);
            }
        }

        private void AddGifUrl(Tweet post, Media media)
        {
            var item = media.VideoInfo.Variants[0];
            var urlPrepared = item.Url.IndexOf('?') > 0 ? item.Url.Substring(0, item.Url.IndexOf('?')) : item.Url;
            AddToDownloadList(new VideoPost(item.Url, post.Legacy.IdStr, UnixTimestamp(post).ToString(), BuildFileName(urlPrepared, post, "gif", -1)));
            AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
        }

        private static int UnixTimestamp(Tweet post)
        {
            long postTime = ((DateTimeOffset)GetDate(post)).ToUnixTimeSeconds();
            return (int)postTime;
        }

        private static DateTime GetDate(Tweet post)
        {
            return DateTime.ParseExact(post.Legacy.CreatedAt, twitterDateTemplate, new CultureInfo("en-US"));
        }

        private void AddPhotoUrl(Tweet post, List<Media> media)
        {
            for (int i = 0; i < media.Count; i++)
            {
                var imageUrl = media[i].MediaUrlHttps;
                var imageUrlConverted = GetUrlForPreferredImageSize(imageUrl);
                var index = media.Count > 1 ? i + 1 : -1;
                var filename = BuildFileName(imageUrl, post, "photo", index);
                AddToDownloadList(new PhotoPost(imageUrlConverted, "", post.Legacy.IdStr, UnixTimestamp(post).ToString(), filename));
            }
            var imageUrl2 = media[0].MediaUrlHttps;
            AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(imageUrl2.Split('/').Last(), ".json"), post));
        }

        private void AddVideoUrl(Tweet post, List<Media> media)
        {
            var size = ShellService.Settings.VideoSize;

            int max = media[0].VideoInfo.Variants.Where(v => v.ContentType == "video/mp4").Max(v => v.Bitrate.GetValueOrDefault());
            var item = media[0].VideoInfo.Variants.First(v => v.Bitrate == max);
            var urlPrepared = item.Url.IndexOf('?') > 0 ? item.Url.Substring(0, item.Url.IndexOf('?')) : item.Url;

            if (Blog.DownloadVideo)
            {
                AddToDownloadList(new VideoPost(item.Url, post.Legacy.IdStr, UnixTimestamp(post).ToString(), BuildFileName(urlPrepared, post, "video", -1)));
                AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
            }

            if (Blog.DownloadVideoThumbnail)
            {
                var imageUrl = media[0].MediaUrlHttps;
                var filename = FileName(imageUrl);
                if (!string.Equals(Path.GetFileNameWithoutExtension(FileName(urlPrepared)), Path.GetFileNameWithoutExtension(filename), StringComparison.OrdinalIgnoreCase))
                {
                    filename = Path.GetFileNameWithoutExtension(FileName(urlPrepared)) + "_" + filename;
                }
                filename = BuildFileName(filename, post, "photo", -1);
                AddToDownloadList(new PhotoPost(imageUrl, "", post.Legacy.IdStr, UnixTimestamp(post).ToString(), filename));
                if (!Blog.DownloadVideo)
                {
                    AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
                }
            }
        }

        private static List<string> GetTags(Tweet post)
        {
            var ht = post.Legacy.Entities.Hashtags;
            return ht == null ? new List<string>() : ht.Select(h => h.Text).ToList();
        }

        private string BuildFileName(string url, Tweet post, string type, int index)
        {
            var reblogged = post.Legacy.RetweetedStatusResult != null && post.Legacy.RetweetedStatusResult.Result.Core.UserResults.Result.RestId != post.Legacy.UserIdStr;
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
            if (reblogged)
            {
                reblogName = post.Legacy.RetweetedStatusResult.Result.Core.UserResults.Result.Legacy.Name;
                reblogId = post.Legacy.RetweetedStatusResult.Result.Legacy.IdStr;
            }
            var tags = GetTags(post);
            return BuildFileNameCore(url, post.Legacy.UserIdStr, GetDate(post), UnixTimestamp(post), index, type, post.Legacy.IdStr,
                tags, "", GetTitle(post.Legacy.FullText, tags), reblogName, "", reblogId);
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
