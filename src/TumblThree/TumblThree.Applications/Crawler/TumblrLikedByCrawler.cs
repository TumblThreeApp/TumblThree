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
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.DataModels.TumblrApiJson;
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
    [ExportMetadata("BlogType", typeof(TumblrLikedByBlog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TumblrLikedByCrawler : AbstractTumblrCrawler, ICrawler, IDisposable
    {
        private static readonly Regex extractJsonFromLikes = new Regex("window\\['___INITIAL_STATE___'\\] = (.*);[\\s]*?</script>", RegexOptions.Singleline);
        private static readonly Regex extractJsonFromLikes2 = new Regex("id=\"___INITIAL_STATE___\">\\s*?({.*})\\s*?</script>", RegexOptions.Singleline);

        private readonly IDownloader downloader;
        private readonly ITumblrToTextParser<Post> tumblrJsonParser;
        private readonly IPostQueue<CrawlerData<DataModels.TumblrSearchJson.Data>> jsonQueue;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;
        private readonly BlockingCollection<string> nextPage = new BlockingCollection<string>();

        private int numberOfPagesCrawled;

        public TumblrLikedByCrawler(IShellService shellService, ICrawlerService crawlerService, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IDownloader downloader, ICrawlerDataDownloader crawlerDataDownloader, 
            ITumblrToTextParser<Post> tumblrJsonParser, ITumblrParser tumblrParser, IImgurParser imgurParser,
            IGfycatParser gfycatParser, IWebmshareParser webmshareParser, IUguuParser uguuParser, ICatBoxParser catboxParser,
            IPostQueue<AbstractPost> postQueue, IPostQueue<CrawlerData<DataModels.TumblrSearchJson.Data>> jsonQueue, IBlog blog, IProgress<DownloadProgress> progress,
            IEnvironmentService environmentService, ILoginService loginService, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, webRequestFactory, cookieService, tumblrParser, imgurParser, gfycatParser,
                webmshareParser, uguuParser, catboxParser, postQueue, blog, downloader, crawlerDataDownloader,
                progress, environmentService, loginService, pt, ct)
        {
            this.downloader = downloader;
            this.tumblrJsonParser = tumblrJsonParser;
            this.jsonQueue = jsonQueue;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("TumblrLikedByCrawler.Crawl:Start");

            Task crawlerDownloader = Task.CompletedTask;
            if (Blog.DumpCrawlerData)
            {
                await crawlerDataDownloader.GetAlreadyExistingCrawlerDataFilesAsync(Progress);
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();
            }

            Task grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            Blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            Blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            Blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            Blog.TotalCount = (Blog.TotalCount - Blog.DuplicatePhotos - Blog.DuplicateAudios - Blog.DuplicateVideos);

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
            await download;

            if (!Ct.IsCancellationRequested)
            {
                Blog.LastCompleteCrawl = DateTime.Now;
            }

            Blog.Save();

            UpdateProgressQueueInformation(string.Empty);
        }

        private async Task GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);
            trackedTasks = new List<Task>();

            if (!await CheckIfLoggedInAsync())
            {
                Logger.Error("TumblrLikedByCrawler:GetUrlsAsync: {0}", "User not logged in");
                ShellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, Blog.Name);
                PostQueue.CompleteAdding();
                jsonQueue.CompleteAdding();
                return;
            }

            long pagination = CreateStartPagination();

            nextPage.Add(Blog.Url + (TumblrLikedByBlog.IsLikesUrl(Blog.Url) ? "?before=" : "/page/1/") + pagination);

            foreach (int crawlerNumber in Enumerable.Range(0, ShellService.Settings.ConcurrentScans))
            {
                await semaphoreSlim.WaitAsync();

                trackedTasks.Add(CrawlPageAsync(crawlerNumber));
            }

            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats(true);
        }

        private long prevPagination = long.MaxValue;
        private long pagination;
        private int pageNumber = 1;

        private async Task CrawlPageAsync(int crawlerNumber)
        {
            try
            {
                var isLikesUrl = TumblrLikedByBlog.IsLikesUrl(Blog.Url);

                while (true)
                {
                    if (CheckIfShouldStop())
                    {
                        return;
                    }

                    CheckIfShouldPause();

                    string url;
                    try
                    {
                        if (!nextPage.TryTake(out url))
                        {
                            return;
                        }
                    }
                    catch (Exception e) when (e is OperationCanceledException || e is InvalidOperationException)
                    {
                        return;
                    }

                    string document = "";
                    try
                    {
                        try
                        {
                            AcquireTimeconstraintSvc();
                            document = await GetRequestAsync(url);
                        }
                        catch (WebException webEx)
                        {
                            if (HandleUnauthorizedWebExceptionRetry(webEx))
                            {
                                await FetchCookiesAgainAsync();
                                AcquireTimeconstraintSvc();
                                document = await GetRequestAsync(url);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        if (!isLikesUrl)
                        {
                            document = Regex.Unescape(document);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("TumblrLikedByCrawler:CrawlPageAsync: {0}", ex);
                    }

                    if (document.Length == 0)
                    {
                        Logger.Verbose("TumblrLikedByCrawler:CrawlPageAsync: empty document");
                        throw new Exception("TumblrLikedByCrawler:CrawlPageAsync: empty document");
                    }
                    if (document.Contains("<div class=\"no_posts_found\""))
                    {
                        nextPage.CompleteAdding();
                        return;
                    }

                    if (isLikesUrl)
                    {
                        var posts = ExtractPosts(document);
                        await DownloadPage(posts);
                    }
                    else
                    {
                        await AddUrlsToDownloadListAsync(document);
                    }

                    pagination = ExtractNextPageLink(document);
                    if (pagination == 0)
                    {
                        nextPage.CompleteAdding();
                        return;
                    }
                    pageNumber++;
                    var notWithinTimespan = !CheckIfWithinTimespan(pagination);
                    if (isLikesUrl)
                    {
                        if (pagination >= prevPagination)
                        {
                            nextPage.CompleteAdding();
                            return;
                        }
                        prevPagination = pagination;
                    }
                    nextPage.Add(Blog.Url + (isLikesUrl ? "?before=" : "/page/" + pageNumber + "/") + pagination);

                    Interlocked.Increment(ref numberOfPagesCrawled);
                    UpdateProgressQueueInformation(Resources.ProgressGetUrlShort, numberOfPagesCrawled);
                    if (notWithinTimespan)
                    {
                        return;
                    }
                }
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (LimitExceededWebException limitExceededException)
            {
                if (!HandleLimitExceededWebException((WebException)limitExceededException.InnerException))
                {
                    Logger.Error("TumblrLikedByCrawler:CrawlPageAsync: {0}", limitExceededException);
                    ShellService.ShowError(limitExceededException, "{0}: {1}", Blog.Name, limitExceededException.Message);
                }
            }
            catch (FormatException formatException)
            {
                Logger.Error("TumblrLikedByCrawler:CrawlPageAsync: {0}", formatException);
                ShellService.ShowError(formatException, "{0}: {1}", Blog.Name, formatException.Message);
            }
            catch (Exception e)
            {
                Logger.Error("TumblrLikedByCrawler:CrawlPageAsync: {0}", e);
                ShellService.ShowError(e, "{0}: Error parsing post!", Blog.Name);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        #region "Likes download"

        private static string InlineSearch(DataModels.TumblrSearchJson.Data post, DataModels.TumblrSearchJson.Content content)
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

        private void AddInlinePhotoUrl(DataModels.TumblrSearchJson.Data post, DataModels.TumblrSearchJson.Content content, Post data)
        {
            if (!Blog.DownloadPhoto) return;

            string text = InlineSearch(post, content);

            AddTumblrPhotoUrl(text, data);

            if (Blog.RegExPhotos)
            {
                AddGenericPhotoUrl(text, data);
            }
        }

        private void AddInlineVideoUrl(DataModels.TumblrSearchJson.Data post, DataModels.TumblrSearchJson.Content content, Post data)
        {
            if (!Blog.DownloadVideo) return;

            string text = InlineSearch(post, content);

            AddTumblrVideoUrl(text, data);

            if (Blog.RegExVideos)
            {
                AddGenericVideoUrl(text, data);
            }
        }

        private static int CountImagesAndVideos(IList<DataModels.TumblrSearchJson.Content> list)
        {
            var count = 0;
            foreach (var content in list)
            {
                count += (content.Type == "image" || content.Type == "video") ? 1 : 0;
            }
            return count;
        }

        private bool PostWithinTimespan(DataModels.TumblrSearchJson.Data post)
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
            long likedTimestamp = post.LikedTimestamp;
            return downloadFromUnixTime <= likedTimestamp && likedTimestamp < downloadToUnixTime;
        }

        private static List<DataModels.TumblrSearchJson.Data> ExtractPosts(string document)
        {
            var extracted = extractJsonFromLikes.Match(document).Groups[1].Value;
            if (string.IsNullOrEmpty(extracted)) extracted = extractJsonFromLikes2.Match(document).Groups[1].Value;
            if (string.IsNullOrEmpty(extracted))
            {
                Logger.Verbose("TumblrLikedByCrawler:ExtractPosts: data not found inside: \n{0}", document);
            }
            dynamic obj = JsonConvert.DeserializeObject(extracted);
            var likedPosts = obj.Likes.likedPosts;
            if (likedPosts is null)
            {
                foreach (var query in obj.queries.queries)
                {
                    if (query?.queryKey?[0] == "likes")
                    {
                        likedPosts = query.state.data.pages[0].items;
                    }
                }
            }
            extracted = JsonConvert.SerializeObject(likedPosts);
            var posts = JsonConvert.DeserializeObject<List<DataModels.TumblrSearchJson.Data>>(extracted);
            return posts;
        }

        private async Task DownloadPage(List<DataModels.TumblrSearchJson.Data> posts)
        {
            foreach (var post in posts)
            {
                if (CheckIfShouldStop()) { break; }
                CheckIfShouldPause();
                if (!PostWithinTimespan(post)) { continue; }

                Logger.Verbose("TumblrLikedByCrawler.DownloadPage: {0}", post.PostUrl);
                try
                {
                    Post data = null;
                    data = new Post()
                    {
                        Date = post.Date,
                        DateGmt = post.Date,
                        Type = "regular",
                        Id = post.Id,
                        Tags = post.Tags.ToList(),
                        Slug = post.Slug,
                        RegularTitle = post.Summary,
                        RebloggedFromName = "",
                        RebloggedRootName = "",
                        ReblogKey = post.ReblogKey,
                        UnixTimestamp = post.Timestamp,
                        Tumblelog = new TumbleLog2() { Name = post.BlogName },
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
                    AddToJsonQueue(new CrawlerData<DataModels.TumblrSearchJson.Data>(Path.ChangeExtension(post.Id, ".json"), post));

                    await Task.CompletedTask;
                }
                catch (NullReferenceException e)
                {
                    Logger.Verbose("TumblrLikedByCrawler.DownloadPage: {0}", e);
                }
                catch (Exception e)
                {
                    Logger.Error("TumblrLikedByCrawler.DownloadPage: {0}", e);
                    ShellService.ShowError(e, "{0}: Error parsing post!", Blog.Name);
                }
            }
        }

        private void DownloadText(DataModels.TumblrSearchJson.Data post, Post data)
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
                                text += content.Text + Environment.NewLine + (content.SubType == "heading1" || content.SubType == "heading2" ? "" : Environment.NewLine);
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
                            text += content.Text + Environment.NewLine + (content.SubType == "heading1" || content.SubType == "heading2" ? "" : Environment.NewLine);
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
                                        text += content.Text + Environment.NewLine + (content.SubType == "heading1" || content.SubType == "heading2" ? "" : Environment.NewLine);
                                    }
                                }
                            }
                            data.RegularBody = text.Trim(Environment.NewLine.ToCharArray());
                        }
                        else
                        {
                            data.RegularTitle = (post.Content?[0]?.SubType ?? "") == "heading1" ? post.Content?[0]?.Text : "";
                            data.RegularBody = string.Join("", post.Content
                                .Where(c => c.Type == "text")
                                .Skip((post.Content?[0]?.SubType ?? "") == "heading1" ? 1 : 0)
                                .Select(s => s.Text + Environment.NewLine + (s.SubType == "heading1" || s.SubType == "heading2" ? "" : Environment.NewLine)))
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
                        var o = post.Content.FirstOrDefault(x => x.Type == "link") ?? new DataModels.TumblrSearchJson.Content();
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

        private void DownloadMedia(DataModels.TumblrSearchJson.Content content, Post data, int index)
        {
            string type = content.Type;

            string url = content.Media?[0]?.Url;
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
                    AddToDownloadList(new VideoPost(url, data.Id, data.UnixTimestamp.ToString(), BuildFileName(url, data, index)));
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

        #endregion

        #region "Liked/By download"
        
        private async Task AddUrlsToDownloadListAsync(string document)
        {
            try
            {
                AddPhotoUrlToDownloadList(document);
                AddVideoUrlToDownloadList(document);
                await Task.CompletedTask;
            }
            catch (NullReferenceException e)
            {
                Logger.Verbose("TumblrLikedByCrawler.AddUrlsToDownloadListAsync: {0}", e);
            }
        }

        private void AddPhotoUrlToDownloadList(string document)
        {
            if (!Blog.DownloadPhoto)
            {
                return;
            }

            var post = new Post()
            {
                Date = DateTime.Now.ToString("R"),
                DateGmt = DateTime.Now.ToString("R"),
                UnixTimestamp = (int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds(),
                Type = "",
                Id = "",
                Tags = new List<string>(),
                Slug = "",
                RegularTitle = "",
                RebloggedFromName = "",
                RebloggedRootName = "",
                ReblogKey = "",
                Tumblelog = new TumbleLog2() { Name = "" }
            };
            AddTumblrPhotoUrl(document, post);

            if (Blog.RegExPhotos)
            {
                AddGenericPhotoUrl(document, post);
            }
        }

        private void AddVideoUrlToDownloadList(string document)
        {
            if (!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail)
            {
                return;
            }

            var post = new Post()
            {
                Id = "",
                Tumblelog = new TumbleLog2() { Name = "" },
                UnixTimestamp = (int)((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()
            };
            AddTumblrVideoUrl(document, post);
            AddInlineTumblrVideoUrl(document, TumblrParser.GetTumblrVVideoUrlRegex(), TumblrParser.GetTumblrThumbnailUrlRegex());

            if (Blog.DownloadVideo && Blog.RegExVideos)
            {
                AddGenericVideoUrl(document, post);
            }
        }

        #endregion

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                AcquireTimeconstraintSvc();
                await GetRequestAsync(Blog.Url);
                Blog.Online = true;
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.RequestCanceled)
                {
                    return;
                }

                Logger.Error("TumblrLikedByCrawler:IsBlogOnlineAsync:WebException {0}", webException);
                ShellService.ShowError(webException, Resources.BlogIsOffline, Blog.Name);
                Blog.Online = false;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.OnlineChecking);
                Blog.Online = false;
            }
            catch (Exception ex) when (ex.Message == "Acceptance of privacy consent needed!")
            {
                Logger.Error("TumblrLikedByCrawler:IsBlogOnlineAsync: {0}", "Acceptance of privacy consent needed!");
                Blog.Online = false;
            }
        }

        private long CreateStartPagination()
        {
            if (string.IsNullOrEmpty(Blog.DownloadTo))
            {
                return DateTimeOffset.Now.ToUnixTimeSeconds();
            }

            DateTime downloadTo = DateTime.ParseExact(Blog.DownloadTo, "yyyyMMdd", CultureInfo.InvariantCulture,
                DateTimeStyles.None);
            var dateTimeOffset = new DateTimeOffset(downloadTo);
            return dateTimeOffset.ToUnixTimeSeconds();
        }

        private bool CheckIfPageCountReached(int pageCount)
        {
            int numberOfPages = RangeToSequence(Blog.DownloadPages).Count();
            return pageCount >= numberOfPages;
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                var url = Blog.Url + (TumblrLikedByBlog.IsLikesUrl(Blog.Url) ? "" : "/page/1");
                AcquireTimeconstraintSvc();
                string document = await GetRequestAsync(url);
                if (string.IsNullOrEmpty(document))
                {
                    Logger.Verbose("TumblrLikedByCrawler:CheckIfLoggedInAsync: empty response!");
                }
                if (document.Contains("___INITIAL_STATE___"))
                {
                    var extracted = extractJsonFromLikes.Match(document).Groups[1].Value;
                    if (string.IsNullOrEmpty(extracted)) extracted = extractJsonFromLikes2.Match(document).Groups[1].Value;
                    if (string.IsNullOrEmpty(extracted))
                    {
                        Logger.Verbose("TumblrLikedByCrawler:CheckIfLoggedInAsync: data not found inside: \n{0}", document);
                    }
                    dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(extracted);
                    var loggedIn = obj?.isLoggedIn?.isLoggedIn ?? false;
                    return loggedIn;
                }
                else
                {
                    return document.IndexOf("data-page-root=\"" + new Uri(Blog.Url).AbsolutePath, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch (WebException webException) when (webException.Status == WebExceptionStatus.RequestCanceled)
            {
                return true;
            }
            catch (TimeoutException timeoutException)
            {
                HandleTimeoutException(timeoutException, Resources.Crawling);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("TumblrLikedByCrawler:CheckIfLoggedInAsync: {0}", ex);
                return false;
            }
        }

        private static long ExtractNextPageLink(string document)
        {
            // Example pagination:
            //
            // <div id="pagination" class="pagination "><a id="previous_page_link" href="/liked/by/wallpaperfx/page/3/-1457140452" class="previous button chrome">Previous</a>
            // <a id="next_page_link" href="/liked/by/wallpaperfx/page/5/1457139681" class="next button chrome blue">Next</a></div></div>

            const string htmlPagination = "(id=\"next_page_link\" href=\"[A-Za-z0-9_/:.-]+/([0-9]+)/([A-Za-z0-9]+))\"";
            const string jsonPagination = @"(&|\\?|\\u0026)before=([0-9]*)";

            _ = long.TryParse(Regex.Match(document, htmlPagination).Groups[3].Value, out var unixTime);

            if (unixTime == 0)
            {
                var r = Regex.Matches(document, jsonPagination);
                if (r.Count > 0)
                {
                    _ = long.TryParse(r[r.Count-1].Groups[2].Value, out unixTime);
                }
            }

            return unixTime;
        }

        private bool CheckIfWithinTimespan(long pagination)
        {
            if (string.IsNullOrEmpty(Blog.DownloadFrom))
            {
                return true;
            }

            try
            {
                DateTime downloadFrom =  DateTime.ParseExact(Blog.DownloadFrom, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None);
                var dateTimeOffset = new DateTimeOffset(downloadFrom);
                return pagination >= dateTimeOffset.ToUnixTimeSeconds();
            }
            catch (System.FormatException)
            {
                throw new FormatException(Resources.BlogValueHasWrongFormat);
            }
        }

        private void AddToJsonQueue(CrawlerData<DataModels.TumblrSearchJson.Data> addToList)
        {
            if (!Blog.DumpCrawlerData) { return; }

            if (Blog.ForceRescan || !crawlerDataDownloader.ExistingCrawlerDataContainsOrAdd(addToList.Filename))
            {
                jsonQueue.Add(addToList);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                semaphoreSlim?.Dispose();
                downloader.Dispose();
                nextPage.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
