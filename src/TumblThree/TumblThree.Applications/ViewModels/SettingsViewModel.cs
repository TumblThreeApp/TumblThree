using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Windows;
using System.Windows.Input;

using TumblThree.Applications.Data;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using TumblThree.Domain.Models;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class SettingsViewModel : ViewModel<ISettingsView>
    {
        private readonly AsyncDelegateCommand _authenticateCommand;
        private readonly AsyncDelegateCommand _privacyConsentCommand;
        private readonly DelegateCommand _browseDownloadLocationCommand;
        private readonly DelegateCommand _browseCollectionDownloadLocationCommand;
        private readonly DelegateCommand _newCollectionCommand;
        private readonly DelegateCommand _deleteCollectionCommand;
        private readonly DelegateCommand _browseExportLocationCommand;
        private readonly DelegateCommand _enableAutoDownloadCommand;
        private readonly DelegateCommand _exportCommand;
        private readonly AsyncDelegateCommand _saveCommand;
        private readonly AsyncDelegateCommand _tumblrLoginCommand;
        private readonly AsyncDelegateCommand _tumblrLogoutCommand;
        private readonly AsyncDelegateCommand _tumblrSubmitTfaCommand;
        private readonly AsyncDelegateCommand _twitterAuthenticateCommand;
        private readonly AsyncDelegateCommand _twitterPrivacyConsentCommand;
        private readonly AsyncDelegateCommand _twitterLogoutCommand;

        private readonly IFolderBrowserDialog _folderBrowserDialog;
        private readonly IFileDialogService _fileDialogService;
        private readonly ExportFactory<AuthenticateViewModel> _authenticateViewModelFactory;
        private readonly FileType _bloglistExportFileType;
        private readonly AppSettings _settings;
        private readonly IDetailsService _detailsService;
        private readonly IMessageService _messageService;

        private string _apiKey;
        private bool _autoDownload;
        private long _bandwidth;
        private double _progressUpdateInterval;
        private string _blogType;
        private bool _checkClipboard;
        private bool _checkDirectoryForFiles;
        private bool _checkOnlineStatusOnStartup;
        private int _connectionTimeIntervalApi;
        private int _connectionTimeIntervalSvc;
        private bool _createAudioMeta;
        private bool _createImageMeta;
        private bool _createVideoMeta;
        private bool _dumpCrawlerData;
        private bool _regExPhotos;
        private bool _regExVideos;
        private string _downloadPages;
        private int _pageSize;
        private string _downloadFrom;
        private string _downloadTo;
        private string _tags;
        private bool _downloadRebloggedPosts;
        private bool _deleteOnlyIndex;
        private bool _archiveIndex;
        private bool _downloadAudios;
        private bool _downloadConversations;
        private bool _downloadImages;
        private bool _downloadLinks;
        private string _downloadLocation;
        private string _exportLocation;
        private bool _downloadQuotes;
        private bool _downloadTexts;
        private bool _downloadAnswers;
        private bool _downloadUrlList;
        private bool _downloadVideos;
        private bool _enablePreview;
        private bool _forceSize;
        private bool _forceRescan;
        private string _imageSize;
        private string _imageSizeCategory;
        private bool _limitConnectionsApi;
        private bool _limitConnectionsSvc;
        private bool _limitScanBandwidth;
        private int _maxConnectionsApi;
        private int _maxConnectionsSvc;
        private string _oauthCallbackUrl;
        private string _oauthToken;
        private string _oauthTokenSecret;
        private int _concurrentBlogs;
        private int _concurrentConnections;
        private int _concurrentVideoConnections;
        private int _concurrentScans;
        private bool _portableMode;
        private bool _loadAllDatabases;
        private bool _loadArchive;
        private string _proxyHost;
        private string _proxyPort;
        private string _proxyUsername;
        private string _proxyPassword;
        private bool _downloadGfycat;
        private bool _downloadImgur;
        private bool _downloadWebmshare;
        private bool _downloadUguu;
        private bool _downloadCatBox;
        private MetadataType _metadataFormat;
        private bool _overrideTumblrBlogCrawler;
        private TumblrBlogCrawlerTypes _tumblrBlogCrawlerType;
        private GfycatTypes _gfycatType;
        private WebmshareTypes _webmshareType;
        private UguuTypes _uguuType;
        private CatBoxTypes _catBoxType;
        private bool _removeIndexAfterCrawl;
        private string _secretKey;
        private bool _showPicturePreview;
        private bool _displayConfirmationDialog;
        private bool _skipGif;
        private bool _downloadVideoThumbnails;
        private int _timeOut;
        private string _timerInterval;
        private int _videoSize;
        private string _videoSizeCategory;
        private int _settingsTabIndex;
        private string _userAgent;
        private string _tumblrUser = string.Empty;
        private string _tumblrPassword = string.Empty;
        private bool _tumblrLoggedIn;
        private bool _tumblrTfaDetected;
        private string _tumblrTfaAuthCode = string.Empty;
        private string _tumblrEmail = string.Empty;
        private string _logLevel = string.Empty;
        private bool _groupPhotoSets;
        private string _filenameTemplate;
        private string _language;
        private ObservableCollection<Collection> _collections;
        private int _activeCollectionId;
        private int _selectedCollectionId;
        private string _pnjDownloadFormat;
        private bool _twitterLoggedIn;
        private string _twitterEmail = string.Empty;
        private bool _tumblrAuthErrorAutoRetry;

        [ImportingConstructor]
        public SettingsViewModel(ISettingsView view, IShellService shellService, ICrawlerService crawlerService, IManagerService managerService,
            ILoginService loginService, IFolderBrowserDialog folderBrowserDialog, IFileDialogService fileDialogService,
            ExportFactory<AuthenticateViewModel> authenticateViewModelFactory, IDetailsService detailsService, IMessageService messageService)
            : base(view)
        {
            _folderBrowserDialog = folderBrowserDialog;
            _fileDialogService = fileDialogService;
            ShellService = shellService;
            _settings = ShellService.Settings;
            CrawlerService = crawlerService;
            ManagerService = managerService;
            LoginService = loginService;
            _detailsService = detailsService;
            _messageService = messageService;
            _authenticateViewModelFactory = authenticateViewModelFactory;
            _browseDownloadLocationCommand = new DelegateCommand(BrowseDownloadLocation);
            _browseCollectionDownloadLocationCommand = new DelegateCommand(BrowseCategoryDownloadLocation);
            _newCollectionCommand = new DelegateCommand(NewCollection);
            _deleteCollectionCommand = new DelegateCommand(DeleteCollection);
            _browseExportLocationCommand = new DelegateCommand(BrowseExportLocation);
            _authenticateCommand = new AsyncDelegateCommand(Authenticate);
            _privacyConsentCommand = new AsyncDelegateCommand(PrivacyConsent);
            _tumblrLoginCommand = new AsyncDelegateCommand(TumblrLogin);
            _tumblrLogoutCommand = new AsyncDelegateCommand(TumblrLogout);
            _tumblrSubmitTfaCommand = new AsyncDelegateCommand(TumblrSubmitTfa);
            _saveCommand = new AsyncDelegateCommand(Save);
            _enableAutoDownloadCommand = new DelegateCommand(EnableAutoDownload);
            _exportCommand = new DelegateCommand(ExportBlogs);
            _twitterAuthenticateCommand = new AsyncDelegateCommand(TwitterAuthenticate);
            _twitterPrivacyConsentCommand = new AsyncDelegateCommand(TwitterPrivacyConsent);
            _twitterLogoutCommand = new AsyncDelegateCommand(TwitterLogout);

            _bloglistExportFileType = new FileType(Resources.Textfile, SupportedFileTypes.BloglistExportFileType);

            Task loadSettingsTask = Load();
            view.Closed += ViewClosed;
        }

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

        public IManagerService ManagerService { get; }

        public ILoginService LoginService { get; }

        public ICommand BrowseDownloadLocationCommand => _browseDownloadLocationCommand;

        public ICommand BrowseCollectionDownloadLocationCommand => _browseCollectionDownloadLocationCommand;

        public ICommand NewCollectionCommand => _newCollectionCommand;

        public ICommand DeleteCollectionCommand => _deleteCollectionCommand;

        public ICommand AuthenticateCommand => _authenticateCommand;

        public ICommand PrivacyConsentCommand => _privacyConsentCommand;

        public ICommand TumblrLoginCommand => _tumblrLoginCommand;

        public ICommand TumblrLogoutCommand => _tumblrLogoutCommand;

        public ICommand TumblrSubmitTfaCommand => _tumblrSubmitTfaCommand;

        public ICommand SaveCommand => _saveCommand;

        public ICommand EnableAutoDownloadCommand => _enableAutoDownloadCommand;

        public ICommand ExportCommand => _exportCommand;

        public ICommand BrowseExportLocationCommand => _browseExportLocationCommand;

        public ICommand TwitterAuthenticateCommand => _twitterAuthenticateCommand;

        public ICommand TwitterPrivacyConsentCommand => _twitterPrivacyConsentCommand;

        public ICommand TwitterLogoutCommand => _twitterLogoutCommand;

        public string OAuthToken
        {
            get => _oauthToken;
            set => SetProperty(ref _oauthToken, value);
        }

        public string OAuthTokenSecret
        {
            get => _oauthTokenSecret;
            set => SetProperty(ref _oauthTokenSecret, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        public string SecretKey
        {
            get => _secretKey;
            set => SetProperty(ref _secretKey, value);
        }

        public string OAuthCallbackUrl
        {
            get => _oauthCallbackUrl;
            set => SetProperty(ref _oauthCallbackUrl, value);
        }

        public string DownloadLocation
        {
            get => _downloadLocation;
            set
            {
                SetProperty(ref _downloadLocation, value);
                AdaptDefaultCollection();
            }
        }

        public string ExportLocation
        {
            get => _exportLocation;
            set => SetProperty(ref _exportLocation, value);
        }

        public int ConcurrentConnections
        {
            get => _concurrentConnections;
            set => SetProperty(ref _concurrentConnections, value);
        }

        public int ConcurrentVideoConnections
        {
            get => _concurrentVideoConnections;
            set => SetProperty(ref _concurrentVideoConnections, value);
        }

        public int ConcurrentBlogs
        {
            get => _concurrentBlogs;
            set => SetProperty(ref _concurrentBlogs, value);
        }

        public int ConcurrentScans
        {
            get => _concurrentScans;
            set => SetProperty(ref _concurrentScans, value);
        }

        public int TimeOut
        {
            get => _timeOut;
            set => SetProperty(ref _timeOut, value);
        }

        public bool LimitConnectionsApi
        {
            get => _limitConnectionsApi;
            set => SetProperty(ref _limitConnectionsApi, value);
        }

        public bool LimitConnectionsSvc
        {
            get => _limitConnectionsSvc;
            set => SetProperty(ref _limitConnectionsSvc, value);
        }

        public int MaxConnectionsApi
        {
            get => _maxConnectionsApi;
            set => SetProperty(ref _maxConnectionsApi, value);
        }

        public int MaxConnectionsSvc
        {
            get => _maxConnectionsSvc;
            set => SetProperty(ref _maxConnectionsSvc, value);
        }

        public int ConnectionTimeIntervalApi
        {
            get => _connectionTimeIntervalApi;
            set => SetProperty(ref _connectionTimeIntervalApi, value);
        }

        public int ConnectionTimeIntervalSvc
        {
            get => _connectionTimeIntervalSvc;
            set => SetProperty(ref _connectionTimeIntervalSvc, value);
        }

        public long Bandwidth
        {
            get => _bandwidth;
            set => SetProperty(ref _bandwidth, value);
        }

        public double ProgressUpdateInterval
        {
            get => _progressUpdateInterval;
            set => SetProperty(ref _progressUpdateInterval, value);
        }

        public bool LimitScanBandwidth
        {
            get => _limitScanBandwidth;
            set => SetProperty(ref _limitScanBandwidth, value);
        }

        public string ImageSize
        {
            get => _imageSize;
            set => SetProperty(ref _imageSize, value);
        }

        public int VideoSize
        {
            get => _videoSize;
            set => SetProperty(ref _videoSize, value);
        }

        public string ImageSizeCategory
        {
            get => _imageSizeCategory;
            set => SetProperty(ref _imageSizeCategory, value);
        }

        public string VideoSizeCategory
        {
            get => _videoSizeCategory;
            set => SetProperty(ref _videoSizeCategory, value);
        }

        public string BlogType
        {
            get => _blogType;
            set => SetProperty(ref _blogType, value);
        }

        public bool CheckClipboard
        {
            get => _checkClipboard;
            set => SetProperty(ref _checkClipboard, value);
        }

        public bool DisplayConfirmationDialog
        {
            get => _displayConfirmationDialog;
            set => SetProperty(ref _displayConfirmationDialog, value);
        }

        public bool ShowPicturePreview
        {
            get => _showPicturePreview;
            set => SetProperty(ref _showPicturePreview, value);
        }

        public bool DeleteOnlyIndex
        {
            get => _deleteOnlyIndex;
            set
            {
                if (SetProperty(ref _deleteOnlyIndex, value))
                {
                    if (!_deleteOnlyIndex) ArchiveIndex = false;
                    RaisePropertyChanged(nameof(ArchiveIndexEnabled));
                }
            }
        }

        public bool ArchiveIndex
        {
            get => _archiveIndex;
            set => SetProperty(ref _archiveIndex, value);
        }

        public bool ArchiveIndexEnabled
        {
            get => _deleteOnlyIndex;
        }

        public bool CheckOnlineStatusOnStartup
        {
            get => _checkOnlineStatusOnStartup;
            set => SetProperty(ref _checkOnlineStatusOnStartup, value);
        }

        public bool SkipGif
        {
            get => _skipGif;
            set => SetProperty(ref _skipGif, value);
        }

        public bool DownloadVideoThumbnails
        {
            get => _downloadVideoThumbnails;
            set => SetProperty(ref _downloadVideoThumbnails, value);
        }

        public bool EnablePreview
        {
            get => _enablePreview;
            set => SetProperty(ref _enablePreview, value);
        }

        public bool AutoDownload
        {
            get => _autoDownload;
            set => SetProperty(ref _autoDownload, value);
        }

        public bool RemoveIndexAfterCrawl
        {
            get => _removeIndexAfterCrawl;
            set => SetProperty(ref _removeIndexAfterCrawl, value);
        }

        public bool ForceSize
        {
            get => _forceSize;
            set => SetProperty(ref _forceSize, value);
        }

        public bool ForceRescan
        {
            get => _forceRescan;
            set => SetProperty(ref _forceRescan, value);
        }

        public bool CheckDirectoryForFiles
        {
            get => _checkDirectoryForFiles;
            set => SetProperty(ref _checkDirectoryForFiles, value);
        }

        public bool DownloadUrlList
        {
            get => _downloadUrlList;
            set => SetProperty(ref _downloadUrlList, value);
        }

        public bool PortableMode
        {
            get => _portableMode;
            set => SetProperty(ref _portableMode, value);
        }

        public ObservableCollection<Collection> Collections
        {
            get => _collections;
            set => SetProperty(ref _collections, value);
        }

        public int ActiveCollectionId
        {
            get => _activeCollectionId;
            set => SetProperty(ref _activeCollectionId, value);
        }

        public int SelectedCollectionId
        {
            get => _selectedCollectionId;
            set
            {
                SetProperty(ref _selectedCollectionId, value);
                RaisePropertyChanged(nameof(BrowseDownloadLocationButton2Enabled));
                RaisePropertyChanged(nameof(DeleteCollectionEnabled));
                RaisePropertyChanged(nameof(CollectionNameEnabled));
                RaisePropertyChanged(nameof(DownloadLocationTextBox2Enabled));
            }
        }

        public string PnjDownloadFormat
        {
            get => _pnjDownloadFormat;
            set => SetProperty(ref _pnjDownloadFormat, value);
        }

        public bool TumblrAuthErrorAutoRetry
        {
            get => _tumblrAuthErrorAutoRetry;
            set => SetProperty(ref _tumblrAuthErrorAutoRetry, value);
        }

        private static string Sanitize(string filename)
        {
            char[] invalids = Path.GetInvalidFileNameChars();
            return string.Join("-", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }

        public bool CollectionNameValidate(string enteredCollectionName)
        {
            bool valid = !string.IsNullOrWhiteSpace(enteredCollectionName);
            if (!valid)
            {
                _messageService.ShowWarning(Resources.CollectionNameIsEmpty);
            }
            if (valid && enteredCollectionName != Sanitize(enteredCollectionName))
            {
                valid = false;
                _messageService.ShowWarning(Resources.CollectionNameInvalidChars);
            }
            return valid;
        }

        public bool DownloadLocationValidate(string enteredDownloadLocation)
        {
            bool valid = !string.IsNullOrWhiteSpace(enteredDownloadLocation);
            if (valid)
            {
                string msg = null;
                try
                {
                    Path.Combine(enteredDownloadLocation, "");
                }
                catch (ArgumentException e)
                {
                    valid = false;
                    msg = e.Message;
                }
                if (!valid)
                {
                    _messageService.ShowWarning(msg);
                }
            }
            return valid;
        }

        public bool LoadAllDatabases
        {
            get => _loadAllDatabases;
            set
            {
                if (SetProperty(ref _loadAllDatabases, value))
                {
                    if (!_loadAllDatabases) LoadArchive = false;
                    RaisePropertyChanged(nameof(LoadArchiveEnabled));
                }
            }
        }

        public bool LoadArchive
        {
            get => _loadArchive;
            set => SetProperty(ref _loadArchive, value);
        }

        public bool LoadArchiveEnabled
        {
            get => _loadAllDatabases;
        }

        public string ProxyHost
        {
            get => _proxyHost;
            set => SetProperty(ref _proxyHost, value);
        }

        public string ProxyPort
        {
            get => _proxyPort;
            set => SetProperty(ref _proxyPort, value);
        }

        public string ProxyUsername
        {
            get => _proxyUsername;
            set => SetProperty(ref _proxyUsername, value);
        }

        public string ProxyPassword
        {
            get => _proxyPassword;
            set => SetProperty(ref _proxyPassword, value);
        }

        public bool DownloadImages
        {
            get => _downloadImages;
            set => SetProperty(ref _downloadImages, value);
        }

        public bool DownloadVideos
        {
            get => _downloadVideos;
            set => SetProperty(ref _downloadVideos, value);
        }

        public bool DownloadAudios
        {
            get => _downloadAudios;
            set => SetProperty(ref _downloadAudios, value);
        }

        public bool DownloadTexts
        {
            get => _downloadTexts;
            set => SetProperty(ref _downloadTexts, value);
        }

        public bool DownloadAnswers
        {
            get => _downloadAnswers;
            set => SetProperty(ref _downloadAnswers, value);
        }

        public bool DownloadQuotes
        {
            get => _downloadQuotes;
            set => SetProperty(ref _downloadQuotes, value);
        }

        public bool DownloadConversations
        {
            get => _downloadConversations;
            set => SetProperty(ref _downloadConversations, value);
        }

        public bool DownloadLinks
        {
            get => _downloadLinks;
            set => SetProperty(ref _downloadLinks, value);
        }

        public bool CreateImageMeta
        {
            get => _createImageMeta;
            set => SetProperty(ref _createImageMeta, value);
        }

        public bool CreateVideoMeta
        {
            get => _createVideoMeta;
            set => SetProperty(ref _createVideoMeta, value);
        }

        public bool CreateAudioMeta
        {
            get => _createAudioMeta;
            set => SetProperty(ref _createAudioMeta, value);
        }

        public MetadataType MetadataFormat
        {
            get => _metadataFormat;
            set => SetProperty(ref _metadataFormat, value);
        }

        public bool OverrideTumblrBlogCrawler
        {
            get => _overrideTumblrBlogCrawler;
            set => SetProperty(ref _overrideTumblrBlogCrawler, value);
        }

        public TumblrBlogCrawlerTypes TumblrBlogCrawlerType
        {
            get => _tumblrBlogCrawlerType;
            set => SetProperty(ref _tumblrBlogCrawlerType, value);
        }

        public bool DumpCrawlerData
        {
            get => _dumpCrawlerData;
            set => SetProperty(ref _dumpCrawlerData, value);
        }

        public bool RegExPhotos
        {
            get => _regExPhotos;
            set => SetProperty(ref _regExPhotos, value);
        }

        public bool RegExVideos
        {
            get => _regExVideos;
            set => SetProperty(ref _regExVideos, value);
        }

        public string DownloadPages
        {
            get => _downloadPages;
            set => SetProperty(ref _downloadPages, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set => SetProperty(ref _pageSize, value);
        }

        public string DownloadFrom
        {
            get => _downloadFrom;
            set => SetProperty(ref _downloadFrom, value);
        }

        public string DownloadTo
        {
            get => _downloadTo;
            set => SetProperty(ref _downloadTo, value);
        }

        public bool DownloadGfycat
        {
            get => _downloadGfycat;
            set => SetProperty(ref _downloadGfycat, value);
        }

        public GfycatTypes GfycatType
        {
            get => _gfycatType;
            set => SetProperty(ref _gfycatType, value);
        }

        public bool DownloadImgur
        {
            get => _downloadImgur;
            set => SetProperty(ref _downloadImgur, value);
        }

        public bool DownloadWebmshare
        {
            get => _downloadWebmshare;
            set => SetProperty(ref _downloadWebmshare, value);
        }

        public WebmshareTypes WebmshareType
        {
            get => _webmshareType;
            set => SetProperty(ref _webmshareType, value);
        }

        public bool DownloadUguu
        {
            get => _downloadUguu;
            set => SetProperty(ref _downloadUguu, value);
        }

        public UguuTypes UguuType
        {
            get => _uguuType;
            set => SetProperty(ref _uguuType, value);
        }

        public bool DownloadCatBox
        {
            get => _downloadCatBox;
            set => SetProperty(ref _downloadCatBox, value);
        }

        public CatBoxTypes CatBoxType
        {
            get => _catBoxType;
            set => SetProperty(ref _catBoxType, value);
        }

        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        public bool DownloadRebloggedPosts
        {
            get => _downloadRebloggedPosts;
            set => SetProperty(ref _downloadRebloggedPosts, value);
        }

        public string TimerInterval
        {
            get => _timerInterval;
            set => SetProperty(ref _timerInterval, value);
        }

        public int SettingsTabIndex
        {
            get => _settingsTabIndex;
            set => SetProperty(ref _settingsTabIndex, value);
        }

        public string UserAgent
        {
            get => _userAgent;
            set => SetProperty(ref _userAgent, value);
        }

        public string TumblrUser
        {
            get => _tumblrUser;
            set => SetProperty(ref _tumblrUser, value);
        }

        public string TumblrPassword
        {
            get => _tumblrPassword;
            set => SetProperty(ref _tumblrPassword, value);
        }

        public bool TumblrLoggedIn
        {
            get => _tumblrLoggedIn;
            set => SetProperty(ref _tumblrLoggedIn, value);
        }

        public bool TumblrTfaDetected
        {
            get => _tumblrTfaDetected;
            set => SetProperty(ref _tumblrTfaDetected, value);
        }

        public string TumblrTfaAuthCode
        {
            get => _tumblrTfaAuthCode;
            set => SetProperty(ref _tumblrTfaAuthCode, value);
        }

        public string TumblrEmail
        {
            get => _tumblrEmail;
            set => SetProperty(ref _tumblrEmail, value);
        }

        public string LogLevel
        {
            get => _logLevel;
            set => SetProperty(ref _logLevel, value);
        }

        public bool GroupPhotoSets
        {
            get => _groupPhotoSets;
            set => SetProperty(ref _groupPhotoSets, value);
        }

        public string FilenameTemplate
        {
            get => _filenameTemplate;
            set
            {
                if (string.IsNullOrEmpty(value)) value = "%f";
                SetProperty(ref _filenameTemplate, value);
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                if (string.IsNullOrEmpty(value)) value = "en-US";
                SetProperty(ref _language, value);
            }
        }

        public bool BrowseDownloadLocationButton2Enabled
        {
            get
            {
                return SelectedCollectionId > 0;
            }
        }

        public bool DeleteCollectionEnabled
        {
            get
            {
                return SelectedCollectionId > 0;
            }
        }

        public bool CollectionNameEnabled
        {
            get
            {
                return SelectedCollectionId > 0;
            }
        }

        public bool DownloadLocationTextBox2Enabled
        {
            get
            {
                return SelectedCollectionId > 0;
            }
        }

        public bool TwitterLoggedIn
        {
            get => _twitterLoggedIn;
            set => SetProperty(ref _twitterLoggedIn, value);
        }

        public string TwitterEmail
        {
            get => _twitterEmail;
            set => SetProperty(ref _twitterEmail, value);
        }

        public void ShowDialog(object owner) => ViewCore.ShowDialog(owner);

        private void ViewClosed(object sender, EventArgs e)
        {
            if (_enableAutoDownloadCommand.CanExecute(null))
            {
                _enableAutoDownloadCommand.Execute(null);
            }
        }

        public bool FilenameTemplateValidate(string enteredFilenameTemplate)
        {
            return _detailsService.FilenameTemplateValidate(enteredFilenameTemplate);
        }

        private void EnableAutoDownload()
        {
            if (AutoDownload)
            {
                if (!CrawlerService.IsTimerSet)
                {
                    TimeSpan.TryParse(TimerInterval, out var alertTime);
                    DateTime current = DateTime.Now;
                    TimeSpan timeToGo = alertTime - current.TimeOfDay;
                    if (timeToGo < TimeSpan.Zero)
                    {
                        // time already passed
                        timeToGo = timeToGo.Add(new TimeSpan(24, 00, 00));
                    }

                    CrawlerService.Timer = new Timer(x => { OnTimedEvent(); }, null, timeToGo, Timeout.InfiniteTimeSpan);

                    CrawlerService.IsTimerSet = true;
                }
            }
            else
            {
                if (CrawlerService.Timer != null)
                {
                    CrawlerService.Timer.Dispose();
                    CrawlerService.IsTimerSet = false;
                }
            }
        }

        private void ExportBlogs()
        {
            List<string> blogList = ManagerService.BlogFiles.Select(blog => blog.Url).ToList();
            blogList.Sort();
            File.WriteAllLines(ExportLocation, blogList);
        }

        private void OnTimedEvent()
        {
            if (CrawlerService.AutoDownloadCommand.CanExecute(null))
            {
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => CrawlerService.AutoDownloadCommand.Execute(null));
            }

            CrawlerService.Timer.Change(new TimeSpan(24, 00, 00), Timeout.InfiniteTimeSpan);
        }

        private void BrowseDownloadLocation()
        {
            _folderBrowserDialog.SelectedPath = DownloadLocation;
            _folderBrowserDialog.ShowNewFolderButton = true;
            if (_folderBrowserDialog.ShowDialog() == true)
            {
                DownloadLocation = _folderBrowserDialog.SelectedPath;
            }
        }

        private void BrowseCategoryDownloadLocation(object id)
        {
            var collection = Collections.Where(x => x.Id == (int)id).First();
            _folderBrowserDialog.SelectedPath = collection.DownloadLocation;
            _folderBrowserDialog.ShowNewFolderButton = true;
            if (_folderBrowserDialog.ShowDialog() == true)
            {
                collection.DownloadLocation = _folderBrowserDialog.SelectedPath;
            }
        }

        private void BrowseExportLocation()
        {
            FileDialogResult result =
                _fileDialogService.ShowSaveFileDialog(ShellService.ShellView, _bloglistExportFileType, ExportLocation);
            if (!result.IsValid)
            {
                return;
            }

            ExportLocation = result.FileName;
        }

        private void AdaptDefaultCollection()
        {
            var item = Collections.Where(x => x.Id == 0).FirstOrDefault();
            item.DownloadLocation = DownloadLocation;
        }

        private void NewCollection()
        {
            int id = Collections.Max(x => x.Id) + 1;
            var name = $"{Resources.Collection} {id}";
            Collections.Add(new Collection() { Id = id, Name = name });
            SelectedCollectionId = id;
        }

        private void DeleteCollection(object id)
        {
            if (ManagerService.IsCollectionIdUsed((int)id))
            {
                _messageService.ShowWarning(Resources.CannotDeleteUsedCollection);
            }
            else
            {
                var index = Collections.IndexOf(Collections.Where(x => x.Id == (int)id).FirstOrDefault());
                Collections.RemoveAt(index);
                SelectedCollectionId = Collections[--index].Id;
            }
        }

        private async Task Authenticate()
        {
            if (!ShellService.CheckForWebView2Runtime()) { return; }

            const string url = @"https://www.tumblr.com/login";
            ShellService.Settings.OAuthCallbackUrl = "https://www.tumblr.com/dashboard_";

            AuthenticateViewModel authenticateViewModel = _authenticateViewModelFactory.CreateExport().Value;
            authenticateViewModel.ShowDialog(ShellService.ShellView, url, "www.tumblr.com");

            var cookies = await authenticateViewModel.GetCookies("https://www.tumblr.com/");

            LoginService.AddCookies(cookies);
            await UpdateLoginAsync(Provider.Tumblr);
        }

        private async Task PrivacyConsent()
        {
            if (!ShellService.CheckForWebView2Runtime()) { return; }

            const string url = @"https://www.tumblr.com/search/cat";

            AuthenticateViewModel authenticateViewModel = _authenticateViewModelFactory.CreateExport().Value;
            authenticateViewModel.ShowDialog(ShellService.ShellView, url, "www.tumblr.com");

            var cookies = await authenticateViewModel.GetCookies("https://www.tumblr.com/");

            LoginService.AddCookies(cookies);
        }

        private async Task TumblrLogin()
        {
            try
            {
                await LoginService.PerformTumblrLoginAsync(TumblrUser, TumblrPassword);
            }
            catch (Exception e)
            {
                Logger.Error("SettingsViewModel.TumblrLogin: {0}", e);
            }

            TumblrTfaDetected = LoginService.CheckIfTumblrTFANeeded();
            if (!TumblrTfaDetected)
            {
                await UpdateLoginAsync(Provider.Tumblr);
            }
        }

        private async Task TumblrLogout()
        {
            try
            {
                await LoginService.PerformLogoutAsync(Provider.Tumblr);
                await UpdateLoginAsync(Provider.Tumblr);
            }
            catch (Exception e)
            {
                Logger.Error("SettingsViewModel.TumblrLogout: {0}", e);
            }
        }

        private async Task TumblrSubmitTfa()
        {
            try
            {
                await LoginService.PerformTumblrTFALoginAsync(TumblrUser, TumblrTfaAuthCode);
                await UpdateLoginAsync(Provider.Tumblr);
            }
            catch (Exception e)
            {
                Logger.Error("SettingsViewModel.TumblrSubmitTfa: {0}", e);
            }
        }

        private async Task UpdateLoginAsync(Provider provider, string document = null)
        {
            try
            {
                switch (provider)
                {
                    case Provider.Tumblr:
                        TumblrEmail = await LoginService.GetUsernameAsync(provider);
                        TumblrLoggedIn = !string.IsNullOrEmpty(TumblrEmail);
                        break;
                    case Provider.Twitter:
                        TwitterEmail = await LoginService.GetUsernameAsync(provider, document);
                        TwitterLoggedIn = !string.IsNullOrEmpty(TwitterEmail);
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("SettingsViewModel.UpdateLoginAsync: {0}", e);
            }
        }

        private async Task CheckIfTumblrLoggedIn()
        {
            TumblrLoggedIn = await LoginService.CheckIfLoggedInAsync();
        }

        private async Task TwitterAuthenticate()
        {
            if (!ShellService.CheckForWebView2Runtime()) { return; }

            const string url = @"https://twitter.com/settings";
            ShellService.Settings.OAuthCallbackUrl = "https://twitter.com";

            AuthenticateViewModel authenticateViewModel = _authenticateViewModelFactory.CreateExport().Value;
            authenticateViewModel.ShowDialog(ShellService.ShellView, url, "twitter.com");

            var cookies = await authenticateViewModel.GetCookies("https://twitter.com/");
            var document = await authenticateViewModel.GetDocument();
            
            LoginService.AddCookies(cookies);
            await UpdateLoginAsync(Provider.Twitter, document);
        }

        private async Task TwitterPrivacyConsent()
        {
            await Task.CompletedTask;
        }

        private async Task TwitterLogout()
        {
            try
            {
                await LoginService.PerformLogoutAsync(Provider.Twitter);
                await UpdateLoginAsync(Provider.Twitter);
            }
            catch (Exception e)
            {
                Logger.Error("SettingsViewModel.TwitterLogout: {0}", e);
            }
        }

        public async Task Load()
        {
            LoadSettings();
            await UpdateLoginAsync(Provider.Tumblr);
            await UpdateLoginAsync(Provider.Twitter);
        }

        private void LoadSettings()
        {
            if (_settings != null)
            {
                ApiKey = _settings.ApiKey;
                SecretKey = _settings.SecretKey;
                OAuthToken = _settings.OAuthToken;
                OAuthTokenSecret = _settings.OAuthTokenSecret;
                OAuthCallbackUrl = _settings.OAuthCallbackUrl;
                Collections = new ObservableCollection<Collection>(_settings.Collections.ConvertAll(x => x.Clone()));
                ActiveCollectionId = _settings.ActiveCollectionId;
                DownloadLocation = _settings.DownloadLocation;
                ExportLocation = _settings.ExportLocation;
                ConcurrentConnections = _settings.ConcurrentConnections;
                ConcurrentVideoConnections = _settings.ConcurrentVideoConnections;
                ConcurrentBlogs = _settings.ConcurrentBlogs;
                ConcurrentScans = _settings.ConcurrentScans;
                LimitScanBandwidth = _settings.LimitScanBandwidth;
                ImageSize = _settings.ImageSize;
                VideoSize = _settings.VideoSize;
                ImageSizeCategory = _settings.ImageSizeCategory;
                VideoSizeCategory = _settings.VideoSizeCategory;
                BlogType = _settings.BlogType;
                TimeOut = _settings.TimeOut;
                LimitConnectionsApi = _settings.LimitConnectionsApi;
                LimitConnectionsSvc = _settings.LimitConnectionsSvc;
                MaxConnectionsApi = _settings.MaxConnectionsApi;
                MaxConnectionsSvc = _settings.MaxConnectionsSvc;
                ConnectionTimeIntervalApi = _settings.ConnectionTimeIntervalApi;
                ConnectionTimeIntervalSvc = _settings.ConnectionTimeIntervalSvc;
                Bandwidth = _settings.Bandwidth;
                ProgressUpdateInterval = _settings.ProgressUpdateInterval;
                CheckClipboard = _settings.CheckClipboard;
                ShowPicturePreview = _settings.ShowPicturePreview;
                DisplayConfirmationDialog = _settings.DisplayConfirmationDialog;
                DeleteOnlyIndex = _settings.DeleteOnlyIndex;
                ArchiveIndex = _settings.ArchiveIndex;
                CheckOnlineStatusOnStartup = _settings.CheckOnlineStatusOnStartup;
                SkipGif = _settings.SkipGif;
                DownloadVideoThumbnails = _settings.DownloadVideoThumbnails;
                EnablePreview = _settings.EnablePreview;
                RemoveIndexAfterCrawl = _settings.RemoveIndexAfterCrawl;
                DownloadImages = _settings.DownloadImages;
                DownloadVideos = _settings.DownloadVideos;
                DownloadTexts = _settings.DownloadTexts;
                DownloadAnswers = _settings.DownloadAnswers;
                DownloadAudios = _settings.DownloadAudios;
                DownloadConversations = _settings.DownloadConversations;
                DownloadLinks = _settings.DownloadLinks;
                DownloadQuotes = _settings.DownloadQuotes;
                CreateImageMeta = _settings.CreateImageMeta;
                CreateVideoMeta = _settings.CreateVideoMeta;
                CreateAudioMeta = _settings.CreateAudioMeta;
                MetadataFormat = _settings.MetadataFormat;
                DumpCrawlerData = _settings.DumpCrawlerData;
                RegExPhotos = _settings.RegExPhotos;
                RegExVideos = _settings.RegExVideos;
                DownloadPages = _settings.DownloadPages;
                PageSize = _settings.PageSize;
                DownloadFrom = _settings.DownloadFrom;
                DownloadTo = _settings.DownloadTo;
                Tags = _settings.Tags;
                DownloadImgur = _settings.DownloadImgur;
                DownloadGfycat = _settings.DownloadGfycat;
                DownloadWebmshare = _settings.DownloadWebmshare;
                DownloadUguu = _settings.DownloadUguu;
                DownloadCatBox = _settings.DownloadCatBox;
                GfycatType = _settings.GfycatType;
                WebmshareType = _settings.WebmshareType;
                OverrideTumblrBlogCrawler = _settings.OverrideTumblrBlogCrawler;
                TumblrBlogCrawlerType = _settings.TumblrBlogCrawlerType;
                UguuType = _settings.UguuType;
                CatBoxType = _settings.CatBoxType;
                DownloadRebloggedPosts = _settings.DownloadRebloggedPosts;
                AutoDownload = _settings.AutoDownload;
                ForceSize = _settings.ForceSize;
                ForceRescan = _settings.ForceRescan;
                CheckDirectoryForFiles = _settings.CheckDirectoryForFiles;
                DownloadUrlList = _settings.DownloadUrlList;
                PortableMode = _settings.PortableMode;
                LoadAllDatabases = _settings.LoadAllDatabases;
                LoadArchive = _settings.LoadArchive;
                ProxyHost = _settings.ProxyHost;
                ProxyPort = _settings.ProxyPort;
                ProxyUsername = _settings.ProxyUsername;
                ProxyPassword = _settings.ProxyPassword;
                TimerInterval = _settings.TimerInterval;
                SettingsTabIndex = _settings.SettingsTabIndex;
                UserAgent = _settings.UserAgent;
                LogLevel = _settings.LogLevel;
                GroupPhotoSets = _settings.GroupPhotoSets;
                FilenameTemplate = _settings.FilenameTemplate;
                Language = _settings.Language;
                PnjDownloadFormat = _settings.PnjDownloadFormat;
                TumblrAuthErrorAutoRetry = _settings.TumblrAuthErrorAutoRetry;
            }
            else
            {
                ApiKey = "x8pd1InspmnuLSFKT4jNxe8kQUkbRXPNkAffntAFSk01UjRsLV";
                SecretKey = "Mul4BviRQgPLuhN1xzEqmXzwvoWicEoc4w6ftWBGWtioEvexmM";
                OAuthCallbackUrl = @"https://github.com/TumblThreeApp/TumblThree";
                OAuthToken = string.Empty;
                OAuthTokenSecret = string.Empty;
                var collectionList = new List<Collection>();
                collectionList.Add(new Collection() { Id = 0, Name = Resources.DefaultCollectionName });
                Collections = new ObservableCollection<Collection>(collectionList);
                ActiveCollectionId = 0;
                DownloadLocation = "Blogs";
                ExportLocation = "blogs.txt";
                ConcurrentConnections = 8;
                ConcurrentVideoConnections = 4;
                ConcurrentBlogs = 1;
                ConcurrentScans = 4;
                LimitScanBandwidth = false;
                TimeOut = 60;
                LimitConnectionsApi = true;
                LimitConnectionsSvc = true;
                MaxConnectionsApi = 90;
                MaxConnectionsSvc = 90;
                ConnectionTimeIntervalApi = 60;
                ConnectionTimeIntervalSvc = 60;
                ProgressUpdateInterval = 100;
                Bandwidth = 0;
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
                DownloadVideoThumbnails = true;
                EnablePreview = true;
                RemoveIndexAfterCrawl = false;
                DownloadImages = true;
                DownloadVideos = true;
                DownloadAudios = true;
                DownloadTexts = true;
                DownloadAnswers = true;
                DownloadConversations = true;
                DownloadQuotes = true;
                DownloadLinks = true;
                CreateImageMeta = false;
                CreateVideoMeta = false;
                CreateAudioMeta = false;
                MetadataFormat = MetadataType.Text;
                OverrideTumblrBlogCrawler = false;
                TumblrBlogCrawlerType = TumblrBlogCrawlerTypes.TumblrSVC;
                DumpCrawlerData = false;
                RegExPhotos = false;
                RegExVideos = false;
                DownloadPages = string.Empty;
                PageSize = 50;
                DownloadFrom = string.Empty;
                DownloadTo = string.Empty;
                Tags = string.Empty;
                DownloadImgur = false;
                DownloadGfycat = false;
                DownloadWebmshare = false;
                DownloadUguu = false;
                DownloadCatBox = false;
                GfycatType = GfycatTypes.Mp4;
                WebmshareType = WebmshareTypes.Webm;
                UguuType = UguuTypes.Any;
                CatBoxType = CatBoxTypes.Any;
                DownloadRebloggedPosts = true;
                AutoDownload = false;
                ForceSize = false;
                ForceRescan = false;
                CheckDirectoryForFiles = false;
                DownloadUrlList = false;
                PortableMode = false;
                LoadAllDatabases = false;
                LoadArchive = false;
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                ProxyHost = string.Empty;
                ProxyPort = string.Empty;
                TimerInterval = "22:40:00";
                SettingsTabIndex = 0;
                UserAgent = AppSettings.USERAGENT;
                LogLevel = nameof(System.Diagnostics.TraceLevel.Verbose);
                GroupPhotoSets = false;
                FilenameTemplate = "%f";
                Language = "en-US";
                TumblrAuthErrorAutoRetry = false;
            }
        }

        private async Task Save()
        {
            using (ShellService.SetApplicationBusy())
            {
                bool downloadLocationChanged = DownloadLocationChanged();
                bool loadAllDatabasesChanged = LoadAllDatabasesChanged();
                bool logLevelChanged = LogLevelChanged();
                SaveSettings();
                await ApplySettings(downloadLocationChanged, loadAllDatabasesChanged, logLevelChanged);

                ShellService.SettingsUpdated();
            }
        }

        private async Task ApplySettings(bool downloadLocationChanged, bool loadAllDatabasesChanged, bool logLevelChanged)
        {
            if (logLevelChanged)
            {
                Logger.ChangeLogLevel((System.Diagnostics.TraceLevel)Enum.Parse(typeof(System.Diagnostics.TraceLevel), _settings.LogLevel));
            }
            CrawlerService.TimeconstraintApi.SetRate(MaxConnectionsApi / (double)ConnectionTimeIntervalApi);
            CrawlerService.TimeconstraintSvc.SetRate(MaxConnectionsSvc / (double)ConnectionTimeIntervalSvc);
            CrawlerService.UpdateCollectionsList(false);

            if (loadAllDatabasesChanged && downloadLocationChanged)
            {
                CrawlerService.LibraryLoaded = new TaskCompletionSource<bool>();
                CrawlerService.DatabasesLoaded = new TaskCompletionSource<bool>();
                CrawlerService.ArchiveLoaded = new TaskCompletionSource<bool>();
                if (CrawlerService.StopCommand.CanExecute(null))
                {
                    CrawlerService.StopCommand.Execute(null);
                }

                CrawlerService.LoadLibraryCommand.Execute(null);
                CrawlerService.LoadAllDatabasesCommand.Execute(null);
                CrawlerService.LoadArchiveCommand.Execute(null);
                await Task.WhenAll(CrawlerService.LibraryLoaded.Task, CrawlerService.DatabasesLoaded.Task);
                CrawlerService.CheckIfDatabasesCompleteCommand.Execute(null);
                await Task.WhenAll(CrawlerService.ArchiveLoaded.Task);
            }
            else if (downloadLocationChanged)
            {
                CrawlerService.LibraryLoaded = new TaskCompletionSource<bool>();
                CrawlerService.DatabasesLoaded = new TaskCompletionSource<bool>();
                CrawlerService.ArchiveLoaded = new TaskCompletionSource<bool>();
                if (CrawlerService.StopCommand.CanExecute(null))
                {
                    CrawlerService.StopCommand.Execute(null);
                }

                CrawlerService.LoadLibraryCommand.Execute(null);
                CrawlerService.LoadAllDatabasesCommand.Execute(null);
                CrawlerService.LoadArchiveCommand.Execute(null);
                await Task.WhenAll(CrawlerService.LibraryLoaded.Task, CrawlerService.DatabasesLoaded.Task);
                CrawlerService.CheckIfDatabasesCompleteCommand.Execute(null);
                await Task.WhenAll(CrawlerService.ArchiveLoaded.Task);
            }
            else if (loadAllDatabasesChanged)
            {
                CrawlerService.DatabasesLoaded = new TaskCompletionSource<bool>();
                CrawlerService.ArchiveLoaded = new TaskCompletionSource<bool>();
                if (CrawlerService.StopCommand.CanExecute(null))
                {
                    CrawlerService.StopCommand.Execute(null);
                }

                CrawlerService.LoadAllDatabasesCommand.Execute(null);
                CrawlerService.LoadArchiveCommand.Execute(null);
                await Task.WhenAll(CrawlerService.ArchiveLoaded.Task);
            }
        }

        private bool DownloadLocationChanged()
        {
            return !_settings.DownloadLocation.Equals(DownloadLocation);
        }

        private bool LoadAllDatabasesChanged()
        {
            return !_settings.LoadAllDatabases.Equals(LoadAllDatabases);
        }

        private bool LogLevelChanged()
        {
            return !_settings.LogLevel.Equals(LogLevel);
        }

        private void SaveSettings()
        {
            _settings.DownloadLocation = DownloadLocation;
            _settings.ExportLocation = ExportLocation;
            _settings.ConcurrentConnections = ConcurrentConnections;
            _settings.ConcurrentVideoConnections = ConcurrentVideoConnections;
            _settings.ConcurrentBlogs = ConcurrentBlogs;
            _settings.ConcurrentScans = ConcurrentScans;
            _settings.LimitScanBandwidth = LimitScanBandwidth;
            _settings.TimeOut = TimeOut;
            _settings.LimitConnectionsApi = LimitConnectionsApi;
            _settings.LimitConnectionsSvc = LimitConnectionsSvc;
            _settings.MaxConnectionsApi = MaxConnectionsApi;
            _settings.MaxConnectionsSvc = MaxConnectionsSvc;
            _settings.ConnectionTimeIntervalApi = ConnectionTimeIntervalApi;
            _settings.ConnectionTimeIntervalSvc = ConnectionTimeIntervalSvc;
            _settings.ProgressUpdateInterval = ProgressUpdateInterval;
            _settings.Bandwidth = Bandwidth;
            _settings.ImageSize = ImageSize;
            _settings.VideoSize = VideoSize;
            _settings.ImageSizeCategory = ImageSizeCategory;
            _settings.VideoSizeCategory = VideoSizeCategory;
            _settings.BlogType = BlogType;
            _settings.CheckClipboard = CheckClipboard;
            _settings.ShowPicturePreview = ShowPicturePreview;
            _settings.DisplayConfirmationDialog = DisplayConfirmationDialog;
            _settings.DeleteOnlyIndex = DeleteOnlyIndex;
            _settings.ArchiveIndex = ArchiveIndex;
            _settings.CheckOnlineStatusOnStartup = CheckOnlineStatusOnStartup;
            _settings.SkipGif = SkipGif;
            _settings.DownloadVideoThumbnails = DownloadVideoThumbnails;
            _settings.EnablePreview = EnablePreview;
            _settings.RemoveIndexAfterCrawl = RemoveIndexAfterCrawl;
            _settings.DownloadImages = DownloadImages;
            _settings.DownloadVideos = DownloadVideos;
            _settings.DownloadTexts = DownloadTexts;
            _settings.DownloadAnswers = DownloadAnswers;
            _settings.DownloadAudios = DownloadAudios;
            _settings.DownloadConversations = DownloadConversations;
            _settings.DownloadQuotes = DownloadQuotes;
            _settings.DownloadLinks = DownloadLinks;
            _settings.CreateImageMeta = CreateImageMeta;
            _settings.CreateVideoMeta = CreateVideoMeta;
            _settings.CreateAudioMeta = CreateAudioMeta;
            _settings.MetadataFormat = MetadataFormat;
            _settings.OverrideTumblrBlogCrawler = OverrideTumblrBlogCrawler;
            _settings.TumblrBlogCrawlerType = TumblrBlogCrawlerType;
            _settings.DumpCrawlerData = DumpCrawlerData;
            _settings.RegExPhotos = RegExPhotos;
            _settings.RegExVideos = RegExVideos;
            _settings.DownloadPages = DownloadPages;
            _settings.PageSize = PageSize;
            _settings.DownloadFrom = DownloadFrom;
            _settings.DownloadTo = DownloadTo;
            _settings.Tags = Tags;
            _settings.DownloadRebloggedPosts = DownloadRebloggedPosts;
            _settings.ApiKey = ApiKey;
            _settings.SecretKey = SecretKey;
            _settings.OAuthToken = OAuthToken;
            _settings.OAuthTokenSecret = OAuthTokenSecret;
            _settings.OAuthCallbackUrl = OAuthCallbackUrl;
            _settings.AutoDownload = AutoDownload;
            _settings.ForceSize = ForceSize;
            _settings.ForceRescan = ForceRescan;
            _settings.DownloadImgur = DownloadImgur;
            _settings.DownloadGfycat = DownloadGfycat;
            _settings.DownloadWebmshare = DownloadWebmshare;
            _settings.DownloadUguu = DownloadUguu;
            _settings.DownloadCatBox = DownloadCatBox;
            _settings.GfycatType = GfycatType;
            _settings.WebmshareType = WebmshareType;
            _settings.UguuType = UguuType;
            _settings.CatBoxType = CatBoxType;
            _settings.CheckDirectoryForFiles = CheckDirectoryForFiles;
            _settings.DownloadUrlList = DownloadUrlList;
            _settings.PortableMode = PortableMode;
            _settings.LoadAllDatabases = LoadAllDatabases;
            _settings.LoadArchive = LoadArchive;
            _settings.ProxyHost = ProxyHost;
            _settings.ProxyPort = ProxyPort;
            _settings.ProxyUsername = ProxyUsername;
            _settings.ProxyPassword = ProxyPassword;
            _settings.TimerInterval = TimerInterval;
            _settings.SettingsTabIndex = SettingsTabIndex;
            _settings.UserAgent = UserAgent;
            _settings.LogLevel = LogLevel;
            _settings.GroupPhotoSets = GroupPhotoSets;
            _settings.FilenameTemplate = FilenameTemplate;
            _settings.Language = Language;
            var list = new List<Collection>();
            foreach (var item in Collections)
            {
                if (item.Id == 0)
                {
                    item.DownloadLocation = DownloadLocation;
                }
                else if (!Path.IsPathRooted(item.DownloadLocation) || !Paths.IsPathFullyQualified(item.DownloadLocation))
                {
                    item.DownloadLocation = Path.Combine(DownloadLocation, item.Name);
                }
                item.IsOnline = Directory.Exists(item.DownloadLocation) || Directory.Exists(Directory.GetParent(item.DownloadLocation).FullName);
                list.Add(item);
            }
            _settings.Collections = list;
            _settings.ActiveCollectionId = ActiveCollectionId;
            _settings.PnjDownloadFormat = PnjDownloadFormat;
            _settings.TumblrAuthErrorAutoRetry = TumblrAuthErrorAutoRetry;
        }
    }
}
