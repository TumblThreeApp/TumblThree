using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.Serialization;
using System.Windows;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Properties
{
    [Export(typeof(AppSettings))]
    public sealed class AppSettings : IExtensibleDataObject
    {
        [IgnoreDataMember]
        public static readonly string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";

        [IgnoreDataMember]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "<Pending>")]
        public static readonly string SETTINGS_FILENAME = "Settings.json";

        private static readonly string[] blogTypes =
            {
                Resources.BlogTypesNone, Resources.BlogTypesAll, Resources.BlogTypesOnceFinished, Resources.BlogTypesNeverFinished
            };

        private static readonly string[] imageSizes =
            {
                "best", "1280", "500", "400", "250", "100", "75"
            };

        private static readonly string[] imageSizeCategories =
            {
                "best", "large", "medium", "small", "thumb"
            };

        private static readonly string[] videoSizes =
            {
                "1080", "480"
            };

        private static readonly string[] videoSizeCategories =
            {
                "large", "medium", "small"
            };

        private static string[] logLevels = Enum.GetNames(typeof(System.Diagnostics.TraceLevel));

        private static string[] pnjDownloadFormats = Enum.GetNames(typeof(PnjDownloadType));

        public AppSettings()
        {
            Initialize();
        }

        [DataMember]
        public string RequestTokenUrl { get; set; }

        [DataMember]
        public string AuthorizeUrl { get; set; }

        [DataMember]
        public string AccessTokenUrl { get; set; }

        [DataMember]
        public string OAuthCallbackUrl { get; set; }

        [DataMember]
        public string ApiKey { get; set; }

        [DataMember]
        public string SecretKey { get; set; }

        [DataMember]
        public string UserAgent { get; set; }

        [DataMember]
        public string OAuthToken { get; set; }

        [DataMember]
        public string OAuthTokenSecret { get; set; }

        [DataMember]
        public double Left { get; set; }

        [DataMember]
        public double Top { get; set; }

        [DataMember]
        public double Height { get; set; }

        [DataMember]
        public double Width { get; set; }

        [DataMember]
        public bool IsMaximized { get; set; }

        [DataMember]
        public double GridSplitterPosition { get; set; }

        [DataMember]
        public string DownloadLocation { get; set; }

        [DataMember]
        public string ExportLocation { get; set; }

        [DataMember]
        public int ConcurrentConnections { get; set; }

        [DataMember]
        public int ConcurrentVideoConnections { get; set; }

        [DataMember]
        public int ConcurrentBlogs { get; set; }

        [DataMember]
        public int ConcurrentScans { get; set; }

        [DataMember]
        public bool LimitScanBandwidth { get; set; }

        [DataMember]
        public int TimeOut { get; set; }

        [DataMember]
        public double ProgressUpdateInterval { get; set; }

        [DataMember]
        public bool LimitConnectionsApi { get; set; }

        [DataMember]
        public int MaxConnectionsApi { get; set; }

        [DataMember]
        public int ConnectionTimeIntervalApi { get; set; }

        [DataMember]
        public bool LimitConnectionsSearchApi { get; set; }

        [DataMember]
        public int MaxConnectionsSearchApi { get; set; }

        [DataMember]
        public int ConnectionTimeIntervalSearchApi { get; set; }

        [DataMember]
        public bool LimitConnectionsSvc { get; set; }

        [DataMember]
        public int MaxConnectionsSvc { get; set; }

        [DataMember]
        public int ConnectionTimeIntervalSvc { get; set; }

        [DataMember]
        public bool LimitConnectionsTwitterApi { get; set; }

        [DataMember]
        public int MaxConnectionsTwitterApi { get; set; }

        [DataMember]
        public int ConnectionTimeIntervalTwitterApi { get; set; }

        [DataMember]
        public int MaxNumberOfRetries { get; set; }

        [DataMember]
        public long Bandwidth { get; set; }

        [DataMember]
        public int BufferSize { get; set; }

        [DataMember]
        public string ImageSize { get; set; }

        [DataMember]
        public string ImageSizeCategory { get; set; }

        [DataMember]
        public int VideoSize { get; set; }

        [DataMember]
        public string VideoSizeCategory { get; set; }

        [DataMember]
        public string BlogType { get; set; }

        [DataMember]
        public bool CheckClipboard { get; set; }

        [DataMember]
        public bool ShowPicturePreview { get; set; }

        [DataMember]
        public bool DisplayConfirmationDialog { get; set; }

        [DataMember]
        public bool DeleteOnlyIndex { get; set; }

        [DataMember]
        public bool ArchiveIndex { get; set; }

        [DataMember]
        public bool CheckOnlineStatusOnStartup { get; set; }

        [DataMember]
        public bool SkipGif { get; set; }

        [DataMember]
        public bool DownloadVideoThumbnails { get; set; }

        [DataMember]
        public bool EnablePreview { get; set; }

        [DataMember]
        public bool RemoveIndexAfterCrawl { get; set; }

        [DataMember]
        public bool DownloadImages { get; set; }

        [DataMember]
        public bool DownloadVideos { get; set; }

        [DataMember]
        public bool DownloadAudios { get; set; }

        [DataMember]
        public bool DownloadTexts { get; set; }

        [DataMember]
        public bool DownloadQuotes { get; set; }

        [DataMember]
        public bool DownloadConversations { get; set; }

        [DataMember]
        public bool DownloadAnswers { get; set; }

        [DataMember]
        public bool DownloadReplies { get; set; }

        [DataMember]
        public bool DownloadLinks { get; set; }

        [DataMember]
        public string DownloadPages { get; set; }

        [DataMember]
        public int PageSize { get; set; }

        [DataMember]
        public string DownloadFrom { get; set; }

        [DataMember]
        public string DownloadTo { get; set; }

        [DataMember]
        public string Tags { get; set; }

        [DataMember]
        public bool CreateImageMeta { get; set; }

        [DataMember]
        public bool CreateVideoMeta { get; set; }

        [DataMember]
        public bool CreateAudioMeta { get; set; }

        [DataMember]
        public MetadataType MetadataFormat { get; set; }

        [DataMember]
        public bool DumpCrawlerData { get; set; }

        [DataMember]
        public bool RegExPhotos { get; set; }

        [DataMember]
        public bool RegExVideos { get; set; }

        [DataMember]
        public bool DownloadRebloggedPosts { get; set; }

        [DataMember]
        public bool DownloadGfycat { get; set; }

        [DataMember]
        public GfycatTypes GfycatType { get; set; }

        [DataMember]
        public bool DownloadImgur { get; set; }

        [DataMember]
        public bool DownloadWebmshare { get; set; }

        [DataMember]
        public WebmshareTypes WebmshareType { get; set; }

        [DataMember]
        public bool DownloadUguu { get; set; }

        [DataMember]
        public UguuTypes UguuType { get; set; }

        [DataMember]
        public bool DownloadCatBox { get; set; }

        [DataMember]
        public CatBoxTypes CatBoxType { get; set; }

        [DataMember]
        public bool OverrideTumblrBlogCrawler { get; set; }

        [DataMember]
        public TumblrBlogCrawlerTypes TumblrBlogCrawlerType { get; set; }

        [DataMember]
        public bool AutoDownload { get; set; }

        [DataMember]
        public string TimerInterval { get; set; }

        [DataMember]
        public bool ForceSize { get; set; }

        [DataMember]
        public bool ForceRescan { get; set; }

        [DataMember]
        public bool CheckDirectoryForFiles { get; set; }

        [DataMember]
        public bool DownloadUrlList { get; set; }

        [DataMember]
        public bool PortableMode { get; set; }

        [DataMember]
        public bool LoadAllDatabases { get; set; }

        [DataMember]
        public bool LoadArchive { get; set; }

        [DataMember]
        public string ProxyHost { get; set; }

        [DataMember]
        public string ProxyPort { get; set; }

        [DataMember]
        public string ProxyUsername { get; set; }

        [DataMember]
        public string ProxyPassword { get; set; }

        [DataMember]
        public string LogLevel { get; set; }

        [DataMember]
        public string FilenameTemplate { get; set; }

        [DataMember]
        public bool GroupPhotoSets { get; set; }

        [DataMember]
        public int SettingsTabIndex { get; set; }

        [DataMember]
        public DateTime LastUpdateCheck { get; set; }

        [DataMember]
        public DateTime TMLastCheck { get; set; }

        [DataMember]
        public string Language { get; set; }

        [DataMember]
        public List<Collection> Collections { get; set; }

        [DataMember]
        public int ActiveCollectionId { get; set; }

        [DataMember]
        public string PnjDownloadFormat { get; set; }

        [DataMember]
        public bool TumblrAuthErrorAutoRetry { get; set; }

        [DataMember]
        public bool HideToolBarButtonsText { get; set; }

        [DataMember]
        public bool DequeueSelectedCommandVisible { get; set; }

        [DataMember]
        public bool FreeDiskSpaceMonitorEnabled { get; set; }

        [DataMember]
        public int FreeDiskSpaceMonitorInterval { get; set; }

        [DataMember]
        public int FreeDiskSpaceMonitorLevel { get; set; }

        [DataMember]
        public Dictionary<object, Tuple<int, double, Visibility>> ColumnSettings { get; set; }

        public static ObservableCollection<string> ImageSizes => new ObservableCollection<string>(imageSizes);

        public static ObservableCollection<string> ImageSizeCategories => new ObservableCollection<string>(imageSizeCategories);

        public static ObservableCollection<string> VideoSizes => new ObservableCollection<string>(videoSizes);

        public static ObservableCollection<string> VideoSizeCategories => new ObservableCollection<string>(videoSizeCategories);

        public static ObservableCollection<string> BlogTypes => new ObservableCollection<string>(blogTypes);

        public static ObservableCollection<string> LogLevels => new ObservableCollection<string>(logLevels);

        public static ObservableCollection<string> PnjDownloadFormats => new ObservableCollection<string>(pnjDownloadFormats);

        [IgnoreDataMember]
        public static ObservableCollection<KeyValuePair<string, string>> Languages
        {
            get
            {
                var languages = new ObservableCollection<KeyValuePair<string, string>>();
                var cultures = GetAvailableCultures();
                foreach (CultureInfo culture in cultures)
                {
                    languages.Add(new KeyValuePair<string, string>(culture.Name, culture.NativeName + " (" +
                        (culture.NativeName == culture.EnglishName ? "" : culture.EnglishName + " ") + "[" + culture.Name + "])"));
                }
                return languages;
            }
        }

        public static bool Upgrade(AppSettings settings)
        {
            _ = settings ?? throw new ArgumentNullException(nameof(settings));

            var updated = false;
            var newStgs = new AppSettings();

            if (settings.UserAgent != newStgs.UserAgent)
            {
                var regex = new System.Text.RegularExpressions.Regex(@"Mozilla\/[\d]+\.[\d]+ \(Windows [ \.\w\d]*; Win(64|32); (x64|x86)\) AppleWebKit\/[\d]+\.[\d]+ \(KHTML, like Gecko\) Chrome\/([\d]+\.[\d]+\.[\d]+\.[\d]+) Safari\/[\d]+\.[\d]+");
                var matchOld = regex.Match(settings.UserAgent);
                var matchNew = regex.Match(newStgs.UserAgent);
                if (matchOld.Success && matchNew.Success && Version.Parse(matchNew.Groups[3].Value) > Version.Parse(matchOld.Groups[3].Value))
                {
                    settings.UserAgent = newStgs.UserAgent;
                    updated = true;
                }
            }

            if (settings.ImageSize == "raw")
            {
                settings.ImageSize = "best";
                updated = true;
            }

            if (settings.LastUpdateCheck == new DateTime(1, 1, 1))
            {
                settings.LastUpdateCheck = new DateTime(1970, 1, 1);
                updated = true;
            }
            if (settings.TMLastCheck == new DateTime(1, 1, 1))
            {
                settings.TMLastCheck = new DateTime(1970, 1, 1);
                updated = true;
            }

            if (settings.ColumnSettings.Count > 0 && settings.ColumnSettings.ContainsKey("Date Added"))
            {
                var newCols = new Dictionary<object, Tuple<int, double, Visibility>>();
                foreach (var item in settings.ColumnSettings)
                {
                    var key = (string)item.Key;
                    if (key == "Downloaded Files")
                        key = "DownloadedItems";
                    else if (key == "Number of Downloads")
                        key = "TotalCount";
                    else if (key == "Date Added")
                        key = "DateAdded";
                    else if (key == "Last Complete Crawl")
                        key = "LastCompleteCrawl";
                    else if (key == "Latest Post")
                        key = "LatestPost";
                    else if (key == "Personal Notes")
                        key = "Notes";
                    else if (key == "Type")
                        key = "BlogType";
                    newCols.Add(key, new Tuple<int, double, Visibility>(item.Value.Item1, item.Value.Item2, item.Value.Item3));
                }
                settings.ColumnSettings = newCols;
                updated = true;
            }
            if (settings.ColumnSettings.Count > 0 && !settings.ColumnSettings.ContainsKey("LatestPost"))
            {
                settings.ColumnSettings.Add("LatestPost", Tuple.Create(9, 120.0, Visibility.Visible));
                updated = true;
            }
            if (settings.ColumnSettings.Count > 0 && !settings.ColumnSettings.ContainsKey("Collection"))
            {
                settings.ColumnSettings.Add("Collection", Tuple.Create(12, 120.0, Visibility.Visible));
                updated = true;
            }
            if (settings.ColumnSettings.Count > 0 && !settings.ColumnSettings.ContainsKey("DownloadedItemsNew"))
            {
                var newList = new Dictionary<object, Tuple<int, double, Visibility>>();
                foreach (var col in settings.ColumnSettings)
                {
                    if (col.Value.Item1 == 2)
                    {
                        newList.Add("DownloadedItemsNew", Tuple.Create(2, 60.0, Visibility.Visible));
                    }
                    newList.Add(col.Key, Tuple.Create(col.Value.Item1 + (col.Value.Item1 >= 2 ? 1 : 0), col.Value.Item2, col.Value.Item3));
                }
                settings.ColumnSettings = newList;
                updated = true;
            }
            if (string.IsNullOrEmpty(settings.ImageSizeCategory))
            {
                settings.ImageSizeCategory = "medium";
                settings.VideoSizeCategory = "medium";
                updated = true;
            }
            if (settings.Collections == null)
            {
                var collections = new List<Collection>();
                collections.Add(new Collection() { Id = 0, Name = Resources.DefaultCollectionName, DownloadLocation = settings.DownloadLocation });
                settings.Collections = collections;
            }
            else
            {
                var collection = settings.Collections.First(x => x.Id == 0);
                if (collection.Name != Resources.DefaultCollectionName)
                {
                    collection.Name = Resources.DefaultCollectionName;
                    updated = true;
                }
            }

            return updated;
        }

        public Collection GetCollection(int id)
        {
            return Collections.Where(x => x.Id == id).First();
        }

        ExtensionDataObject IExtensibleDataObject.ExtensionData { get; set; }

        private void Initialize()
        {
            RequestTokenUrl = @"https://www.tumblr.com/oauth/request_token";
            AuthorizeUrl = @"https://www.tumblr.com/oauth/authorize";
            AccessTokenUrl = @"https://www.tumblr.com/oauth/access_token";
            OAuthCallbackUrl = @"https://github.com/TumblThreeApp/TumblThree";
            ApiKey = "x8pd1InspmnuLSFKT4jNxe8kQUkbRXPNkAffntAFSk01UjRsLV";
            SecretKey = "Mul4BviRQgPLuhN1xzEqmXzwvoWicEoc4w6ftWBGWtioEvexmM";
            UserAgent = USERAGENT;
            OAuthToken = string.Empty;
            OAuthTokenSecret = string.Empty;
            Left = 50;
            Top = 50;
            Height = 800;
            Width = 1200;
            IsMaximized = false;
            GridSplitterPosition = 250;
            DownloadLocation = @"Blogs";
            ExportLocation = @"blogs.txt";
            ConcurrentConnections = 8;
            ConcurrentVideoConnections = 4;
            ConcurrentBlogs = 1;
            ConcurrentScans = 4;
            LimitScanBandwidth = false;
            TimeOut = 60;
            LimitConnectionsApi = true;
            MaxConnectionsApi = 90;
            ConnectionTimeIntervalApi = 60;
            LimitConnectionsSearchApi = true;
            MaxConnectionsSearchApi = 90;
            ConnectionTimeIntervalSearchApi = 60;
            LimitConnectionsSvc = true;
            MaxConnectionsSvc = 90;
            ConnectionTimeIntervalSvc = 60;
            LimitConnectionsTwitterApi = true;
            MaxConnectionsTwitterApi = 5;
            ConnectionTimeIntervalTwitterApi = 60;
            MaxNumberOfRetries = 3;
            ProgressUpdateInterval = 100;
            Bandwidth = 0;
            BufferSize = 512;
            ImageSize = "best";
            VideoSize = 1080;
            ImageSizeCategory = "";
            VideoSizeCategory = "";
            BlogType = "None";
            CheckClipboard = true;
            ShowPicturePreview = true;
            DisplayConfirmationDialog = true;
            DeleteOnlyIndex = true;
            ArchiveIndex = false;
            CheckOnlineStatusOnStartup = false;
            SkipGif = false;
            DownloadVideoThumbnails = true;
            EnablePreview = true;
            RemoveIndexAfterCrawl = false;
            DownloadImages = true;
            DownloadVideos = true;
            DownloadTexts = true;
            DownloadAudios = true;
            DownloadQuotes = true;
            DownloadConversations = true;
            DownloadLinks = true;
            DownloadAnswers = true;
            CreateImageMeta = false;
            CreateVideoMeta = false;
            CreateAudioMeta = false;
            DownloadReplies = false;
            MetadataFormat = MetadataType.Text;
            OverrideTumblrBlogCrawler = false;
            TumblrBlogCrawlerType = TumblrBlogCrawlerTypes.TumblrSVC;
            PageSize = 50;
            DownloadRebloggedPosts = true;
            AutoDownload = false;
            TimerInterval = "22:40:00";
            ForceSize = false;
            ForceRescan = false;
            CheckDirectoryForFiles = false;
            DownloadUrlList = false;
            PortableMode = false;
            LoadAllDatabases = false;
            LoadArchive = false;
            ProxyHost = string.Empty;
            ProxyPort = string.Empty;
            ProxyUsername = string.Empty;
            ProxyPassword = string.Empty;
#if DEBUG
            LogLevel = nameof(System.Diagnostics.TraceLevel.Verbose);
#else
            LogLevel = nameof(System.Diagnostics.TraceLevel.Info);
#endif
            GroupPhotoSets = false;
            FilenameTemplate = "%f";
            LastUpdateCheck = new DateTime(1970, 1, 1);
            TMLastCheck = new DateTime(1970, 1, 1);
            Language = "en-US";
            ColumnSettings = new Dictionary<object, Tuple<int, double, Visibility>>();
            PnjDownloadFormat = nameof(PnjDownloadType.jpg);
            TumblrAuthErrorAutoRetry = false;
            HideToolBarButtonsText = false;
            DequeueSelectedCommandVisible = false;
            FreeDiskSpaceMonitorEnabled = true;
            FreeDiskSpaceMonitorInterval = 5;
            FreeDiskSpaceMonitorLevel = 1024;
        }

        [OnDeserializing]
        private static void OnDeserializing(StreamingContext context)
        {
        }

        private static IEnumerable<CultureInfo> GetAvailableCultures()
        {
            List<CultureInfo> result = new List<CultureInfo>();

            ResourceManager rm = new ResourceManager(typeof(Resources));

            CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            foreach (CultureInfo culture in cultures)
            {
                try
                {
                    if (culture.Equals(CultureInfo.InvariantCulture)) continue; //do not use "==", won't work

                    ResourceSet rs = rm.GetResourceSet(culture, true, false);
                    if (rs != null)
                        result.Add(culture);
                }
                catch (CultureNotFoundException)
                {
                    //NOP
                }
            }
            return result;
        }
    }
}
