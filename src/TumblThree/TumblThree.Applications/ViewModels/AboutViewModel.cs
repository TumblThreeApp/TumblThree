using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class AboutViewModel : ViewModel<IAboutView>
    {
        private readonly AsyncDelegateCommand _checkForUpdatesCommand;
        private readonly DelegateCommand _downloadCommand;
        private readonly DelegateCommand _showWebsiteCommand;
        private readonly DelegateCommand _feedbackCommand;

        private readonly ExportFactory<FeedbackViewModel> _feedbackViewModelFactory;
        private readonly IApplicationUpdateService _applicationUpdateService;
        private bool _isCheckInProgress;
        private bool _isLatestVersionAvailable;
        private string _updateText;

        [ImportingConstructor]
        public AboutViewModel(IAboutView view, IApplicationUpdateService applicationUpdateService, ExportFactory<FeedbackViewModel> feedbackViewModelFactory)
            : base(view)
        {
            _showWebsiteCommand = new DelegateCommand(ShowWebsite);
            _checkForUpdatesCommand = new AsyncDelegateCommand(CheckForUpdates);
            _downloadCommand = new DelegateCommand(DownloadNewVersion);
            _feedbackCommand = new DelegateCommand(Feedback);
            _applicationUpdateService = applicationUpdateService;
            _feedbackViewModelFactory = feedbackViewModelFactory;
        }

        public ICommand ShowWebsiteCommand => _showWebsiteCommand;

        public ICommand CheckForUpdatesCommand => _checkForUpdatesCommand;

        public ICommand DownloadCommand => _downloadCommand;

        public ICommand FeedbackCommand => _feedbackCommand;

#pragma warning disable CA1822
        public string ProductName => ApplicationInfo.ProductName;

        public string Version => ApplicationInfo.Version;

        public string OsVersion => Environment.OSVersion.ToString();

        public string NetVersion => Environment.Version.ToString();

        public bool Is64BitProcess => Environment.Is64BitProcess;
#pragma warning restore CA1822

        public bool IsCheckInProgress
        {
            get => _isCheckInProgress;
            set => SetProperty(ref _isCheckInProgress, value);
        }

        public bool IsLatestVersionAvailable
        {
            get => _isLatestVersionAvailable;
            set => SetProperty(ref _isLatestVersionAvailable, value);
        }

        public string UpdateText
        {
            get => _updateText;
            set => SetProperty(ref _updateText, value);
        }

        public void ShowDialog(object owner) => ViewCore.ShowDialog(owner);

        private void ShowWebsite(object parameter)
        {
            var url = (string)parameter;
            try
            {
                Process.Start(url);
            }
            catch (Exception e)
            {
                Logger.Error("An exception occured when trying to show the url '{0}'. Exception: {1}", url, e);
            }
        }

        private void DownloadNewVersion() => Process.Start(new ProcessStartInfo(_applicationUpdateService.GetDownloadUri().AbsoluteUri));

        private async Task CheckForUpdates()
        {
            if (IsCheckInProgress || IsLatestVersionAvailable)
            {
                return;
            }

            IsCheckInProgress = true;
            IsLatestVersionAvailable = false;
            UpdateText = string.Empty;
            await CheckForUpdatesComplete(_applicationUpdateService.GetLatestReleaseFromServer());
        }

        private void Feedback()
        {
            FeedbackViewModel feedbackViewModel = _feedbackViewModelFactory.CreateExport().Value;
            feedbackViewModel.ShowDialog(this);
        }

        private async Task CheckForUpdatesComplete(Task<string> task)
        {
            IsCheckInProgress = false;
            if (await task == null)
            {
                if (_applicationUpdateService.IsNewVersionAvailable())
                {
                    UpdateText = string.Format(CultureInfo.CurrentCulture, Resources.NewVersionAvailable, _applicationUpdateService.GetNewAvailableVersion());
                    IsLatestVersionAvailable = true;
                }
                else
                {
                    UpdateText = string.Format(CultureInfo.CurrentCulture, Resources.ApplicationUpToDate);
                }
            }
            else
            {
                UpdateText = await task;
            }
        }
    }
}
