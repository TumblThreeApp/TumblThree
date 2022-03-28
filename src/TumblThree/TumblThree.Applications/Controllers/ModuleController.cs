using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Threading;

using AutoUpdaterDotNET;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export(typeof(IModuleController))]
    [Export]
    internal class ModuleController : IModuleController
    {
        private const string AppSettingsFileName = "Settings.json";
        private const string ManagerSettingsFileName = "Manager.json";
        private const string QueueSettingsFileName = "Queuelist.json";
        private const string CookiesFileName = "Cookies.json";

        private readonly ISharedCookieService _cookieService;
        private readonly IEnvironmentService _environmentService;
        private readonly IApplicationUpdateService _applicationUpdateService;
        private readonly ILogService _logService;
        private readonly Lazy<ShellService> _shellService;

        private readonly Lazy<CrawlerController> _crawlerController;
        private readonly Lazy<DetailsController> _detailsController;
        private readonly Lazy<ManagerController> _managerController;
        private readonly Lazy<QueueController> _queueController;

        private readonly QueueManager _queueManager;
        private readonly ISettingsProvider _settingsProvider;
        private readonly IConfirmTumblrPrivacyConsent _confirmTumblrPrivacyConsent;

        private readonly Lazy<ShellViewModel> _shellViewModel;

        private AppSettings _appSettings;
        private ManagerSettings _managerSettings;
        private QueueSettings _queueSettings;
        private List<Cookie> _cookieList;

        [ImportingConstructor]
        public ModuleController(
            Lazy<ShellService> shellService,
            IEnvironmentService environmentService,
            IConfirmTumblrPrivacyConsent confirmTumblrPrivacyConsent,
            ISettingsProvider settingsProvider,
            ISharedCookieService cookieService,
            Lazy<ManagerController> managerController,
            Lazy<QueueController> queueController,
            Lazy<DetailsController> detailsController,
            Lazy<CrawlerController> crawlerController,
            Lazy<ShellViewModel> shellViewModel,
            IApplicationUpdateService applicationUpdateService,
            ILogService logService)
        {
            _shellService = shellService;
            _environmentService = environmentService;
            _confirmTumblrPrivacyConsent = confirmTumblrPrivacyConsent;
            _settingsProvider = settingsProvider;
            _cookieService = cookieService;
            _detailsController = detailsController;
            _managerController = managerController;
            _queueController = queueController;
            _crawlerController = crawlerController;
            _shellViewModel = shellViewModel;
            _queueManager = new QueueManager();
            _applicationUpdateService = applicationUpdateService;
            _logService = logService;
        }

        private ShellService ShellService => _shellService.Value;

        private ManagerController ManagerController => _managerController.Value;

        private QueueController QueueController => _queueController.Value;

        private DetailsController DetailsController => _detailsController.Value;

        private CrawlerController CrawlerController => _crawlerController.Value;

        private ShellViewModel ShellViewModel => _shellViewModel.Value;

        public void Initialize()
        {
            string savePath = _environmentService.AppSettingsPath;
            string logPath = Path.GetFullPath(Path.Combine(savePath, ".."));
            if (CheckIfPortableMode(AppSettingsFileName))
            {
                savePath = AppDomain.CurrentDomain.BaseDirectory;
                logPath = savePath;
            }

            Logger.Initialize(logPath, TraceLevel.Verbose);

            _appSettings = LoadSettings<AppSettings>(Path.Combine(savePath, AppSettingsFileName));
            InitializeCultures(_appSettings);
            if (AppSettings.Upgrade(_appSettings)) SaveSettings(Path.Combine(GetAppDataPath(), AppSettingsFileName), _appSettings);

            Logger.ChangeLogLevel((TraceLevel)Enum.Parse(typeof(TraceLevel), _appSettings.LogLevel));

            Logger.Information(ApplicationInfo.ProductName + " start");
            Logger.Information("AppPath: {0}", ApplicationInfo.ApplicationPath);
            Logger.Information("AppSettingsPath: {0}", _environmentService.AppSettingsPath);
            Logger.Information("LogFilename: {0}", Path.Combine(logPath, "TumblThree.log"));
            Logger.Information("Version: {0}", ApplicationInfo.Version);
            Logger.Information("IsLongPathSupported: {0}", ShellService.IsLongPathSupported);

            _queueSettings = LoadSettings<QueueSettings>(Path.Combine(savePath, QueueSettingsFileName));
            _managerSettings = LoadSettings<ManagerSettings>(Path.Combine(savePath, ManagerSettingsFileName));
            _cookieList = LoadSettings<List<Cookie>>(Path.Combine(savePath, CookiesFileName));

            ShellService.Settings = _appSettings;
            ShellService.ShowErrorAction = ShellViewModel.ShowError;
            ShellService.ShowDetailsViewAction = ShowDetailsView;
            ShellService.ShowQueueViewAction = ShowQueueView;
            ShellService.UpdateDetailsViewAction = UpdateDetailsView;
            ShellService.SettingsUpdatedHandler += OnSettingsUpdated;
            ShellService.InitializeOAuthManager();

            ManagerController.QueueManager = _queueManager;
            ManagerController.ManagerSettings = _managerSettings;
            ManagerController.BlogManagerFinishedLoadingLibrary += OnBlogManagerFinishedLoadingLibrary;
            ManagerController.FinishedCrawlingLastBlog += OnFinishedCrawlingLastBlog;
            QueueController.QueueSettings = _queueSettings;
            QueueController.QueueManager = _queueManager;
            DetailsController.QueueManager = _queueManager;
            CrawlerController.QueueManager = _queueManager;

            Task managerControllerInit = ManagerController.InitializeAsync();
            QueueController.Initialize();
            DetailsController.Initialize();
            CrawlerController.Initialize();
            _cookieService.SetUriCookie(_cookieList);
        }

        public async void Run()
        {
            ShellViewModel.IsQueueViewVisible = true;
            ShellViewModel.Show();

            // Let the UI to initialize first before loading the queuelist.
            await Dispatcher.CurrentDispatcher.InvokeAsync(ManagerController.RestoreColumn, DispatcherPriority.ApplicationIdle);
            await Dispatcher.CurrentDispatcher.InvokeAsync(QueueController.Run, DispatcherPriority.ApplicationIdle);

            if (_appSettings.LastUpdateCheck != DateTime.Today)
            {
                await CheckForUpdatesComplete(_applicationUpdateService.GetLatestReleaseFromServer());
                _appSettings.LastUpdateCheck = DateTime.Today;
            }

            await CheckForTMData();

            ShellViewModel.SetThumbButtonInfosCommands();

            CheckForVCRedistributable();
        }

        public void Shutdown()
        {
            DetailsController.Shutdown();
            QueueController.Shutdown();
            ManagerController.Shutdown();
            CrawlerController.Shutdown();

            SaveSettings();
        }

        private string GetAppDataPath()
        {
            string savePath = _environmentService.AppSettingsPath;
            if (_appSettings.PortableMode)
            {
                savePath = AppDomain.CurrentDomain.BaseDirectory;
            }
            return savePath;
        }

        private void SaveSettings()
        {
            string savePath = GetAppDataPath();

            SaveSettings(Path.Combine(savePath, AppSettingsFileName), _appSettings);
            SaveSettings(Path.Combine(savePath, QueueSettingsFileName), _queueSettings);
            SaveSettings(Path.Combine(savePath, ManagerSettingsFileName), _managerSettings);
            SaveSettings(Path.Combine(savePath, CookiesFileName), new List<Cookie>(_cookieService.GetAllCookies()));
        }

        private void OnSettingsUpdated(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void OnBlogManagerFinishedLoadingLibrary(object sender, EventArgs e)
        {
            QueueController.LoadQueue();
        }

        private void OnFinishedCrawlingLastBlog(object sender, EventArgs e)
        {
            DetailsController.OnFinishedCrawlingLastBlog(EventArgs.Empty);
        }

        private async Task CheckForUpdatesComplete(Task<string> task)
        {
            try
            {
                string updateText = await task;
                if (updateText != null || !_applicationUpdateService.IsNewVersionAvailable()) return;
                updateText = string.Format(CultureInfo.CurrentCulture, Resources.NewVersionAvailable, _applicationUpdateService.GetNewAvailableVersion());
                string url = _applicationUpdateService.GetDownloadUri().AbsoluteUri;
                MessageBoxResult ret = MessageBoxResult.No;
                if (updateText != null && url != null)
                    ret = MessageBox.Show($"{updateText}\n{Resources.DownloadAndInstallNewVersion}", Resources.DownloadNewVersionTitle, MessageBoxButton.YesNo);
                if (ret == MessageBoxResult.Yes)
                    DownloadAndUnzipUpdatePackage(url);
            }
            catch (Exception e)
            {
                Logger.Error("ModuleController.CheckForUpdatesComplete: {0}", e.ToString());
            }
        }

        private async Task CheckForTMData()
        {
            try
            {
                if (_appSettings.TMLastCheck.AddDays(14) > DateTime.Today) return;

                await _logService.SendLogData();
                _appSettings.TMLastCheck = DateTime.Today;
            }
            catch (Exception e)
            {
                Logger.Error("ModuleController.CheckForTMData: {0}", e.ToString());
            }
        }

        private void DownloadAndUnzipUpdatePackage(string url)
        {
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.UpdateMode = Mode.ForcedDownload;
            try
            {
                UpdateInfoEventArgs args = new UpdateInfoEventArgs { DownloadURL = url };
                if (AutoUpdater.DownloadUpdate(args))
                {
                    ((IShellView)_shellViewModel.Value.View).Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error("ModuleController.DownloadAndUnzipUpdatePackage: {0}", e.ToString());
                MessageBox.Show(e.Message, e.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CheckForVCRedistributable()
        {
            try
            {
                if (!IsVC2019Installed())
                {
                    var url = Environment.Is64BitProcess ? "https://aka.ms/vs/17/release/vc_redist.x64.exe" : "https://aka.ms/vs/17/release/vc_redist.x86.exe";
                    MessageBoxResult ret = MessageBoxResult.No;
                    ret = MessageBox.Show($"{Resources.DownloadVCRedistributable}", Resources.DownloadVCRedistributableTitle, MessageBoxButton.YesNo);
                    if (ret == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo(url));
                }
            }
            catch (Exception e)
            {
                Logger.Error("ModuleController.CheckForRedistributableAsync: {0}", e.ToString());
            }
        }

        private static bool IsVC2019Installed()
        {
            string dependenciesPath = @"SOFTWARE\Classes\Installer\Dependencies";

            using (RegistryKey dependencies = Registry.LocalMachine.OpenSubKey(dependenciesPath))
            {
                if (dependencies == null) return false;

                foreach (string subKeyName in dependencies.GetSubKeyNames().Where(n => !n.ToLower().Contains("dotnet") && !n.ToLower().Contains("microsoft")))
                {
                    using (RegistryKey subDir = Registry.LocalMachine.OpenSubKey(dependenciesPath + "\\" + subKeyName))
                    {
                        var value = subDir.GetValue("DisplayName")?.ToString() ?? null;
                        if (string.IsNullOrEmpty(value)) continue;

                        var pf = Environment.Is64BitProcess ? "x64" : "x86";
                        if (Regex.IsMatch(value, $@"C\+\+ 2015.*\({pf}\)"))
                        {
                            value = subDir.GetValue("Version")?.ToString() ?? null;
                            if (string.IsNullOrEmpty(value)) continue;

                            var vs = new Version(value);
                            if (vs >= new Version(14, 20)) return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool CheckIfPortableMode(string fileName)
        {
            return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName));
        }

        private T LoadSettings<T>(string fileName) where T : class, new()
        {
            try
            {
                return _settingsProvider.LoadSettings<T>(fileName);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not read the settings file: {0}", ex);
                return new T();
            }
        }

        private void SaveSettings(string fileName, object settings)
        {
            try
            {
                _settingsProvider.SaveSettings(fileName, settings);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not save the settings file: {0}", ex);
            }
        }

        private void ShowDetailsView()
        {
            ShellViewModel.IsDetailsViewVisible = true;
        }

        private void ShowQueueView()
        {
            ShellViewModel.IsQueueViewVisible = true;
        }

        private void UpdateDetailsView()
        {
            if (!ShellViewModel.IsQueueViewVisible)
            {
                ShellViewModel.IsDetailsViewVisible = true;
            }
        }

        private static void InitializeCultures(AppSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.Language))
            {
                var ci = new CultureInfo(settings.Language);
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                CultureInfo.CurrentCulture = ci;
                CultureInfo.CurrentUICulture = ci;
            }
        }
    }
}
