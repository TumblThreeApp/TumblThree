using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TumblThree.Applications.Converter;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.DataModels.TumblrNPF;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.DataModels.TumblrSvcJson2.BlogInfo;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using Resources = TumblThree.Applications.Properties.Resources;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(TumblrHiddenBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrHiddenCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private static readonly Regex extractJsonFromPage = new Regex("window\\['___INITIAL_STATE___'\\] = (.*);");
        private static readonly Regex extractJsonFromPage2 = new Regex("id=\"___INITIAL_STATE___\">\\s*?({.*})\\s*?</script>", RegexOptions.Singleline);

        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<DataModels.TumblrApiJson.Post> tumblrJsonParser;
        private readonly IPostQueue<CrawlerData<Post>> jsonQueue;

        private string apiUrl;
        private string bearerToken;

        private bool incompleteCrawl;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;
        private readonly BlockingCollection<string> nextPage = new BlockingCollection<string>();
        private ulong highestId;
        private DateTime latestPost;

        private int numberOfPagesCrawled;

        public TumblrHiddenCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader,
            ITumblrToTextParser<DataModels.TumblrApiJson.Post> tumblrJsonParser, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IWebmshareParser webmshareParser, IUguuParser uguuParser, ICatBoxParser catboxParser,
            IPostQueue<AbstractPost> postQueue, IPostQueue<CrawlerData<Post>> jsonQueue, IBlog blog, IProgress<DownloadProgress> progress,
            IEnvironmentService environmentService, ILoginService loginService, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser,
                  webmshareParser, uguuParser, catboxParser, postQueue, blog, downloader, crawlerDataDownloader,
                  progress, environmentService, loginService, pt, ct)
        {
            this.downloader = downloader;
            this.tumblrJsonParser = tumblrJsonParser;
            this.jsonQueue = jsonQueue;
        }

        public override async Task IsBlogOnlineAsync()
        {
            if (!await CheckIfLoggedInAsync())
            {
                Logger.Error("TumblrHiddenCrawler:IsBlogOnlineAsync: {0}", "User not logged in");
                ShellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, Blog.Name);
                PostQueue.CompleteAdding();
                jsonQueue.CompleteAdding();
                return;
            }

            try
            {
                string document = await GetSvcPageAsync(GetStartUrl());
                Blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                if (HandleServiceUnavailableWebException(webException))
                {
                    Blog.Online = true;
                }
                else if (HandleNotFoundWebException(webException))
                {
                    Blog.Online = false;
                }
                else if (HandleLimitExceededWebException(webException))
                {
                    Blog.Online = true;
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
            if (!Blog.Online)
            {
                return;
            }
            try
            {
                var document = await GetSvcPageAsync(Blog.Url);
                _ = ExtractPosts(document, out var result);

                bearerToken = result.apiFetchStore.API_TOKEN;
                apiUrl = result.apiUrl;

                var blogName = Domain.Models.Blogs.Blog.ExtractName(Blog.Url);
                var url = $"{apiUrl}/v2/blog/{blogName}/info";
                document = await GetSvcPageAsync(url);
                var obj = JsonConvert.DeserializeObject<BlogInfo>(document);

                if (obj.Meta.Status == 200)
                {
                    Blog.Title = obj.Response.Blog.Title;
                    Blog.Description = obj.Response.Blog.Description;
                    Blog.Posts = obj.Response.Blog.Posts;
                }
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }
                HandleServiceUnavailableWebException(webException);
            }
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrHiddenCrawler.Crawl:Start");

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
                    //            if (DateTime.TryParse(post?.Date, out var latestPost)) Blog.LatestPost = latestPost;
                    if (highestId != 0) Blog.LatestPost = latestPost;
                }
            }

            Blog.Save();

            UpdateProgressQueueInformation(string.Empty);
        }

        protected override IEnumerable<int> GetPageNumbers()
        {
            if (!TestRange(Blog.PageSize, 1, 100))
            {
                Blog.PageSize = 100;
            }

            return string.IsNullOrEmpty(Blog.DownloadPages)
                ? Enumerable.Range(0, ShellService.Settings.ConcurrentScans)
                : RangeToSequence(Blog.DownloadPages);
        }

        private async Task<bool> GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);
            trackedTasks = new List<Task>();

            GenerateTags();

            nextPage.Add(GetStartUrl());

            foreach (int crawlerNumber in Enumerable.Range(0, ShellService.Settings.ConcurrentScans))
            {
                await semaphoreSlim.WaitAsync();
                trackedTasks.Add(CrawlPageAsync(crawlerNumber));
            }

            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats(GetLastPostId() != 0);

            return incompleteCrawl;
        }

        private async Task CrawlPageAsync(int crawlerNumber)
        {
            string document;
            CancellationTokenSource timeoutCts = null;
            CancellationTokenSource linkedCts = null;
            try
            {
                timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(Ct, timeoutCts.Token);

                foreach (var url in nextPage.GetConsumingEnumerable(linkedCts.Token))
                {
                    try
                    {
                        if (string.IsNullOrEmpty(url))
                            continue;

                        document = null;
                        try
                        {
                            document = await GetSvcPageAsync(url);
                        }
                        catch (WebException webEx)
                        {
                            if (HandleUnauthorizedWebExceptionRetry(webEx))
                            {
                                await FetchCookiesAgainAsync();
                                document = await GetSvcPageAsync(url);
                            }
                            else
                            {
                                throw;
                            }
                        }

                        var posts = ExtractPosts(document, out var result);

                        bearerToken = bearerToken ?? result.apiFetchStore.API_TOKEN;
                        apiUrl = apiUrl ?? result.apiUrl;

                        if (!posts.Any())
                        {
                            nextPage.CompleteAdding();
                            return;
                        }

                        if (highestId == 0)
                        {
                            highestId = Math.Max(Blog.LastId, posts.DefaultIfEmpty(new Post()).Max(x => ulong.Parse(x.Id)));
                            latestPost = DateTimeOffset.FromUnixTimeSeconds(posts.Where(x => !x.IsPinned).Select(s => s.Timestamp).FirstOrDefault()).UtcDateTime;
                        }

                        if (HasProperty(result, "PeeprRoute") && !HasProperty(result.PeeprRoute.initialTimeline, "nextLink") ||
                            HasProperty(result, "response") && !HasProperty(result.response, "_links"))
                        {
                            nextPage.CompleteAdding();
                        }
                        else
                        {
                            var nextLink = apiUrl + (string)(HasProperty(result, "PeeprRoute") ?
                                result.PeeprRoute.initialTimeline.nextLink.href : result.response._links.next.href);
                            nextPage.Add(nextLink);
                        }

                        if (CheckIfShouldStop()) { return; }

                        CheckIfShouldPause();

                        if (!CheckPostAge(posts)) { return; }

                        await DownloadPage(posts);

                        Interlocked.Increment(ref numberOfPagesCrawled);
                        UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                    }
                    catch (WebException webException)
                    {
                        if (HandleLimitExceededWebException(webException) ||
                            HandleUnauthorizedWebExceptionRetry(webException))
                        {
                            incompleteCrawl = true;
                        }
                        incompleteCrawl = true;
                        nextPage.Add(url);
                    }
                    catch (TimeoutException timeoutException)
                    {
                        HandleTimeoutException(timeoutException, Resources.Crawling);
                        incompleteCrawl = true;
                        nextPage.Add(url);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                incompleteCrawl = true;
            }
            catch (FormatException formatException)
            {
                Logger.Error("TumblrHiddenCrawler.CrawlPageAsync: {0}", formatException);
                ShellService.ShowError(formatException, "{0}: {1}", Blog.Name, formatException.Message);
                incompleteCrawl = true;
            }
            catch (Exception ex)
            {
                Logger.Error("TumblrHiddenCrawler.CrawlPageAsync: {0}", ex);
                incompleteCrawl = true;
            }
            finally
            {
                linkedCts?.Dispose();
                timeoutCts?.Dispose();
                semaphoreSlim.Release();
            }
        }

        protected async Task<string> GetRequestAsync(string url, string bearerToken)
        {
            AcquireTimeconstraintSvc();
            string[] cookieHosts = { "https://www.tumblr.com/" };
            return await RequestApiDataAsync(url, bearerToken, null, cookieHosts);
        }

        private static string ExtractJson(string document)
        {
            string json = document;
            if (!json.StartsWith("{"))
            {
                json = extractJsonFromPage.Match(document).Groups[1].Value;
                if (string.IsNullOrEmpty(json)) json = extractJsonFromPage2.Match(document).Groups[1].Value;
            }
            return json;
        }

        private int i = 1;

        private List<Post> ExtractPosts(string document, out dynamic result)
        {
            var json = ExtractJson(document);

            //TODO: if (json is null) throw new Exception("");

            result = JsonConvert.DeserializeObject<ExpandoObject>(json, new JsonSerializerSettings() { Converters = { new ExpandoObjectConverter() } });

            var postsList = (HasProperty(result, "PeeprRoute") ? (HasProperty(result.PeeprRoute, "initialTimeline") ? result.PeeprRoute.initialTimeline.objects : null) : result.response.posts) as IEnumerable<dynamic>;
            if (postsList is null) return new List<Post>();

            var serializerSettings = new JsonSerializerSettings()
            {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    Converters = { new ExpandoObjectConverter(), new FlexibleNamingConverter<Post>(), new FlexibleNamingConverter<DataModels.TumblrNPF.Blog>(),
                    new FlexibleNamingConverter<DataModels.TumblrNPF.Resources>(), new FlexibleNamingConverter<ClientSideAd>(), new FlexibleNamingConverter<Context>(),
                    new FlexibleNamingConverter<DataModels.TumblrNPF.Theme>(), new FlexibleNamingConverter<DataModels.TumblrNPF.Meta>(),
                    new FlexibleNamingConverter<CommunityLabels>(), new FlexibleNamingConverter<Badge>(), new FlexibleNamingConverter<TumblrmartAccessories>(),
                    new FlexibleNamingConverter<Poster>(), new FlexibleNamingConverter<Content>(), new FlexibleNamingConverter<Medium>(),
                    new FlexibleNamingConverter<Attribution>(), new FlexibleNamingConverter<Trail>(), new FlexibleNamingConverter<Style>(),
                    new FlexibleNamingConverter<Layout>() }
            };

            try
            {
                _ = postsList.Select(p => JsonConvert.DeserializeObject<Post>((string)JsonConvert.SerializeObject(p), serializerSettings))
                    .Where(x => !new string[] { "client_side_ad_waterfall", "backfill_ad" }.Contains(x.ObjectType)).ToList();
            }
            catch (JsonSerializationException ex)
            {
                Logger.Verbose("TumblrHiddenCrawler.ExtractPosts: {0}", ex.Message);
                ShellService.ShowError(ex, "{0}: Error parsing page!", Blog.Name);
            }

            List<Post> posts = null;
            try
            {
                serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                posts = postsList.Select(p => JsonConvert.DeserializeObject<Post>((string)JsonConvert.SerializeObject(p), serializerSettings))
                    .Where(x => !new string[] { "client_side_ad_waterfall", "backfill_ad" }.Contains(x.ObjectType)).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                Logger.Verbose("TumblrHiddenCrawler.ExtractPosts: {0}", ex.Message);
            }
        
            return posts;
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

        private async Task DownloadPage(List<Post> posts)
        {
            var lastPostId = GetLastPostId();
            foreach (var post in posts)
            {
                if (CheckIfShouldStop()) { break; }
                CheckIfShouldPause();
                if (lastPostId > 0 && ulong.TryParse(post.Id, out var postId) && postId < lastPostId) { continue; }
                if (!PostWithinTimespan(post)) { continue; }
                if (!CheckIfContainsTaggedPost(post)) { continue; }
                if (!CheckIfDownloadRebloggedPosts(post)) { continue; }

                Logger.Verbose("TumblrHiddenCrawler.DownloadPage: {0}", post.PostUrl);
                try
                {
                    DataModels.TumblrApiJson.Post data = null;
                    data = new DataModels.TumblrApiJson.Post()
                    {
                        Date = post.Date,
                        DateGmt = post.Date,
                        Type = "regular",
                        Id = post.Id,
                        Tags = post.Tags?.ToList(),
                        Slug = post.Slug,
                        RegularTitle = post.Summary,
                        RebloggedFromName = "",
                        RebloggedRootName = "",
                        ReblogKey = post.ReblogKey,
                        UnixTimestamp = post.Timestamp,
                        Tumblelog = new DataModels.TumblrApiJson.TumbleLog2() { Name = post.BlogName },
                        UrlWithSlug = post.PostUrl
                    };
                    var countImagesVideos = CountImagesAndVideos(GetContents(post));
                    int index = -1;
                    foreach (var content in GetContents(post))
                    {
                        data.Type = ConvertContentTypeToPostType(content.Type);
                        index += (countImagesVideos > 1) ? 1 : 0;
                        DownloadMedia(content, data, index);
                        AddInlinePhotoUrl(post, content, data);
                        AddInlineVideoUrl(post, content, data);
                    }
                    DownloadText(post, data);
                    AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(post.Id, ".json"), post));

                    await Task.CompletedTask;
                }
                catch (NullReferenceException e)
                {
                    Logger.Verbose("TumblrHiddenCrawler.DownloadPage: {0}", e);
                }
                catch (Exception e)
                {
                    Logger.Error("TumblrHiddenCrawler.DownloadPage: {0}", e);
                    ShellService.ShowError(e, "{0}: Error parsing post!", Blog.Name);
                }
            }
        }

        private static List<Content> GetContents(Post post)
        {
            if (post.Content?.Count > 0)
            {
                return post.Content;
            }
            else if (post.Trail?.Count > 0)
            {
                return post.Trail.SelectMany(t => t.Content).ToList();
            }
            return new List<Content>();
        }

        private void DownloadText(Post post, DataModels.TumblrApiJson.Post data)
        {
            if (Blog.DownloadText && new string[] { "regular", "quote", "note", "link", "conversation" }.Contains(post.OriginalType))
            {
                string text = "";
                if (post.Content.Count == 0)
                {
                    foreach (var trail in post.Trail)
                    {
                        text += Environment.NewLine + (trail.Blog ?? trail.BrokenBlog).Name + "/" + trail.Post.Id + ":" + Environment.NewLine + Environment.NewLine;
                        foreach (var content in trail.Content)
                        {
                            if (content.Type == "text")
                            {
                                text += content.Text + Environment.NewLine + (content.Subtype == "heading1" || content.Subtype == "heading2" ? "" : Environment.NewLine);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var content in post.Content)
                    {
                        if (content.Type == "text")
                        {
                            text += content.Text + Environment.NewLine + (content.Subtype == "heading1" || content.Subtype == "heading2" ? "" : Environment.NewLine);
                        }
                    }
                }
                data.RegularBody = text.Trim(Environment.NewLine.ToCharArray());
                data.Type = post.OriginalType;

                switch (post.OriginalType)
                {
                    case "regular":
                        if (post.Content.Count == 0)
                        {
                            data.RegularTitle = $"{post.BlogName} reblogged {post.RebloggedFromName}/{post.RebloggedFromId}";
                            foreach (var trail in post.Trail)
                            {
                                text += Environment.NewLine + (trail.Blog ?? trail.BrokenBlog).Name + "/" + (trail.Post.Id ?? "") + ":" + Environment.NewLine + Environment.NewLine;
                                foreach (var content in trail.Content)
                                {
                                    if (content.Type == "text")
                                    {
                                        text += content.Text + Environment.NewLine + (content.Subtype == "heading1" || content.Subtype == "heading2" ? "" : Environment.NewLine);
                                    }
                                }
                            }
                            data.RegularBody = text.Trim(Environment.NewLine.ToCharArray());
                        }
                        else
                        {
                            data.RegularTitle = (post.Content?.FirstOrDefault()?.Subtype ?? "") == "heading1" ? post.Content?.FirstOrDefault()?.Text : "";
                            data.RegularBody = string.Join("", post.Content
                                .Where(c => c.Type == "text")
                                .Skip((post.Content?.FirstOrDefault()?.Subtype ?? "") == "heading1" ? 1 : 0)
                                .Select(s => s.Text + Environment.NewLine + (s.Subtype == "heading1" || s.Subtype == "heading2" ? "" : Environment.NewLine)))
                                .Trim(Environment.NewLine.ToCharArray());
                        }
                        if (data.RegularTitle.Length != 0 || data.RegularBody.Length != 0)
                        {
                            text = tumblrJsonParser.ParseText(data);
                            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{data.Id}.txt", data, "text", -1) : null;
                            AddToDownloadList(new TextPost(text, data.Id, data.UnixTimestamp.ToString(), filename));
                        }
                        break;
                    case "quote":
                        data.QuoteText = post.Content?.FirstOrDefault()?.Text;
                        data.QuoteSource = post.Content?.Skip(1).FirstOrDefault()?.Text;
                        text = tumblrJsonParser.ParseQuote(data);
                        string filename2 = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{data.Id}.txt", data, "quote", -1) : null;
                        AddToDownloadList(new QuotePost(text, data.Id, data.UnixTimestamp.ToString(), filename2));
                        break;
                    case "note":
                        data.Type = "answer";
                        data.Question = post.Content?.FirstOrDefault()?.Text;
                        data.Answer = string.Join(Environment.NewLine, post.Content.Skip(1).Select(s => s.Text));
                        text = tumblrJsonParser.ParseAnswer(data);
                        filename2 = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{data.Id}.txt", data, "answer", -1) : null;
                        AddToDownloadList(new AnswerPost(text, data.Id, data.UnixTimestamp.ToString(), filename2));
                        break;
                    case "link":
                        var o = post.Content.FirstOrDefault(x => x.Type == "link") ?? new Content();
                        data.LinkDescription = o.Description;
                        data.LinkText = o.Title;
                        data.LinkUrl = o.Url;
                        text = tumblrJsonParser.ParseLink(data);
                        filename2 = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{data.Id}.txt", data, "link", -1) : null;
                        AddToDownloadList(new LinkPost(text, data.Id, data.UnixTimestamp.ToString(), filename2));
                        break;
                    case "conversation":
                        data.Conversation = null;
                        data.ConversationTitle = post.Content?.FirstOrDefault()?.Text;
                        data.ConversationText = string.Join(Environment.NewLine, post.Content.Skip(1).Select(s => s.Text));
                        text = tumblrJsonParser.ParseConversation(data);
                        filename2 = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{data.Id}.txt", data, "conversation", -1) : null;
                        AddToDownloadList(new ConversationPost(text, data.Id, data.UnixTimestamp.ToString(), filename2));
                        break;
                }
            }
        }

        private void DownloadMedia(Content content, DataModels.TumblrApiJson.Post data, int index)
        {
            string type = content.Type;

            string url = content.Media?.FirstOrDefault()?.Url;
            if (url == null)
                return;
            if (CheckIfSkipGif(url))
                return;
            if (type == "video")
            {
                if (Blog.DownloadVideoThumbnail)
                {
                    if (content.Provider == "tumblr" || url.Contains("tumblr.com") || Blog.RegExVideos)
                    {
                        string thumbnailUrl = content.Poster[0].Url;
                        AddToDownloadList(new PhotoPost(thumbnailUrl, thumbnailUrl, data.Id, data.UnixTimestamp.ToString(), BuildFileName(thumbnailUrl, data, index)));
                    }
                }
                // can only download preview image for non-tumblr (embedded) video posts
                if (Blog.DownloadVideo && content.Provider == "tumblr")
                    AddToDownloadList(new VideoPost(url, null, data.Id, data.UnixTimestamp.ToString(), BuildFileName(url, data, index)));
            }
            else if (type == "audio")
            {
                if (Blog.DownloadAudio && content.Provider == "tumblr")
                {
                    url = url.IndexOf("?") > -1 ? url.Substring(0, url.IndexOf("?")) : url;
                    AddToDownloadList(new AudioPost(url, data.Id, data.UnixTimestamp.ToString(), BuildFileName(url, data, index)));
                }
            }
            else if (type == "image")
            {
                if (Blog.DownloadPhoto)
                {
                    var postedUrl = url;
                    if (url.Contains("tumblr.com/"))
                    {
                        url = RetrieveOriginalImageUrl(url, 2000, 3000, false);
                        url = CheckPnjUrl(url);
                    }
                    AddToDownloadList(new PhotoPost(url, postedUrl, data.Id, data.UnixTimestamp.ToString(), BuildFileName(url, data, index)));
                }
            }
        }


        private static string InlineSearch(Post post, Content content)
        {
            string text = post.Summary;

            if (content.Type == "video")
            {
                text += HttpUtility.UrlDecode(content.EmbedHtml);
            }
            else if (content.Type == "text")
            {
                text += content.Text;
            }

            return text;
        }

        private void AddInlinePhotoUrl(Post post, Content content, DataModels.TumblrApiJson.Post data)
        {
            if (!Blog.DownloadPhoto) return;

            string text = InlineSearch(post, content);

            AddTumblrPhotoUrl(text, data);

            if (Blog.RegExPhotos)
            {
                AddGenericPhotoUrl(text, data);
            }
        }

        private void AddInlineVideoUrl(Post post, Content content, DataModels.TumblrApiJson.Post data)
        {
            if (!Blog.DownloadVideo) return;

            string text = InlineSearch(post, content);

            AddTumblrVideoUrl(text, data);

            if (Blog.RegExVideos)
            {
                AddGenericVideoUrl(text, data);
            }
        }

        private static int CountImagesAndVideos(IList<Content> list)
        {
            var count = 0;
            foreach (var content in list)
            {
                count += (content.Type == "image" || content.Type == "video") ? 1 : 0;
            }
            return count;
        }

        private bool PostWithinTimespan(Post post)
        {
            if (string.IsNullOrEmpty(Blog.DownloadFrom) && string.IsNullOrEmpty(Blog.DownloadTo))
            {
                return true;
            }

            try
            {
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

                long postTime = Convert.ToInt64(post.Timestamp);
                return downloadFromUnixTime <= postTime && postTime < downloadToUnixTime;
            }
            catch (System.FormatException)
            {
                throw new FormatException(Resources.BlogValueHasWrongFormat);
            }
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                string document = await GetSvcPageAsync(GetStartUrl());
                var json = ExtractJson(document);
                dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(json);
                var loggedIn = obj?.isLoggedIn?.isLoggedIn ?? false;
                return loggedIn;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return true;
                }

                if (HandleServiceUnavailableWebException(webException))
                {
                    return false;
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return false;
            }

            return true;
        }

        private async Task<string> GetSvcPageAsync(string url)
        {
            AcquireTimeconstraintSvc();

            return await GetRequestAsync(url);
        }

        private bool CheckPostAge(List<Post> posts)
        {
            ulong highestPostId = 0;
            var post = posts.FirstOrDefault(x => !x.IsPinned);
            if (post == null) return false;
            _ = ulong.TryParse(post.Id, out highestPostId);
            return highestPostId >= GetLastPostId();
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            return Blog.DownloadRebloggedPosts || string.IsNullOrEmpty(post.RebloggedFromUuid) || post.RebloggedFromUuid == post.Blog.Uuid;
        }

        private void AddToJsonQueue(CrawlerData<Post> addToList)
        {
            if (!Blog.DumpCrawlerData) { return; }

            if ((Blog.ForceRescan && !ShellService.Settings.NoCrawlerDataUpdate) || !crawlerDataDownloader.ExistingCrawlerDataContainsOrAdd(addToList.Filename))
            {
                jsonQueue.Add(addToList);
            }
        }

        private void AddToJsonQueue(string[] urls, Post post)
        {
            if (urls == null || urls.Length == 0) return;
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(FileName(urls[0]), ".json"), post));
        }

        private bool CheckIfContainsTaggedPost(Post post)
        {
            return !Tags.Any() || post.Tags.Any(x => Tags.Contains(x, StringComparer.OrdinalIgnoreCase));
        }

        private string GetStartUrl()
        {
            var blogName = Domain.Models.Blogs.Blog.ExtractName(Blog.Url);
            var url = $"https://www.tumblr.com/dashboard/blog/{blogName}";
            return url;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                semaphoreSlim?.Dispose();
                downloader.Dispose();
                nextPage?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
