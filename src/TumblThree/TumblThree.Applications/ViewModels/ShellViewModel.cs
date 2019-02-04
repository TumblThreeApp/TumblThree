using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ShellViewModel : ViewModel<IShellView>
    {
        private readonly DelegateCommand _closeErrorCommand;
        private readonly DelegateCommand _exitCommand;
        private readonly DelegateCommand _garbageCollectorCommand;
        private readonly DelegateCommand _showAboutCommand;
        private readonly DelegateCommand _showSettingsCommand;

        private readonly ExportFactory<AboutViewModel> _aboutViewModelFactory;
        private readonly ObservableCollection<Tuple<Exception, string>> _errors;
        private readonly AppSettings _settings;
        private readonly ExportFactory<SettingsViewModel> _settingsViewModelFactory;

        private object _detailsView;

        [ImportingConstructor]
        public ShellViewModel(IShellView view, IShellService shellService, ICrawlerService crawlerService, ExportFactory<SettingsViewModel> settingsViewModelFactory, ExportFactory<AboutViewModel> aboutViewModelFactory)
            : base(view)
        {
            ShellService = shellService;
            CrawlerService = crawlerService;
            _settings = shellService.Settings;
            _settingsViewModelFactory = settingsViewModelFactory;
            _aboutViewModelFactory = aboutViewModelFactory;
            _errors = new ObservableCollection<Tuple<Exception, string>>();
            _exitCommand = new DelegateCommand(Close);
            _closeErrorCommand = new DelegateCommand(CloseError);
            _garbageCollectorCommand = new DelegateCommand(GC.Collect);
            _showSettingsCommand = new DelegateCommand(ShowSettingsView);
            _showAboutCommand = new DelegateCommand(ShowAboutView);

            _errors.CollectionChanged += ErrorsCollectionChanged;
            view.Closed += ViewClosed;

            // Restore the window size when the values are valid.
            if (_settings.Left >= 0 && _settings.Top >= 0 && _settings.Width > 0 && _settings.Height > 0
                && _settings.Left + _settings.Width <= view.VirtualScreenWidth
                && _settings.Top + _settings.Height <= view.VirtualScreenHeight)
            {
                view.Left = _settings.Left;
                view.Top = _settings.Top;
                view.Height = _settings.Height;
                view.Width = _settings.Width;
                view.GridSplitterPosition = _settings.GridSplitterPosition;
            }

            view.IsMaximized = _settings.IsMaximized;
        }

        public string Title => ApplicationInfo.ProductName;

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService { get; }

        public IReadOnlyList<Tuple<Exception, string>> Errors => _errors;

        public Tuple<Exception, string> LastError => _errors.LastOrDefault();

        public ICommand ExitCommand => _exitCommand;

        public ICommand CloseErrorCommand => _closeErrorCommand;

        public ICommand GarbageCollectorCommand => _garbageCollectorCommand;

        public ICommand ShowSettingsCommand => _showSettingsCommand;

        public ICommand ShowAboutCommand => _showAboutCommand;

        public object DetailsView
        {
            get => _detailsView;
            private set => SetProperty(ref _detailsView, value);
        }

        public bool IsDetailsViewVisible
        {
            get => DetailsView == ShellService.DetailsView;
            set
            {
                if (value)
                {
                    DetailsView = ShellService.DetailsView;
                }
            }
        }

        public bool IsQueueViewVisible
        {
            get => DetailsView == ShellService.QueueView;
            set
            {
                if (value)
                {
                    DetailsView = ShellService.QueueView;
                }
            }
        }

        public void Show() => ViewCore.Show();

        private void Close() => ViewCore.Close();

        public void ShowSettingsView()
        {
            SettingsViewModel settingsViewModel = _settingsViewModelFactory.CreateExport().Value;
            settingsViewModel.ShowDialog(ShellService.ShellView);
        }

        public void ShowAboutView()
        {
            AboutViewModel aboutViewModel = _aboutViewModelFactory.CreateExport().Value;
            aboutViewModel.ShowDialog(ShellService.ShellView);
        }

        public void ShowError(Exception exception, string message)
        {
            var errorMessage = new Tuple<Exception, string>(exception, message);
            if (
                !_errors.Any(
                    error =>
                        (error.Item1?.ToString() ?? "null") == (errorMessage.Item1?.ToString() ?? "null") &&
                        error.Item2 == errorMessage.Item2))
            {
                _errors.Add(errorMessage);
            }
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(DetailsView))
            {
                RaisePropertyChanged(nameof(IsDetailsViewVisible));
                RaisePropertyChanged(nameof(IsQueueViewVisible));
            }
        }

        private void CloseError()
        {
            if (_errors.Any())
            {
                _errors.RemoveAt(_errors.Count - 1);
            }
        }

        private void ErrorsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RaisePropertyChanged(nameof(LastError));

        private void ViewClosed(object sender, EventArgs e)
        {
            _settings.Left = ViewCore.Left;
            _settings.Top = ViewCore.Top;
            _settings.Height = ViewCore.Height;
            _settings.Width = ViewCore.Width;
            _settings.IsMaximized = ViewCore.IsMaximized;
            _settings.GridSplitterPosition = ViewCore.GridSplitterPosition;
        }
    }
}
