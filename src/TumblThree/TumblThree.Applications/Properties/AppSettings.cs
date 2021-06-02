using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Resources;
using System.Runtime.Serialization;
using System.Windows;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Properties
{
    [Export(typeof(AppSettings))]
    public sealed class AppSettings : IExtensibleDataObject
    {
        private static readonly string[] blogTypes =
            {
                Resources.BlogTypesNone, Resources.BlogTypesAll, Resources.BlogTypesOnceFinished, Resources.BlogTypesNeverFinished
            };

        private static readonly string[] imageSizes =
            {
                "best", "1280", "500", "400", "250", "100", "75"
            };

        private static readonly string[] videoSizes =
            {
                "1080", "480"
            };

        private static string[] tumblrHosts =
            {
                "data.tumblr.com"
            };

        private static string[] logLevels = Enum.GetNames(typeof(System.Diagnostics.TraceLevel));

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
        public int MaxNumberOfRetries { get; set; }

        [DataMember]
        public long Bandwidth { get; set; }

        [DataMember]
        public int BufferSize { get; set; }

        [DataMember]
        public string ImageSize { get; set; }

        [DataMember]
        public int VideoSize { get; set; }

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
        public bool DownloadMixtape { get; set; }

        [DataMember]
        public MixtapeTypes MixtapeType { get; set; }

        [DataMember]
        public bool DownloadUguu { get; set; }

        [DataMember]
        public UguuTypes UguuType { get; set; }

        [DataMember]
        public bool DownloadSafeMoe { get; set; }

        [DataMember]
        public SafeMoeTypes SafeMoeType { get; set; }

        [DataMember]
        public bool DownloadLoliSafe { get; set; }

        [DataMember]
        public LoliSafeTypes LoliSafeType { get; set; }

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
        public string Language { get; set; }

        [DataMember]
        public Dictionary<object, Tuple<int, double, Visibility>> ColumnSettings { get; set; }

        public ObservableCollection<string> ImageSizes => new ObservableCollection<string>(imageSizes);

        public ObservableCollection<string> VideoSizes => new ObservableCollection<string>(videoSizes);

        public ObservableCollection<string> BlogTypes => new ObservableCollection<string>(blogTypes);

        public ObservableCollection<string> LogLevels => new ObservableCollection<string>(logLevels);

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

        public string[] TumblrHosts
        {
            get => tumblrHosts;
            set => tumblrHosts = value;
        }

        public static bool Upgrade(AppSettings settings)
        {
            var updated = false;
            var newStgs = new AppSettings();

            if (settings.UserAgent != new AppSettings().UserAgent &&
                System.Text.RegularExpressions.Regex.IsMatch(settings.UserAgent,
                @"Mozilla\/[\d]+\.[\d]+ \(Windows [ \.\w\d]*; Win(64|32); (x64|x86)\) AppleWebKit\/[\d]+\.[\d]+ \(KHTML, like Gecko\) Chrome\/[\d]+\.[\d]+\.[\d]+\.[\d]+ Safari\/[\d]+\.[\d]+"))
            {
                settings.UserAgent = newStgs.UserAgent;
                updated = true;
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

            return updated;
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
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36";
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
            MaxNumberOfRetries = 3;
            ProgressUpdateInterval = 100;
            Bandwidth = 0;
            BufferSize = 512;
            ImageSize = "best";
            VideoSize = 1080;
            BlogType = "None";
            CheckClipboard = true;
            ShowPicturePreview = true;
            DisplayConfirmationDialog = false;
            DeleteOnlyIndex = true;
            ArchiveIndex = false;
            CheckOnlineStatusOnStartup = false;
            SkipGif = false;
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
            Language = "en-US";
            ColumnSettings = new Dictionary<object, Tuple<int, double, Visibility>>();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
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
