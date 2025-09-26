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
                string document = await GetSvcPageAsync(Blog.Url);
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

            nextPage.Add(Blog.Url);

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
            try
            {
                while (true)
                {
                    string url;
                    try
                    {
                        url = nextPage.Take(Ct);
                    }
                    catch (Exception e) when (e is OperationCanceledException || e is InvalidOperationException)
                    {
                        return;
                    }

                    string document = null;
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

                    if (highestId == 0)
                    {
                        highestId = Math.Max(Blog.LastId, posts.Max(x => ulong.Parse(x.Id)));
                        latestPost = DateTimeOffset.FromUnixTimeSeconds(posts.Where(x => !x.IsPinned).Select(s => s.Timestamp).FirstOrDefault()).UtcDateTime;
                    }

                    if (HasProperty(result, "response") && !HasProperty(result.response, "_links"))
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
            }
            catch (WebException webException)
            {
                if (HandleLimitExceededWebException(webException) ||
                    HandleUnauthorizedWebExceptionRetry(webException))
                {
                    incompleteCrawl = true;
                }
            }
            catch (TimeoutException timeoutException)
            {
                incompleteCrawl = true;
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (FormatException formatException)
            {
                Logger.Error("TumblrHiddenCrawler.CrawlPageAsync: {0}", formatException);
                ShellService.ShowError(formatException, "{0}: {1}", Blog.Name, formatException.Message);
            }
            catch (Exception ex)
            {
                Logger.Error("TumblrHiddenCrawler.CrawlPageAsync: {0}", ex);
            }
            finally
            {
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

        private static List<Post> ExtractPosts(string document, out dynamic result)
        {
            var json = ExtractJson(document);

            result = JsonConvert.DeserializeObject<ExpandoObject>(json, new JsonSerializerSettings() { Converters = { new ExpandoObjectConverter() } });

            var postsList = (HasProperty(result, "PeeprRoute") ? result.PeeprRoute.initialTimeline.objects : result.response.posts) as IEnumerable<dynamic>;
            List<Post> posts = null;
            try
            {
                posts = postsList.Select(p => JsonConvert.DeserializeObject<Post>((string)JsonConvert.SerializeObject(p), new JsonSerializerSettings()
                {
                    MissingMemberHandling = MissingMemberHandling.Error,
                    Converters = { new ExpandoObjectConverter(), new FlexibleNamingConverter<Post>(), new FlexibleNamingConverter<DataModels.TumblrNPF.Blog>(),
                    new FlexibleNamingConverter<DataModels.TumblrNPF.Resources>(), new FlexibleNamingConverter<ClientSideAd>(), new FlexibleNamingConverter<Context>(),
                    new FlexibleNamingConverter<DataModels.TumblrNPF.Theme>(), new FlexibleNamingConverter<DataModels.TumblrNPF.Meta>(),
                    new FlexibleNamingConverter<CommunityLabels>(), new FlexibleNamingConverter<Badge>(), new FlexibleNamingConverter<TumblrmartAccessories>(),
                    new FlexibleNamingConverter<Poster>(), new FlexibleNamingConverter<Content>(), new FlexibleNamingConverter<Medium>(),
                    new FlexibleNamingConverter<Attribution>()}
                })).Where(x => !new string[] { "client_side_ad_waterfall", "backfill_ad" }.Contains(x.ObjectType)).ToList();
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
            foreach (var post in posts)
            {
                if (CheckIfShouldStop()) { break; }
                CheckIfShouldPause();
                if (!PostWithinTimespan(post)) { continue; }

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
                    var countImagesVideos = CountImagesAndVideos(post.Content);
                    int index = -1;
                    foreach (var content in post.Content)
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

        private void DownloadText(Post post, DataModels.TumblrApiJson.Post data)
        {
            if (Blog.DownloadText && new string[] { "regular", "quote", "note", "link", "conversation" }.Contains(post.OriginalType))
            {
                string text = "";
                if (post.Content.Count == 0)
                {
                    foreach (var trail in post.Trail)
                    {
                        text += Environment.NewLine + trail.Blog.Name + "/" + trail.Post.Id + ":" + Environment.NewLine + Environment.NewLine;
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
                                text += Environment.NewLine + trail.Blog.Name + "/" + trail.Post.Id + ":" + Environment.NewLine + Environment.NewLine;
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
                            data.RegularTitle = (post.Content?[0]?.Subtype ?? "") == "heading1" ? post.Content?[0]?.Text : "";
                            data.RegularBody = string.Join("", post.Content
                                .Where(c => c.Type == "text")
                                .Skip((post.Content?[0]?.Subtype ?? "") == "heading1" ? 1 : 0)
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
                        data.QuoteText = post.Content?[0]?.Text;
                        data.QuoteSource = post.Content?[1]?.Text;
                        text = tumblrJsonParser.ParseQuote(data);
                        string filename2 = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{data.Id}.txt", data, "quote", -1) : null;
                        AddToDownloadList(new QuotePost(text, data.Id, data.UnixTimestamp.ToString(), filename2));
                        break;
                    case "note":
                        data.Type = "answer";
                        data.Question = post.Content?[0]?.Text;
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
                        data.ConversationTitle = post.Content?[0]?.Text;
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
                string document = await GetSvcPageAsync(Blog.Url);
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
            return Blog.DownloadRebloggedPosts || string.IsNullOrEmpty(post.RebloggedFromName) || post.RebloggedFromName == Blog.Name;
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


        /*
         * 
        private void AddPhotoUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadPhoto) { return; }

            Post postCopy = post;
            if (post.Type == "photo")
            {
                AddPhotoUrl(post);
                postCopy = (Post)post.Clone();
                postCopy.Photos.Clear();
            }

            AddInlinePhotoUrl(postCopy);

            if (Blog.RegExPhotos)
            {
                AddGenericInlinePhotoUrl(post);
            }
        }

        private void AddPhotoUrl(Post post)
        {
            string postId = post.Id;
            bool jsonSaved = false;
            int i = 1;
            if (post.Photos.Count != 0 && post.Photos[0].AltSizes.FirstOrDefault().Url.Split('/').Last().StartsWith("tumblr_")) i = -1;
            foreach (Photo photo in post.Photos)
            {
                string imageUrl = photo.AltSizes.Where(url => url.Width == int.Parse(ImageSizeForSearching())).Select(url => url.Url)
                                       .FirstOrDefault() ??
                                  photo.AltSizes.FirstOrDefault().Url;

                if (ShellService.Settings.ImageSize == "best")
                {
                    imageUrl = photo.AltSizes.FirstOrDefault().Url;
                }

                if (CheckIfSkipGif(imageUrl)) { continue; }
                imageUrl = CheckPnjUrl(imageUrl);

                var filename = BuildFileName(imageUrl, post, i);
                AddDownloadedMedia(imageUrl, filename, post);
                AddToDownloadList(new PhotoPost(imageUrl, "", postId, post.Timestamp.ToString(), filename));
                if (!jsonSaved || !Blog.GroupPhotoSets && !(string.Equals(Blog.FilenameTemplate, "%f", StringComparison.OrdinalIgnoreCase) && i == -1))
                {
                    jsonSaved = true;
                    AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                }
                if (i != -1) i++;
            }
        }

        private void AddInlinePhotoUrl(Post post)
        {
            AddTumblrPhotoUrl(InlineSearch(post), ConvertTumblrApiJson(post));
        }

        private void AddGenericInlinePhotoUrl(Post post)
        {
            AddTumblrPhotoUrl(InlineSearch(post), ConvertTumblrApiJson(post));
        }

        private void AddVideoUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail) { return; }

            Post postCopy = post;
            if (post.Type == "video")
            {
                AddVideoUrl(post);

                postCopy = (Post)post.Clone();
                postCopy.VideoUrl = string.Empty;
            }

            var urls = AddTumblrVideoUrl(InlineSearch(postCopy), ConvertTumblrApiJson(post));
            AddToJsonQueue(urls, post);
            urls = AddInlineTumblrVideoUrl(InlineSearch(postCopy), ConvertTumblrApiJson(post));
            AddToJsonQueue(urls, post);

            if (Blog.DownloadVideo && Blog.RegExVideos)
            {
                AddGenericInlineVideoUrl(postCopy);
            }
        }

        private void AddVideoUrl(Post post)
        {
            if (post.VideoUrl == null) { return; }

            string postId = post.Id;
            string videoUrl = post.VideoUrl;

            if (ShellService.Settings.VideoSize == 480)
            {
                if (!videoUrl.Contains("_480"))
                {
                    videoUrl = videoUrl.Replace(".mp4", "_480.mp4");
                }
            }

            if (Blog.DownloadVideo)
            {
                var filename = BuildFileName(videoUrl, post, -1);
                AddDownloadedMedia(videoUrl, filename, post);
                AddToDownloadList(new VideoPost(videoUrl, null, postId, post.Timestamp.ToString(), filename));
                AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
            }

            if (Blog.DownloadVideoThumbnail)
            {
                var filename = BuildFileName(post.ThumbnailUrl, post, "photo", -1);
                AddDownloadedMedia(post.ThumbnailUrl, filename, post);
                AddToDownloadList(new PhotoPost(post.ThumbnailUrl, "", postId, post.Timestamp.ToString(), filename));
                if (!Blog.DownloadVideo)
                {
                    AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(videoUrl.Split('/').Last(), ".json"), post));
                }
            }
        }

        private void AddGenericInlineVideoUrl(Post post)
        {
            AddGenericVideoUrl(InlineSearch(post), ConvertTumblrApiJson(post));
        }

        private void AddAudioUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadAudio) { return; }
            if (post.Type != "audio") { return; }

            string postId = post.Id;
            string audioUrl = post.AudioUrl;
            if (!audioUrl.EndsWith(".mp3"))
            {
                audioUrl = audioUrl + ".mp3";
            }

            var filename = BuildFileName(audioUrl, post, -1);
            AddDownloadedMedia(audioUrl, filename, post);
            AddToDownloadList(new AudioPost(audioUrl, postId, post.Timestamp.ToString(), filename));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(audioUrl.Split('/').Last(), ".json"), post));
        }

        private void AddTextUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadText) { return; }
            if (post.Type != "text") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseText(post);
            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{postId}.txt", post, "text", -1) : null;
            AddToDownloadList(new TextPost(textBody, postId, post.Timestamp.ToString(), filename));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddQuoteUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadQuote) { return; }
            if (post.Type != "quote") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseQuote(post);
            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{post.Id}.txt", post, "quote", -1) : null;
            AddToDownloadList(new QuotePost(textBody, postId, post.Timestamp.ToString(), filename));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddLinkUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadLink) { return; }
            if (post.Type != "link") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseLink(post);
            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{post.Id}.txt", post, "link", -1) : null;
            AddToDownloadList(new LinkPost(textBody, postId, post.Timestamp.ToString(), filename));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddConversationUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadConversation) { return; }
            if (post.Type != "chat") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseConversation(post);
            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{post.Id}.txt", post, "conversation", -1) : null;
            AddToDownloadList(new ConversationPost(textBody, postId, post.Timestamp.ToString(), filename));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddAnswerUrlToDownloadList(Post post)
        {
            if (!Blog.DownloadAnswer) { return; }
            if (post.Type != "answer") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseAnswer(post);
            string filename = Blog.SaveTextsIndividualFiles ? BuildFileName($"/{post.Id}.txt", post, "answer", -1) : null;
            AddToDownloadList(new AnswerPost(textBody, postId, post.Timestamp.ToString(), filename));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddPhotoMetaUrlToDownloadList(Post post)
        {
            if (!Blog.CreatePhotoMeta) { return; }
            if (post.Type != "photo") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParsePhotoMeta(post);
            AddToDownloadList(new PhotoMetaPost(textBody, postId));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddVideoMetaUrlToDownloadList(Post post)
        {
            if (!Blog.CreateVideoMeta) { return; }
            if (post.Type != "video") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseVideoMeta(post);
            AddToDownloadList(new VideoMetaPost(textBody, postId));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private void AddAudioMetaUrlToDownloadList(Post post)
        {
            if (!Blog.CreateAudioMeta) { return; }
            if (post.Type != "audio") { return; }

            string postId = post.Id;
            string textBody = tumblrJsonParser.ParseAudioMeta(post);
            AddToDownloadList(new AudioMetaPost(textBody, postId));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(postId, ".json"), post));
        }

        private static string InlineSearch(Post post)
        {
            return string.Join(" ", post.Trail?.Select(trail => trail.ContentRaw) ?? Enumerable.Empty<string>());
        }

        private async Task AddExternalPhotoUrlToDownloadListAsync(Post post)
        {
            string searchableText = InlineSearch(post);
            string timestamp = post.Timestamp.ToString();

            if (Blog.DownloadImgur)
            {
                AddImgurUrl(searchableText, timestamp);
                await AddImgurAlbumUrlAsync(searchableText, timestamp);
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
        */

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
