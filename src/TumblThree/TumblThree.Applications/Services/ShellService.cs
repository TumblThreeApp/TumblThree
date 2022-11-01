using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Waf.Applications.Services;
using System.Waf.Foundation;
using System.Waf.Presentation.Services;
using System.Windows.Input;
using TumblThree.Applications.Auth;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Views;
using TumblThree.Domain;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IShellService))]
    [Export]
    internal class ShellService : Model, IShellService
    {
        private readonly List<ApplicationBusyContext> applicationBusyContext;
        private readonly Lazy<IShellView> shellView;
        private readonly Lazy<IMessageService> messageService;
        private readonly List<Task> tasksToCompleteBeforeShutdown;
        private object aboutView;
        private ClipboardMonitor clipboardMonitor;
        private object contentView;
        private object crawlerView;
        private object detailsView;
        private bool isApplicationBusy;
        private bool isClosingEventInitialized;
        private OAuthManager oauthManager;
        private object queueView;
        private object settingsView;
        private int isLongPathSupported = -1;

        public event EventHandler SettingsUpdatedHandler;

        [ImportingConstructor]
        public ShellService(Lazy<IShellView> shellView, Lazy<IMessageService> messageService)
        {
            this.shellView = shellView;
            this.messageService = messageService;
            tasksToCompleteBeforeShutdown = new List<Task>();
            applicationBusyContext = new List<ApplicationBusyContext>();
            clipboardMonitor = new ClipboardMonitor();
            oauthManager = new OAuthManager();
        }

        public object SettingsView
        {
            get => settingsView;
            set => SetProperty(ref settingsView, value);
        }

        public object AboutView
        {
            get => aboutView;
            set => SetProperty(ref aboutView, value);
        }

        public Action<Exception, string> ShowErrorAction { get; set; }

        public Action ShowDetailsViewAction { get; set; }

        public Action ShowQueueViewAction { get; set; }

        public Action UpdateDetailsViewAction { get; set; }

        public AppSettings Settings { get; set; }

        public object ShellView => shellView.Value;

        private IMessageService MessageService => messageService.Value;

        public object ContentView
        {
            get => contentView;
            set => SetProperty(ref contentView, value);
        }

        public object DetailsView
        {
            get => detailsView;
            set => SetProperty(ref detailsView, value);
        }

        public object QueueView
        {
            get => queueView;
            set => SetProperty(ref queueView, value);
        }

        public object CrawlerView
        {
            get => crawlerView;
            set => SetProperty(ref crawlerView, value);
        }

        public IReadOnlyCollection<Task> TasksToCompleteBeforeShutdown => tasksToCompleteBeforeShutdown;

        public bool IsApplicationBusy
        {
            get => isApplicationBusy;
            private set => SetProperty(ref isApplicationBusy, value);
        }

        public bool IsLongPathSupported
        {
            get
            {
                if (isLongPathSupported == -1)
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem"))
                        {
                            if (key != null)
                            {
                                Object o = key.GetValue("LongPathsEnabled");
                                if (o != null && o is int)
                                {
                                    isLongPathSupported = (int)o;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine($"ShellService.IsLongPathSupported: {ex}");
                        throw;
                    }
                }
                return isLongPathSupported == 1;
            }
        }

        public bool CheckForWebView2Runtime()
        {
            bool found = false;
            try
            {
                found = IsWebView2Installed();
                if (!found)
                {
                    var url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
                    if (MessageService.ShowYesNoQuestion(Resources.DownloadWebView2Runtime, Resources.DownloadComponentTitle))
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            catch (Exception e)
            {
                Logger.Error("ModuleController.CheckForWebView2Runtime: {0}", e.ToString());
            }
            return found;
        }

        private static bool IsWebView2Installed()
        {
            string regPath = Environment.Is64BitOperatingSystem ? @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" :
                @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

            using (RegistryKey machineKey = Registry.LocalMachine.OpenSubKey(regPath))
            {
                var value = machineKey?.GetValue("pv")?.ToString();
                if (value != null)
                {
                    var version = new Version(value);
                    if (version >= new Version("106.0.1370.52")) { return true; }
                }
            }

            regPath = Environment.Is64BitOperatingSystem ? @"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" :
                @"Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";

            using (RegistryKey userKey = Registry.CurrentUser.OpenSubKey(regPath))
            {
                var value = userKey?.GetValue("pv")?.ToString();
                if (value != null)
                {
                    var version = new Version(value);
                    if (version >= new Version("106.0.1370.52")) { return true; }
                }
            }

            return false;
        }

        public static bool IsWriteProtectedInstallation
        {
            get
            {
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var hasWriteAccess = HasCurrentUserDirectoryAccessRights(appPath, FileSystemRights.Write);
                return !hasWriteAccess;
            }
        }

        private static bool HasCurrentUserDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
            var hasAccessRights = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var authorizationRules = acl.GetAccessRules(true, true, typeof(NTAccount));

                var currentIdentity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentIdentity);
                foreach (AuthorizationRule authorizationRule in authorizationRules)
                {
                    var accessRule = authorizationRule as FileSystemAccessRule;
                    if (accessRule == null)
                    {
                        continue;
                    }

                    if ((accessRule.FileSystemRights & accessRights) != 0)
                    {
                        var account = authorizationRule.IdentityReference as NTAccount;
                        if (account == null)
                        {
                            continue;
                        }

                        if (principal.IsInRole(account.Value))
                        {
                            if (accessRule.AccessControlType == AccessControlType.Deny)
                            {
                                hasAccessRights = false;
                                break;
                            }
                            hasAccessRights = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                hasAccessRights = false;
            }
            return hasAccessRights;
        }

        public event CancelEventHandler Closing
        {
            add
            {
                Closing1 += value;
                InitializeClosingEvent();
            }
            remove => Closing1 -= value;
        }

        public void ShowError(Exception exception, string displayMessage)
        {
            ShowErrorAction(exception, displayMessage);
        }

        public void ShowDetailsView() => ShowDetailsViewAction();

        public void UpdateDetailsView() => UpdateDetailsViewAction();

        public void ShowQueueView() => ShowQueueViewAction();

        public void AddTaskToCompleteBeforeShutdown(Task task) => tasksToCompleteBeforeShutdown.Add(task);

        public IDisposable SetApplicationBusy()
        {
            var context = new ApplicationBusyContext()
            {
                DisposeCallback = ApplicationBusyContextDisposeCallback
            };
            applicationBusyContext.Add(context);
            IsApplicationBusy = true;
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => Mouse.OverrideCursor = Cursors.Wait);
            return context;
        }

        public ClipboardMonitor ClipboardMonitor
        {
            get => clipboardMonitor;
            set => SetProperty(ref clipboardMonitor, value);
        }

        public OAuthManager OAuthManager
        {
            get => oauthManager;
            set => SetProperty(ref oauthManager, value);
        }

        private event CancelEventHandler Closing1;

        protected virtual void OnClosing(CancelEventArgs e) => Closing1?.Invoke(this, e);

        private void ApplicationBusyContextDisposeCallback(ApplicationBusyContext context)
        {
            applicationBusyContext.Remove(context);
            var isBusy = applicationBusyContext.Any();
            IsApplicationBusy = isBusy;
            if (isBusy) { return; }
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => Mouse.OverrideCursor = null);
        }

        private void InitializeClosingEvent()
        {
            if (isClosingEventInitialized)
            {
                return;
            }

            isClosingEventInitialized = true;
            shellView.Value.Closing += ShellViewClosing;
        }

        private void ShellViewClosing(object sender, CancelEventArgs e) => OnClosing(e);

        public void InitializeOAuthManager()
        {
            OAuthManager["consumer_key"] = Settings.ApiKey;
            OAuthManager["consumer_secret"] = Settings.SecretKey;
            OAuthManager["token"] = Settings.OAuthToken;
            OAuthManager["token_secret"] = Settings.OAuthTokenSecret;
        }

        public void SettingsUpdated()
        {
            SettingsUpdatedHandler.Invoke(null, EventArgs.Empty);
        }
    }
}
