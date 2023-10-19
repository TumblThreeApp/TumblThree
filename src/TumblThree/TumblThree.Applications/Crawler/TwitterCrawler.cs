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
using TumblThree.Applications.DataModels.Twitter.TwitterUser;
using TumblThree.Applications.DataModels.Twitter.TimelineTweets;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

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
        private const string graphQlTokenSearchTimeline = "NA567V_8AFwu0cZEkAAKcw";
        private const string BearerToken = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";

        private readonly IDownloader downloader;
        private readonly IPostQueue<CrawlerData<Tweet>> jsonQueue;
        private readonly IList<string> existingCrawlerData = new List<string>();
        private readonly object existingCrawlerDataLock = new object();
        private readonly ICrawlerDataDownloader crawlerDataDownloader;

        private bool completeGrab = true;
        private bool incompleteCrawl;

        private SemaphoreSlim semaphoreSlim;

        private int numberOfPostsCrawled;

        private TwitterUser twUser;
        private SemaphoreSlim twUserMutex = new SemaphoreSlim(1);
        private string guestToken;
        private ulong highestId;
        private string cursor;
        private string oldestApiPost;
        private string oldestApiPostPrevious;

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
                if (!string.IsNullOrEmpty(twUser.Errors?.FirstOrDefault()?.Message))
                {
                    Logger.Warning("TwitterCrawler.IsBlogOnlineAsync: {0} ({1}): {2}", Blog.Name, GetCollectionName(Blog), twUser.Errors?[0]?.Message);
                    ShellService.ShowError(null, (twUser.Errors?[0]?.Code == 63 ? $"{Blog.Name} ({GetCollectionName(Blog)}): " : "") + twUser.Errors?[0]?.Message);
                    Blog.Online = false;
                }
                else if (twUser.Data.User.Typename != "User")
                {
                    Logger.Warning("TwitterCrawler.IsBlogOnlineAsync: {0}: {1}, {2}", Blog.Name, twUser.Data.User.Typename, twUser.Data.User.Reason);
                    ShellService.ShowError(null, "{0}: {1}, {2}", Blog.Name, twUser.Data.User.Typename, twUser.Data.User.Reason);
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
                else if (HandleLimitExceededWebException(webException, LimitExceededSource.twitter))
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
            catch (Exception ex)
            {
                Logger.Error("TwitterCrawler:IsBlogOnlineAsync: {0}", ex);
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

            twUser = await GetTwUser();

            if (twUser.Errors?.Count > 0) throw new Exception($"{Blog.Name}: {twUser.Errors[0].Message}");

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
                    var includeReplies = Blog.DownloadReplies.ToString().ToLower();
                    url = string.Format("https://twitter.com/i/api/graphql/{0}/UserTweets" +
                        "?variables=%7B%22userId%22%3A%22{1}%22%2C%22count%22%3A{2}{3}%2C%22includePromotedContent%22%3Atrue%2C%22withQuickPromoteEligibilityTweetFields%22%3Atrue%2C%22withVoice%22%3Atrue%2C%22withV2Timeline%22%3Atrue%7D&features=%7B%22rweb_lists_timeline_redesign_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Atrue%2C%22responsive_web_twitter_article_tweet_consumption_enabled%22%3Afalse%2C%22tweet_awards_web_tipping_enabled%22%3Afalse%2C%22freedom_of_speech_not_reach_fetch_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Atrue%2C%22longform_notetweets_rich_text_read_enabled%22%3Atrue%2C%22longform_notetweets_inline_media_enabled%22%3Atrue%2C%22responsive_web_media_download_video_enabled%22%3Afalse%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D&fieldToggles=%7B%22withAuxiliaryUserLabels%22%3Afalse%2C%22withArticleRichContentState%22%3Afalse%7D",
                        graphQlTokenUserTweets, restId, pageSize, cursor);
                    break;
                case 3:
                    if (!string.IsNullOrEmpty(cursor)) cursor = string.Format("%2C%22cursor%22%3A%22{0}%22", cursor.Replace("+", "%2B"));
                    url = string.Format("https://twitter.com/i/api/graphql/{0}/SearchTimeline" +
                        "?variables=%7B%22rawQuery%22%3A%22from%3A{1}%20until%3A{2}%22%2C%22count%22%3A{3}{4}%2C%22product%22%3A%22Latest%22%7D&features=%7B%22rweb_lists_timeline_redesign_enabled%22%3Atrue%2C%22responsive_web_graphql_exclude_directive_enabled%22%3Atrue%2C%22verified_phone_label_enabled%22%3Afalse%2C%22creator_subscriptions_tweet_preview_api_enabled%22%3Atrue%2C%22responsive_web_graphql_timeline_navigation_enabled%22%3Atrue%2C%22responsive_web_graphql_skip_user_profile_image_extensions_enabled%22%3Afalse%2C%22tweetypie_unmention_optimization_enabled%22%3Atrue%2C%22responsive_web_edit_tweet_api_enabled%22%3Atrue%2C%22graphql_is_translatable_rweb_tweet_is_translatable_enabled%22%3Atrue%2C%22view_counts_everywhere_api_enabled%22%3Atrue%2C%22longform_notetweets_consumption_enabled%22%3Atrue%2C%22responsive_web_twitter_article_tweet_consumption_enabled%22%3Afalse%2C%22tweet_awards_web_tipping_enabled%22%3Afalse%2C%22freedom_of_speech_not_reach_fetch_enabled%22%3Atrue%2C%22standardized_nudges_misinfo%22%3Atrue%2C%22tweet_with_visibility_results_prefer_gql_limited_actions_policy_enabled%22%3Atrue%2C%22longform_notetweets_rich_text_read_enabled%22%3Atrue%2C%22longform_notetweets_inline_media_enabled%22%3Atrue%2C%22responsive_web_media_download_video_enabled%22%3Afalse%2C%22responsive_web_enhance_cards_enabled%22%3Afalse%7D&fieldToggles=%7B%22withAuxiliaryUserLabels%22%3Afalse%2C%22withArticleRichContentState%22%3Afalse%7D",
                        graphQlTokenSearchTimeline, Blog.Name, oldestApiPost, pageSize, cursor);
                    break;
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
                    if (ShellService.Settings.LimitConnectionsTwitterApi)
                    {
                        CrawlerService.TimeconstraintTwitterApi.Acquire();
                    }
                    var headers = new Dictionary<string, string>();
                    headers.Add("Origin", "https://twitter.com");
                    headers.Add("Authorization", "Bearer " + BearerToken);
                    headers.Add("Accept-Language", "en-US,en;q=0.5");
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

            if (ShellService.Settings.LimitConnectionsTwitterApi)
            {
                CrawlerService.TimeconstraintTwitterApi.Acquire();
            }

            var referer = type < 3 ? Blog.Url : $"https://twitter.com/search?q=from%3A{Blog.Name}%20until%3A{oldestApiPost}&src=typed_query&f=latest";

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
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);

            GenerateTags();

            bool error = false;
            try
            {
                await IsBlogOnlineAsync();
            }
            catch (Exception)
            {
                error = true;
            }
            if (error || !Blog.Online)
            {
                PostQueue.CompleteAdding();
                jsonQueue.CompleteAdding();
                return true;
            }

            Blog.Posts = twUser.Data.User.Legacy.StatusesCount;
            if (!TestRange(Blog.PageSize, 1, 20)) Blog.PageSize = 20;

            int expectedPages = (Blog.Posts > 3200) ? (Blog.Posts - 3200) / 20 + 3200 / Blog.PageSize + 1 : Blog.Posts / Blog.PageSize + 1;
            if (Blog.Posts > 3200) expectedPages += 20;
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

        private static List<Entry> GetEntries(TimelineTweets response, bool includePinEntry = false, bool includeCursors = false)
        {
            if (response.Data.User?.Result == null && response.Data.SearchByRawQuery == null) throw new Exception("NoPostsYet");
            if (response.Data.User?.Result?.Typename == "UserUnavailable") throw new Exception("UserUnavailable");
            if (!string.IsNullOrEmpty(response.Errors?.FirstOrDefault()?.Message))
            {
                var twErrors = $"Twitter Errors:{Environment.NewLine}" + string.Join("," + Environment.NewLine, response.Errors.Select(s => $"{s.Name}: {s.Message}").ToHashSet<string>());
                throw new Exception($"{response.Errors[0].Name}: {response.Errors[0].Message}", new Exception(twErrors)) { Source = "TwitterError" };
            }

            List<Entry> entries = response.Timeline.Instructions.Where(x => x.Type == "TimelineAddEntries").FirstOrDefault()?.Entries ?? new List<Entry>();
            if (includePinEntry)
            {
                var pinEntry = response.Timeline.Instructions.Where(x => x.Type == "TimelinePinEntry").FirstOrDefault();
                if (pinEntry != null)
                {
                    entries.Insert(0, pinEntry.Entry);
                }
            }
            entries = entries.Where(x => x.Content.EntryType == "TimelineTimelineItem" || x.Content.EntryType == "TimelineTimelineModule" ||
                    includeCursors && x.Content.EntryType == "TimelineTimelineCursor")
                .Where(x => x.Content.EntryType == "TimelineTimelineCursor" ||
                    x.Content.EntryType == "TimelineTimelineItem" && x.Content.ItemContent.TweetDisplayType == "Tweet" && x.Content.ItemContent.TweetResults.Tweet.Legacy != null ||
                    x.Content.EntryType == "TimelineTimelineModule" && x.Content.DisplayType.EndsWith("Conversation") &&
                        x.Content.Items.Any(a => a.Item.ItemContent.TweetResults.Tweet.Legacy != null)
                ).ToList();

            List<Entry> replaceEntries = response.Timeline.Instructions.Where(x => x.Type == "TimelineReplaceEntry").Select(x => x.Entry).ToList();
            if (replaceEntries?.Count > 0)
                entries.AddRange(replaceEntries);

            return entries;
        }

        private static IEnumerable<Entry> GetPostEntries(List<Entry> entries)
        {
            return entries.Where(w => w.Content?.EntryType == "TimelineTimelineItem" && w.Content.ClientEventInfo?.Element == "tweet" ||
                                      w.Content?.EntryType == "TimelineTimelineModule" && (w.Content.DisplayType?.EndsWith("Conversation") ?? false));
        }

        private static List<Tweet> SelectTweets(Entry entry)
        {
            List<ItemContent> list = new List<ItemContent>();
            if (entry?.Content?.ItemContent != null) list.Add(entry.Content.ItemContent);
            if (entry?.Content?.Items != null) list.AddRange(entry.Content.Items.Select(s => s.Item.ItemContent));
            return list.Select(s => s.TweetResults.Tweet).ToList();
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
                    var entries = GetEntries(response, pageNo == 1, true);

                    if (highestId == 0)
                    {
                        highestId = ulong.Parse(GetPostEntries(entries)
                            .Max(x => x.Content?.ItemContent?.TweetResults.Tweet.Legacy.IdStr ?? x.Content?.Items?.LastOrDefault()?.Item.ItemContent.TweetResults.Tweet.Legacy.IdStr) ?? "0");
                        if (highestId > 0)
                        {
                            Entry entry = entries.Find(f => f.EntryId == $"tweet-{highestId}" || 
                                f.EntryId == f.Content.Items.Where(w => w.EntryId.EndsWith($"tweet-{highestId}")).FirstOrDefault()?.EntryId.Replace($"-tweet-{highestId}", ""));
                            Blog.LatestPost = DateTime.ParseExact(
                                (entry.Content?.ItemContent ?? entry.Content?.Items?.LastOrDefault()?.Item.ItemContent)?.TweetResults.Tweet.Legacy.CreatedAt,
                                twitterDateTemplate, new CultureInfo("en-US"));
                        }
                    }

                    bool noNewCursor = false;
                    var lastEntry = GetPostEntries(entries).LastOrDefault();
                    var createdAtField = SelectTweets(lastEntry)?.LastOrDefault()?.Legacy.CreatedAt;
                    if (createdAtField != null)
                    {
                        DateTime createdAt = DateTime.ParseExact(createdAtField, twitterDateTemplate, new CultureInfo("en-US"));
                        oldestApiPostPrevious = createdAt.ToString("yyyy-MM-dd", new CultureInfo("en-US"));
                    }
                    if (oldestApiPost == null && entries.Count <= 2)
                    {
                        oldestApiPost = oldestApiPostPrevious;
                        cursor = null;
                        noNewCursor = true;
                    }
                    else
                    {
                        completeGrab = CheckPostAge(response);
                    }

                    if (response.Timeline.Instructions.Last().Type == "TimelineReplaceEntries") Debug.WriteLine("");

                    var cursorNew = entries.Last().Content.Value;
                    if (cursor == cursorNew) completeGrab = false;
                    if (!noNewCursor) cursor = cursorNew;

                    await AddUrlsToDownloadListAsync(entries);

                    numberOfPostsCrawled += oldestApiPost == null ? Blog.PageSize : 20;
                    UpdateProgressQueueInformation(Resources.ProgressGetUrl2Long, Math.Min(numberOfPostsCrawled, Blog.Posts), Blog.Posts);
                    retries = 200;
                }
                catch (WebException webException) when (webException.Response != null)
                {
                    if (HandleLimitExceededWebException(webException, LimitExceededSource.twitter))
                    {
                        //incompleteCrawl = true;
                        retries++;
                        handle429 = ((HttpWebResponse)webException?.Response).Headers["x-rate-limit-reset"];
                    }
                    if (((HttpWebResponse)webException?.Response).StatusCode == HttpStatusCode.Forbidden)
                    {
                        Logger.Error("TwitterCrawler.CrawlPageAsync: {0}", string.Format(CultureInfo.CurrentCulture, Resources.ProtectedBlog, $"{Blog.Name} ({GetCollectionName(Blog)})"));
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
                    Logger.Error("TwitterCrawler.CrawlPageAsync: {0}", string.Format(CultureInfo.CurrentCulture, Resources.ProtectedBlog, Blog.Name));
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
                    Logger.Error("TwitterCrawler.CrawlPageAsync: {0}: {1}", Blog.Name, e.InnerException.Message);
                    ShellService.ShowError(e, "{0}: Twitter Error: {1}", Blog.Name, e.Message);
                    retries++;
                    Thread.Sleep(2000);
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
            // get id of the first tweet, but not a pinned one
            var id = SelectTweets(entries.FirstOrDefault())?.LastOrDefault()?.RestId;
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

        private async Task AddUrlsToDownloadListAsync(List<Entry> entries)
        {
            var lastPostId = GetLastPostId();
            foreach (Entry entry in entries)
            {
                var cursorType = entry.Content.CursorType;
                if (cursorType != null) continue;
                if (!entry.EntryId.StartsWith("tweet-", StringComparison.InvariantCultureIgnoreCase) &&
                    !entry.EntryId.StartsWith("sq-i-t-", StringComparison.InvariantCultureIgnoreCase) &&
                    !entry.EntryId.StartsWith("profile-conversation", StringComparison.InvariantCultureIgnoreCase)) continue;

                foreach (Tweet post in SelectTweets(entry))
                {
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
            }
            await Task.CompletedTask;
        }

        private bool CheckIfDownloadReplyPost(Tweet post)
        {
            return Blog.DownloadReplies || string.IsNullOrEmpty(post.Legacy.InReplyToStatusIdStr);
        }

        private bool CheckIfDownloadRebloggedPosts(Tweet post)
        {
            var rsr = post.Legacy.RetweetedStatusResult;
            return Blog.DownloadRebloggedPosts || rsr == null || rsr.Result.User.RestId == post.Core.UserResults.Result.RestId;
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
            foreach (var item in post.Legacy.Entities.Media)
            {
                if (!(item.Type == "photo" || item.Type == "video" || item.Type == "animated_gif"))
                    throw new Exception("unknown new media type: " + item.Type);
            }
            return post.Legacy.Entities.Media;
        }

        private void AddGifUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadPhoto || Blog.SkipGif) return;
            if (!CheckIfDownloadReplyPost(post)) return;

            var media = GetMedia(post);
            AddGifUrl(post, media);
        }

        private void AddPhotoUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadPhoto) return;
            if (!CheckIfDownloadReplyPost(post)) return;

            var media = GetMedia(post);
            AddPhotoUrl(post, media);
        }

        private void AddVideoUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail) return;
            if (!CheckIfDownloadReplyPost(post)) return;

            var media = GetMedia(post);
            AddVideoUrl(post, media);
        }

        private void AddTextUrlToDownloadList(Tweet post)
        {
            if (!Blog.DownloadText) return;
            if (!CheckIfDownloadReplyPost(post)) return;

            var body = GetTweetText(post);
            if (string.IsNullOrEmpty(body)) return;
            AddToDownloadList(new TextPost(body, post.Legacy.IdStr));
            if (post.Legacy.Entities?.Media?.Count == 0 || !(Blog.DownloadPhoto || Blog.DownloadVideo))
            {
                AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(post.Legacy.IdStr, ".json"), post));
            }
        }

        private static DataModels.Twitter.TimelineTweets.User GetRetweetedUser(Tweet post)
        {
            return (post.Legacy.RetweetedStatusResult.Result.Core ?? post.Legacy.RetweetedStatusResult.Result.TweetWithVisibilityResults.Core).UserResults.Result;
        }

        private static Tweet GetRetweetedTweet(Tweet post)
        {
            return post.Legacy.RetweetedStatusResult.Result.Legacy is null ? post.Legacy.RetweetedStatusResult.Result.TweetWithVisibilityResults : post.Legacy.RetweetedStatusResult.Result;
        }

        private static string GetTweetText(Tweet post)
        {
            var dateString = GetDate(post).ToString("u");
            // shortened FullText can happen for foreign and own retweets
            var reblogged = post.Legacy.RetweetedStatusResult != null; // && GetUser(post).RestId != post.Legacy.UserIdStr;
            var text = reblogged ? $"RT @{GetRetweetedUser(post).Legacy.ScreenName}: " + GetRetweetedTweet(post).Legacy.FullText : post.Legacy.FullText;
            if (post.Legacy.Entities?.Media?.Any(x => x.Url.Equals(text)) ?? false) return "";
            object url = post.Legacy.Url;
            if (url is null) url = post.Legacy.Entities?.Urls?.Select(x => x.ExpandedUrl).ToList();
            url = (post.Legacy.Entities?.Urls?.Count == 1) ? ((List<string>)url)[0] : url;
            if (url is null) url = $"https://twitter.com/{post.User.Legacy.ScreenName}/status/{post.RestId}";
            var dict = new Dictionary<string, object>()
            {
                { "id", post.RestId },
                { "date", dateString },
                { "text", text },
                { "url", url }
            };
            var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
            return json;
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

        private void AddGifUrl(Tweet post, List<Media> media)
        {
            for (int i = 0; i < media.Count; i++)
            {
                if (media[i].Type != "animated_gif") continue;

                var item = media[i].VideoInfo.Variants[0];
                var urlPrepared = item.Url.IndexOf('?') > 0 ? item.Url.Substring(0, item.Url.IndexOf('?')) : item.Url;
                AddToDownloadList(new VideoPost(item.Url, post.Legacy.IdStr, UnixTimestamp(post).ToString(), BuildFileName(urlPrepared, post, "gif", -1)));
                if (i == 0)
                {
                    AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddPhotoUrl(Tweet post, List<Media> media)
        {
            for (int i = 0; i < media.Count; i++)
            {
                if (media[i].Type != "photo") continue;

                var imageUrl = media[i].MediaUrlHttps;
                var imageUrlConverted = GetUrlForPreferredImageSize(imageUrl);
                var index = media.Count > 1 ? i + 1 : -1;
                var filename = BuildFileName(imageUrl, post, "photo", index);
                AddToDownloadList(new PhotoPost(imageUrlConverted, "", post.Legacy.IdStr, UnixTimestamp(post).ToString(), filename));
                if (i == 0)
                {
                    var urlPrepared = imageUrl.IndexOf('?') > 0 ? imageUrl.Substring(0, imageUrl.IndexOf('?')) : imageUrl;
                    AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddVideoUrl(Tweet post, List<Media> media)
        {
            for (int i = 0; i < media.Count; i++)
            {
                if (media[i].Type != "video") continue;

                var size = ShellService.Settings.VideoSize;

                int max = media[i].VideoInfo.Variants.Where(v => v.ContentType == "video/mp4").Max(v => v.Bitrate.GetValueOrDefault());
                var item = media[i].VideoInfo.Variants.First(v => v.Bitrate == max);
                var urlPrepared = item.Url.IndexOf('?') > 0 ? item.Url.Substring(0, item.Url.IndexOf('?')) : item.Url;

                if (Blog.DownloadVideo)
                {
                    AddToDownloadList(new VideoPost(item.Url, post.Legacy.IdStr, UnixTimestamp(post).ToString(), BuildFileName(urlPrepared, post, "video", -1)));
                    if (i == 0)
                        AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
                }

                if (Blog.DownloadVideoThumbnail)
                {
                    var imageUrl = media[i].MediaUrlHttps;
                    var filename = FileName(imageUrl);
                    if (!string.Equals(Path.GetFileNameWithoutExtension(FileName(urlPrepared)), Path.GetFileNameWithoutExtension(filename), StringComparison.OrdinalIgnoreCase))
                    {
                        filename = Path.GetFileNameWithoutExtension(FileName(urlPrepared)) + "_" + filename;
                    }
                    filename = BuildFileName(filename, post, "photo", -1);
                    AddToDownloadList(new PhotoPost(imageUrl, "", post.Legacy.IdStr, UnixTimestamp(post).ToString(), filename));
                    if (!Blog.DownloadVideo && i == 0)
                    {
                        AddToJsonQueue(new CrawlerData<Tweet>(Path.ChangeExtension(urlPrepared.Split('/').Last(), ".json"), post));
                    }
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
            var reblogged = false;
            DataModels.Twitter.TimelineTweets.User user = null;
            if (post.Legacy.RetweetedStatusResult != null)
            {
                user = GetRetweetedUser(post);
                reblogged = user.RestId != post.Legacy.UserIdStr;
            }
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
                reblogName = user.Legacy.ScreenName;
                reblogId = GetRetweetedTweet(post).Legacy.IdStr;
            }
            var tags = GetTags(post);
            return BuildFileNameCore(url, post.User.Legacy.ScreenName, GetDate(post), UnixTimestamp(post), index, type, post.Legacy.IdStr,
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
                twUserMutex.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
