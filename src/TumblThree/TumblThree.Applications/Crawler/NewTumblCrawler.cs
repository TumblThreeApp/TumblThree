using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.DataModels.NewTumbl;
using PostType = TumblThree.Applications.DataModels.NewTumbl.PostType;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using System.Dynamic;
using Newtonsoft.Json;
using TumblThree.Applications.Parser;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawler))]
    [ExportMetadata("BlogType", typeof(NewTumblCrawler))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class NewTumblCrawler : AbstractCrawler, ICrawler, IDisposable
    {
        private static readonly Regex extractJsonFromPage = new Regex("var Data_Session[ ]+?=[ ]??(.*?)};", RegexOptions.Singleline);
        private const string BaseUrl = "https://newtumbl.com/x_";

        private readonly string[] cookieHosts = { "https://newtumbl.com/" };
        private readonly string[] aExt = new string[] { "", "html", "jpg", "png", "gif", "mp3", "mp4", "mov" };

        private readonly IDownloader downloader;
        private readonly IPostQueue<CrawlerData<Post>> jsonQueue;
        private readonly IList<string> existingCrawlerData = new List<string>();
        private readonly object existingCrawlerDataLock = new object();
        private readonly ICrawlerDataDownloader crawlerDataDownloader;
        private readonly INewTumblParser newTumblParser;

        private bool completeGrab = true;
        private bool incompleteCrawl;

        private SemaphoreSlim semaphoreSlim;
        private List<Task> trackedTasks;

        private string blogJson;
        private int numberOfPagesCrawled;
        private ulong highestId;
        private string dtSearchField;
        private long blogIx;
        private int totalPosts;
        private int pageNo;
        private bool isLike;

        public NewTumblCrawler(IShellService shellService, ICrawlerService crawlerService, IProgress<DownloadProgress> progress, IWebRequestFactory webRequestFactory,
            ISharedCookieService cookieService, IPostQueue<AbstractPost> postQueue, IPostQueue<CrawlerData<Post>> jsonQueue, IBlog blog, IDownloader downloader,
            ICrawlerDataDownloader crawlerDataDownloader, INewTumblParser newTumblParser, PauseToken pt, CancellationToken ct)
            : base(shellService, crawlerService, progress, webRequestFactory, cookieService, postQueue, blog, downloader, pt, ct)
        {
            this.downloader = downloader;
            this.downloader.ChangeCancellationToken(Ct);
            this.jsonQueue = jsonQueue;
            this.crawlerDataDownloader = crawlerDataDownloader;
            this.crawlerDataDownloader.ChangeCancellationToken(Ct);
            this.newTumblParser = newTumblParser;
        }

        public override async Task IsBlogOnlineAsync()
        {
            try
            {
                await GetApiPageAsync(0);
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
                else if (HandleNotFoundWebException(webException))
                {
                    Blog.Online = false;
                }
                else
                {
                    Logger.Error("NewTumblCrawler:IsBlogOnlineAsync: {0}, {1}", Blog.Name, webException);
                    ShellService.ShowError(webException, "{0}, {1}", Blog.Name, webException.Message);
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

            string json = await GetApiPageAsync(0);
            var response = ConvertJsonToClassNew<Root>(json);
            CheckError(response);

            var info = BlogInfo(response, 2);
            Blog.Title = info.szBlogId;
            Blog.Description = info.szDescription;
            Blog.TotalCount = info.nCount_Post ?? 0;
            Blog.Posts = info.nCount_Post ?? 0;
        }

        public async Task CrawlAsync()
        {
            Logger.Verbose("NewTumblCrawler.Crawl:Start");

            isLike = Blog.Url.EndsWith("/Like", StringComparison.InvariantCultureIgnoreCase);

            Task<bool> grabber = GetUrlsAsync();
            Task<bool> download = downloader.DownloadBlogAsync();

            Task crawlerDownloader = Task.CompletedTask;
            if (Blog.DumpCrawlerData)
            {
                await GetAlreadyExistingCrawlerDataFilesAsync();
                crawlerDownloader = crawlerDataDownloader.DownloadCrawlerDataAsync();
            }

            bool errorsOccurred = await grabber;

            UpdateProgressQueueInformation(Resources.ProgressUniqueDownloads);
            if (!errorsOccurred && (Blog.ForceRescan || Blog.TotalCount == 0)) Blog.Posts = totalPosts;
            Blog.DuplicatePhotos = DetermineDuplicates<PhotoPost>();
            Blog.DuplicateVideos = DetermineDuplicates<VideoPost>();
            Blog.DuplicateAudios = DetermineDuplicates<AudioPost>();
            Blog.TotalCount = Blog.TotalCount - Blog.DuplicatePhotos - Blog.DuplicateAudios - Blog.DuplicateVideos;

            CleanCollectedBlogStatistics();

            await crawlerDownloader;
            bool finishedDownloading = await download;

            if (!Ct.IsCancellationRequested)
            {
                Blog.LastCompleteCrawl = DateTime.Now;
                if (finishedDownloading && !errorsOccurred)
                {
                    Blog.LastId = highestId;
                }
            }

            Blog.Save();

            UpdateProgressQueueInformation(string.Empty);
        }

        protected new void AddToDownloadList(TumblrPost addToList)
        {
            PostQueue.Add(addToList);
            TumblrPost tmp = addToList.CloneWithAdjustedUrl(addToList.Id);
            StatisticsBag.Add(tmp);
        }

        private async Task<bool> GetUrlsAsync()
        {
            semaphoreSlim = new SemaphoreSlim(ShellService.Settings.ConcurrentScans);
            trackedTasks = new List<Task>();

            if (!await CheckIfLoggedInAsync())
            {
                //Logger.Error("NewTumblCrawler:GetUrlsAsync: {0}", "User not logged in");
                //ShellService.ShowError(new Exception("User not logged in"), Resources.NotLoggedIn, Blog.Name);
                //PostQueue.CompleteAdding();
                //jsonQueue.CompleteAdding();
                //return true;
            }

            GenerateTags();

            await semaphoreSlim.WaitAsync();
            trackedTasks.Add(CrawlPageAsync());
            await Task.WhenAll(trackedTasks);

            PostQueue.CompleteAdding();
            jsonQueue.CompleteAdding();

            UpdateBlogStats(GetLastPostId() != 0);

            return incompleteCrawl;
        }

        private async Task CrawlPageAsync()
        {
            try
            {
                while (true)
                {
                    if (!completeGrab || CheckIfShouldStop())
                    {
                        break;
                    }

                    CheckIfShouldPause();

                    string json = null;
                    List<Post> posts = null;
                    for (int numberOfTrials = 0; numberOfTrials < 2; numberOfTrials++)
                    {
                        try
                        {
                            json = await GetApiPageAsync(1);
                            posts = GetPosts(json);
                            break;
                        }
                        catch (APIException ex)
                        {
                            if (numberOfTrials == 0)
                            {
                                Logger.Error($"CrawlPageAsync, retrying: {ex.Message}");
                                await Task.Delay(10000);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    pageNo++;
                    totalPosts += posts.Count;

                    if (dtSearchField == null)
                    {
                        dtSearchField = PostInfo(json, 7)[0].dtSearch.Value.ToString("yyyy-MM-ddTHH:mm:ss");
                    }

                    if (highestId == 0)
                    {
                        Blog.LatestPost = posts[0]?.dtActive ?? DateTime.MinValue;
                        highestId = (ulong)posts[0].qwPostIx.Value;
                    }

                    completeGrab = CheckPostAge(posts);

                    await AddUrlsToDownloadListAsync(posts);

                    numberOfPagesCrawled += Blog.PageSize;
                    UpdateProgressQueueInformation(Resources.ProgressGetUrl2Short, numberOfPagesCrawled);
                }
            }
            catch (TimeoutException timeoutException)
            {
                incompleteCrawl = true;
                HandleTimeoutException(timeoutException, Resources.Crawling);
            }
            catch (ApplicationException ae)
            {
                incompleteCrawl = true;
                Logger.Error(Resources.ErrorDownloadingBlog2, Blog.Name, ShellService.Settings.GetCollection(Blog.CollectionId).Name);
                ShellService.ShowError(new Exception(), string.Format(Resources.ErrorDownloadingBlog2, Blog.Name, ShellService.Settings.GetCollection(Blog.CollectionId).Name));
            }
            catch (APIException ae2)
            {
                incompleteCrawl = true;
                Logger.Error(Resources.ErrorDownloadingBlog, Blog.Name, ae2.Message, ShellService.Settings.GetCollection(Blog.CollectionId).Name);
                ShellService.ShowError(new Exception(), string.Format(Resources.ErrorDownloadingBlog, Blog.Name, ae2.Message, ShellService.Settings.GetCollection(Blog.CollectionId).Name));
            }
            catch (Exception e)
            {
                incompleteCrawl = true;
                Logger.Error(Resources.ErrorDownloadingBlog, Blog.Name, e.Message, ShellService.Settings.GetCollection(Blog.CollectionId).Name);
                ShellService.ShowError(new Exception(), string.Format(Resources.ErrorDownloadingBlog, Blog.Name, e.Message, ShellService.Settings.GetCollection(Blog.CollectionId).Name));
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private bool CheckPostAge(List<Post> posts)
        {
            var post = posts.FirstOrDefault();
            if (post == null) return false;
            ulong highestPostId = (ulong)post.qwPostIx.Value;

            return highestPostId >= GetLastPostId();
        }

        private bool PostWithinTimeSpan(Post post)
        {
            if (string.IsNullOrEmpty(Blog.DownloadFrom) && string.IsNullOrEmpty(Blog.DownloadTo))
            {
                return true;
            }

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

            return downloadFrom <= post.dtActive.Value && post.dtActive.Value < downloadTo;
        }

        private bool CheckIfSkipGif(string imageUrl)
        {
            return Blog.SkipGif && (imageUrl.EndsWith(".gif") || imageUrl.EndsWith(".gifv"));
        }

        private bool CheckIfDownloadRebloggedPosts(Post post)
        {
            return Blog.DownloadRebloggedPosts || post.qwPostIx.Value == post.qwPostIx_From.Value;
        }

        private bool CheckIfContainsTaggedPost(Post post)
        {
            return !Tags.Any() || post.Tags.Any(x => Tags.Contains(x.szTagId, StringComparer.OrdinalIgnoreCase));
        }

        private async Task GetAlreadyExistingCrawlerDataFilesAsync()
        {
            foreach (var filepath in Directory.GetFiles(Blog.DownloadLocation(), "*.json"))
            {
                existingCrawlerData.Add(Path.GetFileName(filepath));
            }
            await Task.CompletedTask;
        }

        private static List<string> GetTags(Post post)
        {
            return post.Tags == null ? new List<string>() : post.Tags.Select(t => t.szTagId).ToList();
        }

        private string BuildFileName(string url, Post post, string type, int index)
        {
            var reblogged = !post.dwBlogIx.Equals(post.dwBlogIx_From);
            var userId = post.dwBlogIx.ToString();
            var reblogName = "";
            var reblogId = "";
            if (reblogged)
            {
                reblogName = post.dwBlogIx_From.ToString();
                reblogId = post.dwBlogIx_From.ToString();
            }
            var tags = GetTags(post);
            return BuildFileNameCore(url, GetBlogName(), post.dtActive.Value, UnixTimestamp(post), index, type, GetPostId(post),
                tags, "", GetTitle(post), reblogName, "", reblogId);
        }

        private static string RemoveHtmlFromString(string text)
        {
            if (string.IsNullOrEmpty(text)) { return text; }

            text = Regex.Replace(text, "<[^>]+>", "").Trim();
            text = Regex.Replace(text, "&nbsp;", " ");
            return Regex.Replace(text, @"\s{2,}", " ");
        }

        private static string GetTitle(Post post)
        {
            var title = "";
            if (post.bPostTypeIx.Equals(PostType.Photo) || post.bPostTypeIx.Equals(PostType.Video) || post.bPostTypeIx.Equals(PostType.Audio))
            {
                title = post.Parts.Where(p => p.bPartTypeIx == PostType.Comment).OrderBy(o => o.nPartIz).FirstOrDefault()?.Medias?[0]?.szBody ?? "";
                title = RemoveHtmlFromString(title);
            }
            return title;
        }

        private static void AddDownloadedMedia(string url, string filename, Post post)
        {
            if (post == null) throw new ArgumentNullException(nameof(post));
            post.DownloadedFilenames.Add(filename);
            post.DownloadedUrls.Add(url);
        }

        private void AddToJsonQueue(CrawlerData<DataModels.NewTumbl.Post> addToList)
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

        private async Task AddUrlsToDownloadListAsync(List<Post> posts)
        {
            var lastPostId = GetLastPostId();
            foreach (Post post in posts)
            {
                try
                {
                    if (CheckIfShouldStop()) { break; }
                    CheckIfShouldPause();
                    if (lastPostId > 0 && (ulong)post.qwPostIx.Value < lastPostId) { continue; }
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
                        AddAnswerUrlToDownloadList(post);
                    }
                    catch (Exception e)
                    {
                        Logger.Verbose("NewTumblCrawler.AddUrlsToDownloadListAsync: {0}", e);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("NewTumblCrawler.AddUrlsToDownloadListAsync: {0}", e);
                    ShellService.ShowError(e, "{0}: Error parsing post!", Blog.Name);
                }
            }
            await Task.CompletedTask;
        }

        private void AddPhotoUrlToDownloadList(Post post)
        {
            //if (!Blog.DownloadPhoto || !post.bPostTypeIx.Equals(PostType.Photo)) return;

            AddPhotoUrl(post);

            AddInlinePhotoUrl(post);

            if (Blog.RegExPhotos)
            {
                AddGenericInlinePhotoUrl(post);
            }
        }

        private void AddVideoUrlToDownloadList(Post post)
        {
            //if ((!Blog.DownloadVideo && !Blog.DownloadVideoThumbnail) || !post.bPostTypeIx.Equals(PostType.Video)) return;

            AddVideoUrl(post);
        }

        private void AddAudioUrlToDownloadList(Post post)
        {
            //if (!Blog.DownloadAudio || !post.bPostTypeIx.Equals(PostType.Audio)) return;

            AddAudioUrl(post);
        }

        private void AddTextUrlToDownloadList(Post post)
        {
            if (!post.bPostTypeIx.Equals(PostType.Text)) return;

            var data = ParseText(post);

            if (!Blog.DownloadText)
            {
                StatisticsBag.Add(new TextPost(data, null, null));
                return;
            }

            AddToDownloadList(new TextPost(data, GetPostId(post)));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(GetPostId(post), ".json"), post));
        }

        private void AddQuoteUrlToDownloadList(Post post)
        {
            if (!post.bPostTypeIx.Equals(PostType.Quote)) return;

            var data = ParseQuote(post);

            if (!Blog.DownloadQuote)
            {
                StatisticsBag.Add(new QuotePost(data, null, null));
                return;
            }

            AddToDownloadList(new QuotePost(data, GetPostId(post)));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(GetPostId(post), ".json"), post));
        }

        private void AddLinkUrlToDownloadList(Post post)
        {
            if (!post.bPostTypeIx.Equals(PostType.Link)) return;

            var data = ParseLink(post);

            if (!Blog.DownloadLink)
            {
                StatisticsBag.Add(new LinkPost(data, null, null));
                return;
            }

            AddToDownloadList(new LinkPost(data, GetPostId(post)));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(GetPostId(post), ".json"), post));
        }

        private void AddAnswerUrlToDownloadList(Post post)
        {
            if (!post.bPostTypeIx.Equals(PostType.Answer)) return;

            var data = ParseAnswer(post);

            if (!Blog.DownloadAnswer)
            {
                StatisticsBag.Add(new AnswerPost(data, null, null));
                return;
            }

            AddToDownloadList(new AnswerPost(data, GetPostId(post)));
            AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(GetPostId(post), ".json"), post));
        }

        private string ParseText(Post post)
        {
            var title = post.Parts.First(x => x.bPartTypeIx == PostType.Text).Medias[0].szSub;
            var text = post.Parts.First(x => x.bPartTypeIx == PostType.Text).Medias[0].szBody;
            if (Blog.MetadataFormat == Domain.Models.MetadataType.Json)
            {
                dynamic o = new ExpandoObject();
                o.Id = post.qwPostIx.Value;
                o.Url = $"{BaseUrl}{post.szExternal}";
                o.Title = title;
                o.Text = text;
                return JsonConvert.SerializeObject(o, Formatting.Indented);
            }
            else
            {
                return $"Id: {post.qwPostIx}\nUrl: {BaseUrl}{post.szExternal}\nTitle: {title}\nText: {text}\n";
            }
        }

        private string ParseQuote(Post post)
        {
            var quote = post.Parts.First(x => x.bPartTypeIx == PostType.Quote).Medias[0].szBody;
            var source = post.Parts.First(x => x.bPartTypeIx == PostType.Quote).Medias[0].szSub;
            var comment = post.Parts.FirstOrDefault(x => x.bPartTypeIx == PostType.Comment)?.Medias[0].szBody ?? "";
            if (Blog.MetadataFormat == Domain.Models.MetadataType.Json)
            {
                dynamic o = new ExpandoObject();
                o.Id = post.qwPostIx.Value;
                o.Url = $"{BaseUrl}{post.szExternal}";
                o.Quote = quote;
                o.Source = source;
                o.Comment = comment;
                return JsonConvert.SerializeObject(o, Formatting.Indented);
            }
            else
            {
                return $"Id: {post.qwPostIx}\nUrl: {BaseUrl}{post.szExternal}\nQuote: {quote}\nSource: {source}\nComment: {comment}\n";
            }
        }

        private string ParseLink(Post post)
        {
            var caption = post.Parts.First(x => x.bPartTypeIx == PostType.Link).Medias[0].szBody;
            var link = post.Parts.First(x => x.bPartTypeIx == PostType.Link).Medias[0].szSub;
            var comment = post.Parts.FirstOrDefault(x => x.bPartTypeIx == PostType.Comment)?.Medias[0].szBody ?? "";
            if (Blog.MetadataFormat == Domain.Models.MetadataType.Json)
            {
                dynamic o = new ExpandoObject();
                o.Id = post.qwPostIx.Value;
                o.Url = $"{BaseUrl}{post.szExternal}";
                o.Caption = caption;
                o.Link = link;
                o.Comment = comment;
                return JsonConvert.SerializeObject(o, Formatting.Indented);
            }
            else
            {
                return $"Id: {post.qwPostIx}\nUrl: {BaseUrl}{post.szExternal}\nCaption: {caption}\nLink: {link}\nComment: {comment}\n";
            }
        }

        private string ParseAnswer(Post post)
        {
            var question = post.Parts.First(x => x.bPartTypeIx == PostType.Answer).Medias[0].szBody;
            var answer = post.Parts.First(x => x.bPartTypeIx == PostType.Answer).Medias[0].szSub;
            var comment = post.Parts.FirstOrDefault(x => x.bPartTypeIx == PostType.Comment)?.Medias[0].szBody ?? "";
            if (Blog.MetadataFormat == Domain.Models.MetadataType.Json)
            {
                dynamic o = new ExpandoObject();
                o.Id = post.qwPostIx.Value;
                o.Url = $"{BaseUrl}{post.szExternal}";
                o.Question = question;
                o.Answer = answer;
                o.Comment = comment;
                return JsonConvert.SerializeObject(o, Formatting.Indented);
            }
            else
            {
                return $"Id: {post.qwPostIx}\nUrl: {BaseUrl}{post.szExternal}\nQuestion: {question}\nAnswer: {answer}\nComment: {comment}\n";
            }
        }

        private static int UnixTimestamp(Post post)
        {
            long postTime = ((DateTimeOffset)post.dtActive).ToUnixTimeSeconds();
            return (int)postTime;
        }

        private void AddPhotoUrl(Post post)
        {
            string firstImageUrl = null;
            var photoCount = post.Parts.Count(c => c.bPartTypeIx == PostType.Photo);
            int counter = 1;

            foreach (var part in post.Parts)
            {
                if (!part.bPartTypeIx.Equals(PostType.Photo)) continue;

                var media = part.Medias[0];
                var imageUrl = GetMediaUrl(blogIx, post.qwPostIx, part.nPartIz, part.qwPartIx, media.bMediaTypeIx, media.nWidth, media.nHeight, 0);

                if (!Blog.DownloadPhoto && post.bPostTypeIx.Equals(PostType.Photo))
                {
                    StatisticsBag.Add(new PhotoPost(imageUrl, null, null, null));
                    continue;
                }

                if (firstImageUrl == null) firstImageUrl = imageUrl;

                var index = photoCount > 1 ? counter++ : -1;
                var filename = BuildFileName(imageUrl, post, "photo", index);
                AddDownloadedMedia(imageUrl, filename, post);
                AddToDownloadList(new PhotoPost(imageUrl, null, GetPostId(part), UnixTimestamp(post).ToString(), filename));
            }
            if (firstImageUrl != null)
            {
                AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(firstImageUrl.Split('/').Last(), ".json"), post));
            }
        }

        private static string InlineSearch(Post post)
        {
            var s = "";
            foreach (var part in post.Parts)
            {
                foreach (var media in part.Medias)
                {
                    s += $"{media.szSub} {media.szBody} ";
                }
            }
            return s;
        }

        private void AddInlinePhotoUrl(Post post)
        {
            string firstImageUrl = null;
            foreach (string imageUrl in newTumblParser.SearchForPhotoUrl(InlineSearch(post)))
            {
                if (CheckIfShouldStop()) { return; }
                CheckIfShouldPause();

                if (CheckIfSkipGif(imageUrl)) { continue; }
                if (firstImageUrl == null) { firstImageUrl = imageUrl; }

                var filename = BuildFileName(imageUrl, post, "photo", -1);
                AddDownloadedMedia(imageUrl, filename, post);
                AddToDownloadList(new PhotoPost(imageUrl, null, GetPostId(post), UnixTimestamp(post).ToString(), filename));
            }
            if (firstImageUrl != null)
            {
                AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(GetPostId(post), ".json"), post));
            }
        }

        private void AddGenericInlinePhotoUrl(Post post)
        {
            string firstImageUrl = null;
            foreach (string imageUrl in newTumblParser.SearchForGenericPhotoUrl(InlineSearch(post)))
            {
                if (CheckIfSkipGif(imageUrl)) { continue; }
                if (newTumblParser.IsNewTumblUrl(imageUrl)) { continue; }
                if (firstImageUrl == null) { firstImageUrl = imageUrl; }

                AddDownloadedMedia(imageUrl, FileName(imageUrl), post);
                AddToDownloadList(new PhotoPost(imageUrl, null, GetPostId(post), UnixTimestamp(post).ToString(), FileName(imageUrl)));
            }
            if (firstImageUrl != null)
            {
                AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(GetPostId(post), ".json"), post));
            }
        }

        private void AddVideoUrl(Post post)
        {
            string firstVideoUrl = null;

            foreach (var part in post.Parts)
            {
                if (!post.bPostTypeIx.Equals(PostType.Video) || !part.bPartTypeIx.Equals(PostType.Video)) continue;

                var media = part.Medias[0];
                var videoUrl = GetMediaUrl(blogIx, post.qwPostIx, part.nPartIz, part.qwPartIx, media.bMediaTypeIx, media.nWidth, media.nHeight, 0);
                
                if (Blog.DownloadVideo)
                {
                    if (firstVideoUrl == null) firstVideoUrl = videoUrl;
                    var filename = BuildFileName(videoUrl, post, "video", -1);
                    AddDownloadedMedia(videoUrl, filename, post);
                    AddToDownloadList(new VideoPost(videoUrl, GetPostId(part), UnixTimestamp(post).ToString(), filename));
                }
                else
                {
                    StatisticsBag.Add(new VideoPost(videoUrl, null, null));
                }

                var imageUrl = GetMediaUrl(blogIx, post.qwPostIx, part.nPartIz, part.qwPartIx, media.bMediaTypeIx, media.nWidth, media.nHeight, 300);

                if (Blog.DownloadVideoThumbnail)
                {
                    var filename = FileName(imageUrl);
                    filename = BuildFileName(filename, post, "photo", -1);
                    AddDownloadedMedia(imageUrl, filename, post);
                    AddToDownloadList(new PhotoPost(imageUrl, null, GetPostId(part, true), UnixTimestamp(post).ToString(), filename));
                    if (!Blog.DownloadVideo)
                    {
                        AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(imageUrl.Split('/').Last(), ".json"), post));
                    }
                }
                else
                {
                    StatisticsBag.Add(new PhotoPost(imageUrl, null, null, null));
                }
            }
            if (firstVideoUrl != null)
            {
                AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(firstVideoUrl.Split('/').Last(), ".json"), post));
            }
        }

        private void AddAudioUrl(Post post)
        {
            string firstAudioUrl = null;

            foreach (var part in post.Parts)
            {
                if (!part.bPartTypeIx.Equals(PostType.Audio)) continue;

                var media = part.Medias[0];
                var audioUrl = GetMediaUrl(blogIx, post.qwPostIx, part.nPartIz, part.qwPartIx, media.bMediaTypeIx, media.nWidth, media.nHeight, 0);

                if (!Blog.DownloadAudio && post.bPostTypeIx.Equals(PostType.Audio))
                {
                    StatisticsBag.Add(new AudioPost(audioUrl, null, null));
                    continue;
                }

                if (firstAudioUrl == null) firstAudioUrl = audioUrl;

                var filename = BuildFileName(audioUrl, post, "audio", -1);
                AddDownloadedMedia(audioUrl, filename, post);
                AddToDownloadList(new AudioPost(audioUrl, GetPostId(part), UnixTimestamp(post).ToString(), filename));
            }
            if (firstAudioUrl != null)
            {
                AddToJsonQueue(new CrawlerData<Post>(Path.ChangeExtension(firstAudioUrl.Split('/').Last(), ".json"), post));
            }
        }

        private async Task<bool> CheckIfLoggedInAsync()
        {
            try
            {
                blogJson = await GetApiPageAsync(0);
                var user = BlogInfo(blogJson, 3);
                var blog = BlogInfo(blogJson, 2);
                if ((user?.bLoggedIn ?? 0) == 0 && blog?.bRatingIx > 2 || user?.bRatingIx < blog?.bRatingIx)
                {
                    var msg = ((user?.bRatingIx < blog?.bRatingIx) ? Resources.BlogOverrated : Resources.NotLoggedInNT);
                    var errorMsg = $"{Blog.Name} ({ShellService.Settings.GetCollection(Blog.CollectionId).Name}): {msg.Replace(Environment.NewLine, " ")}";
                    Logger.Error($"NewTumblCrawler:CheckIfLoggedInAsync: {errorMsg}");
                    ShellService.ShowError(new Exception(msg), errorMsg);
                }
                return (user?.bLoggedIn ?? 0) == 1;
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
        }

        private async Task<string> GetApiPageAsync(int mode)
        {
            if (ShellService.Settings.LimitConnectionsSearchApi)
            {
                CrawlerService.TimeconstraintSearchApi.Acquire();
            }
            switch (mode)
            {
                case 0:
                    string json = null;
                    Root obj = null;
                    for (int numberOfTrials = 0; numberOfTrials < 2; numberOfTrials++)
                    {
                        try
                        {
                            var test = Blog.Url;
                            string document = await RequestDataAsync(test, null, cookieHosts);

                            json = extractJsonFromPage.Match(document).Groups[1].Value + "}";
                            obj = ConvertJsonToClassNew<Root>(json);
                            CheckError(obj);
                            break;
                        }
                        catch (APIException ex)
                        {
                            if (numberOfTrials == 0)
                            {
                                Logger.Error($"GetApiPageAsync, retrying: {ex.Message}");
                                await Task.Delay(10000);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    if (obj.aResultSet[2].aRow.Count == 0)
                    {
                        throw CreateWebException(HttpStatusCode.NotFound);
                    }
                    blogIx = blogIx > 0 ? blogIx : BlogInfo(obj, isLike ? 2 : 8)?.dwBlogIx ?? 0;
                    return json;
                case 1:
                    var cookies = CookieService.GetAllCookies();
                    var cookie = cookies.FirstOrDefault(c => c.Name == "Affinity");
                    var affinity = cookie?.Value ?? "";
                    cookie = cookies.FirstOrDefault(c => c.Name == "LoginToken");
                    var token = cookie?.Value ?? "";
                    cookie = cookies.FirstOrDefault(c => c.Name == "ActiveBlog");
                    var activeBlog = long.TryParse(cookie?.Value, out var result) ? result : 0;

                    var url = "https://api-ro.newtumbl.com/sp/NewTumbl/" +
                        (isLike ? "search_User_Posts_Like" : "search_Blog_Posts") + $"?affinity={affinity}";

                    var d = new Dictionary<string, string>();
                    d.Add("json", "{\"Params\":[\"[{IPADDRESS}]\",\"" + token + "\",null,\"0\",\"0\"," + activeBlog + ",null," +
                        (dtSearchField == null ? "null" : $"\"{dtSearchField}\"") + "," + pageNo + ",50,0,null,0,\"\",0,0,0,0,0," +
                        (isLike ? "null" : blogIx.ToString()) + ",null]}");
                    
                    return await PostDataAsync(url, Blog.Url, d, cookieHosts);
                default:
                    throw new NotImplementedException();
            }
        }

        private void CheckError(Root obj)
        {
            if (obj.nResult == "-1")
            {
                Logger.Error($"server returned: {obj.aResultSet[0].aRow[0].szError} ({obj.aResultSet[0].aRow[0].dwError})");
                throw new ApplicationException($"{obj.aResultSet[0].aRow[0].szError} ({obj.aResultSet[0].aRow[0].dwError})");
            }
            else if (obj.nResult == "-9999")
            {
                throw new APIException($"{obj.sError}({obj.sAPIErrorCode}): {obj.sAPIErrorMessage}");
            }
        }

        private List<Post> GetPosts(string json)
        {
            var obj = ConvertJsonToClassNew<Root>(json);
            CheckError(obj);
            var posts = PostInfo(obj, 3);
            var tags = PostInfo(obj, 5);
            var parts = PostInfo(obj, 4);
            var medias = PostInfo(obj, 1);

            List<Post> list = new List<Post>();
            foreach (var post in posts.OrderByDescending(o => o.qwPostIx))
            {
                var item = Post.Create(post);
                item.Tags = tags.Where(w => w.qwPostIx == item.qwPostIx).OrderBy(o => o.bOrder).Select(s => Tag.Create(s)).ToList();
                item.Parts = parts.Where(w => w.qwPostIx == item.qwPostIx).OrderBy(o => o.bOrder).Select(s => Part.Create(s)).ToList();
                foreach (var part in item.Parts)
                {
                    part.Medias = medias.Where(w => w.qwPartIx == part.qwPartIx).OrderBy(o => o.bOrder).Select(s => Media.Create(s)).ToList();
                }
                list.Add(item);
            }
            
            return list;
        }

        private static string GetPostId(Post post)
        {
            return post.qwPostIx.ToString();
        }

        private static string GetPostId(Part part, bool isThumb = false)
        {
            // url filenames are unique and can't identify duplicates, so use mediaIx for now
            return part.Medias[0].qwMediaIx.ToString() + (isThumb ? "T" : "");
        }

        private string GetBlogName()
        {
            return BlogInfo(blogJson, 2).szBlogId;
        }

        private ARow BlogInfo(string json, int type)
        {
            var obj = ConvertJsonToClassNew<Root>(json);
            CheckError(obj);
            return BlogInfo(obj, type);
        }

        private ARow BlogInfo(Root obj, int type)
        {
            /*
             * 0.0 - user account details
             * 1.1 - blog image
             * 1.2 - blog banner
             * 2.0 - blog info
             * 3.0 - user settings
             * 4.0 - user's active blog
             * 7.0 - search time
             * 8.0 - blog stati
             * 12 - genres
             * */
            switch (type)
            {
                case 2:
                    return isLike ? obj.aResultSet[type].aRow[0] : obj.aResultSet[type].aRow.Where(w => w.dwBlogIx == blogIx).First();
                case 3:
                    return obj.aResultSet[type].aRow[0];
                case 8:
                    return obj.aResultSet[type].aRow[0];
            }
            return null;
        }

        private List<ARow> PostInfo(string json, int type)
        {
            var obj = ConvertJsonToClassNew<Root>(json);
            CheckError(obj);
            return PostInfo(obj, type);
        }

        private static List<ARow> PostInfo(Root obj, int type)
        {
            /*
             * 1.x - post part medias
             * 2.x - blog info
             * 3.x - posts
             * 4.x - post parts
             * 5.x - post tags
             * 7.0 - search time
             */
            switch (type)
            {
                case 1:
                    return obj.aResultSet[type].aRow;
                case 3:
                    return obj.aResultSet[type].aRow;
                case 4:
                    return obj.aResultSet[type].aRow;
                case 5:
                    return obj.aResultSet[type].aRow;
                case 7:
                    return obj.aResultSet[type].aRow;
            }
            return null;
        }

        private static byte[] GetSHA256(string msg)
        {
            using (SHA256 sha256 = SHA256Managed.Create())
            {
                var buffer = Encoding.ASCII.GetBytes(msg);
                byte[] bytes = sha256.ComputeHash(buffer);
                return bytes;
            }
        }

        private static string GetBase32(byte[] input)
        {
            const string Map = "abcdefghijknpqrstuvxyz0123456789";
            var output = "";
            int i = -1;
            int b = 0;
            int c = 0;
            int d;
            while (i < input.Length || b > 0)
            {
                if (b < 5)
                {
                    if (++i < input.Length)
                    {
                        c = (c << 8) + input[i];
                        b += 8;
                    }
                }
                d = c % 32;
                c >>= 5;
                b -= 5;
                output += Map[d];
            }
            return output;
        }

        private string GetMediaUrl(long dwBlogIx, long? qwPostIx, int? nPartIz, long? qwPartIx, int? bMediaTypeIx, int? nWidth, int? nHeigh, int nThumb)
        {
            string sURL = null;
            bool bThumb = false;
            bool bJPG = false;

            if (new int[] { 4, 6, 7, 2, 3 }.Contains(bMediaTypeIx.Value))
            {
                bJPG = (bMediaTypeIx == 4 || bMediaTypeIx == 6 || bMediaTypeIx == 7) && nThumb > 0;
                bThumb = (nThumb == 600 && nHeigh > 800) || (nThumb == 1200 && nHeigh > 1200) ? true : nThumb > 0 && nWidth > nThumb;
            }
            else if (bMediaTypeIx != 5)
            {
                sURL = "";
            }
            if (sURL == null)
            {
                var sPath = $"/{dwBlogIx}/{qwPostIx}/{nPartIz}/{qwPartIx}/nT_";
                var sThumb = bThumb ? "_" + nThumb : "";
                var sExt = bThumb || bJPG ? "jpg" : aExt[bMediaTypeIx.Value];
                var sHost = "dn" + (qwPostIx % 4);
                sURL = "https://" + sHost + ".newtumbl.com/img" + sPath + GetBase32(GetSHA256(sPath)).Substring(0, 24) + sThumb + "." + sExt;
            }
            return sURL;
        }

        private static WebException CreateWebException(HttpStatusCode httpStatus)
        {
            
            using (HttpListener listener = new HttpListener())
            {
                string prefix = "";
                int port = 56789;
                int count = 0;
                do
                {
                    count++;
                    try
                    {
                        prefix = $"http://localhost:{port}/mocking/";
                        listener.Prefixes.Clear();
                        listener.Prefixes.Add(prefix);
                        listener.Start();
                        port = 0;
                    }
                    catch (NotSupportedException)
                    {
                        port = 0;
                    }
                    catch (HttpListenerException)
                    {
                        port = new Random().Next(50000, 65000);
                    }
                } while (port != 0 && count < 3);
                try
                {
                    listener.BeginGetContext((ar) =>
                    {
                        HttpListenerContext context = listener.EndGetContext(ar);
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;
                        response.StatusCode = (int)httpStatus;
                        response.Close();
                    }, null);

                    using (WebClient client = new WebClient())
                        try
                        {
                            client.OpenRead(prefix + "error.aspx");
                        }
                        catch (WebException e)
                        {
                            HttpWebResponse httpWebResponse = e.Response as HttpWebResponse;
                            return new WebException("Error", null, WebExceptionStatus.ProtocolError, e.Response);
                        }
                }
                finally
                {
                    listener.Stop();
                }
            }
            return null;
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
