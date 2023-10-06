using System;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Waf.Foundation;
using System.Windows.Input;

using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class ManagerViewModel : ViewModel<IManagerView>
    {
        private ICommand _copyUrlCommand;
        private ICommand _checkStatusCommand;
        private ICommand _showDetailsCommand;
        private ICommand _showFilesCommand;
        private ICommand _visitBlogCommand;
        private ICommand _visitBlogOnTumbexCommand;

        private readonly DelegateCommand _viewImagesCommand;
        private readonly ExportFactory<ImageViewerViewModel> _imageViewerViewModelFactory;

        private readonly Lazy<ICrawlerService> _crawlerService;
        private readonly Lazy<IManagerService> _managerService;
        private readonly Lazy<ISelectionService> _selectionService;
        private Blog _selectedBlogFile;

        [ImportingConstructor]
        public ManagerViewModel(IManagerView view, IShellService shellService, Lazy<ISelectionService> selectionService, Lazy<ICrawlerService> crawlerService, Lazy<IManagerService> managerService,
            ExportFactory<ImageViewerViewModel> imageViewerViewModelFactory)
            : base(view)
        {
            ShellService = shellService;
            _selectionService = selectionService;
            _crawlerService = crawlerService;
            _managerService = managerService;

            _viewImagesCommand = new DelegateCommand(ViewImages);
            _imageViewerViewModelFactory = imageViewerViewModelFactory;

            ShellService.Closing += ViewClosed;
        }

        public ISelectionService SelectionService => _selectionService.Value;

        public IShellService ShellService { get; }

        public ICrawlerService CrawlerService => _crawlerService.Value;

        public IManagerService ManagerService => _managerService.Value;

        public ICommand ShowFilesCommand
        {
            get => _showFilesCommand;
            set => SetProperty(ref _showFilesCommand, value);
        }

        public ICommand ViewImagesCommand => _viewImagesCommand;

        public ICommand VisitBlogCommand
        {
            get => _visitBlogCommand;
            set => SetProperty(ref _visitBlogCommand, value);
        }

        public ICommand VisitBlogOnTumbexCommand
        {
            get => _visitBlogOnTumbexCommand;
            set => SetProperty(ref _visitBlogOnTumbexCommand, value);
        }

        public ICommand ShowDetailsCommand
        {
            get => _showDetailsCommand;
            set => SetProperty(ref _showDetailsCommand, value);
        }

        public ICommand CopyUrlCommand
        {
            get => _copyUrlCommand;
            set => SetProperty(ref _copyUrlCommand, value);
        }

        public ICommand CheckStatusCommand
        {
            get => _checkStatusCommand;
            set => SetProperty(ref _checkStatusCommand, value);
        }

        public Blog SelectedBlogFile
        {
            get => _selectedBlogFile;
            set => SetProperty(ref _selectedBlogFile, value);
        }

        public IReadOnlyObservableList<QueueListItem> QueueItems { get; set; }

        public void ViewClosed(object sender, EventArgs e) => ShellService.Settings.ColumnSettings = ViewCore.DataGridColumnRestore;

        public void DataGridColumnRestore()
        {
            try
            {
                if (ShellService.Settings.ColumnSettings.Count != 0)
                {
                    ViewCore.DataGridColumnRestore = ShellService.Settings.ColumnSettings;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ManagerViewModel:ManagerViewModel {0}", ex);
                ShellService.ShowError(new UISettingsException(ex), Resources.CouldNotRestoreUISettings);
                return;
            }
        }

        public void QueueItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                RaisePropertyChanged("QueueItems");
            }
        }

        private void ViewImages()
        {
            ImageViewerViewModel imageViewerViewModel = _imageViewerViewModelFactory.CreateExport().Value;
            imageViewerViewModel.ImageFolder = SelectedBlogFile.DownloadLocation();
            imageViewerViewModel.ShowDialog(ShellService.ShellView);
        }

       }
    }
