using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Waf.Applications;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.ViewModels.DetailsViewModels
{
    [Export(typeof(IDetailsViewModel))]
    [ExportMetadata("BlogType", typeof(Blog))]
    public class DetailsAllViewModel : ViewModel<IDetailsView>, IDetailsViewModel
    {
        private readonly DelegateCommand _browseFileDownloadLocationCommand;
        private readonly DelegateCommand _copyUrlCommand;

        private readonly IClipboardService _clipboardService;
        private readonly IDetailsService _detailsService;
        private IBlog _blogFile;
        private IBlogAll _blogAll;
        private int _count;

        private ObservableCollection<Collection> _collectionList = new ObservableCollection<Collection>();

        [ImportingConstructor]
        public DetailsAllViewModel([Import("AllView", typeof(IDetailsView))] IDetailsView view, IClipboardService clipboardService, IDetailsService detailsService, ICrawlerService crawlerService)
            : base(view)
        {
            _clipboardService = clipboardService;
            _detailsService = detailsService;
            _copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
            _browseFileDownloadLocationCommand = new DelegateCommand(BrowseFileDownloadLocation);
            foreach (var item in crawlerService.Collections.SourceCollection)
            {
                _collectionList.Add((Collection)item);
            }
            Collections = CollectionViewSource.GetDefaultView(_collectionList);
        }

        public ICommand CopyUrlCommand => _copyUrlCommand;

        public ICommand BrowseFileDownloadLocationCommand => _browseFileDownloadLocationCommand;

        public ICollectionView Collections { get; }

        public ObservableCollection<Collection> CollectionList { get => _collectionList; }

        public void ViewFullScreenMedia()
        {
            _detailsService.ViewFullScreenMedia();
        }

        public bool FilenameTemplateValidate(string enteredFilenameTemplate)
        {
            return _detailsService.FilenameTemplateValidate(enteredFilenameTemplate);
        }

        public Collection CollectionItem
        {
            get
            {
                if (_blogAll == null) return null;
                var item = _collectionList.FirstOrDefault(x => x.Id == _blogAll.CollectionId);
                System.Diagnostics.Debug.WriteLine("Getter called: " + item?.Id);
                return item;
            }
            set
            {
                if (value == null || _blogAll == null) return;
                if (value.Id == _blogAll.CollectionId) return;
                Collection oldItem = _collectionList.FirstOrDefault(x => x.Id == _blogAll.CollectionId);
                Collection newItem = value;
                var changed = _detailsService.ChangeCollection(_blogAll, oldItem, newItem);
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(CollectionItem)));
            }
        }

        public void RaiseCollectionItemChanged()
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CollectionItem)));
        }

        public IBlog BlogFile
        {
            get => _blogFile;
            set => SetProperty(ref _blogFile, value);
        }

        public IBlogAll BlogAll
        {
            get => _blogAll;
            set => SetProperty(ref _blogAll, value);
        }

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        private void CopyUrlToClipboard()
        {
            if (BlogFile != null)
            {
                _clipboardService.SetText(BlogFile.Url);
            }
        }

        private void BrowseFileDownloadLocation()
        {
            using (var dialog = new FolderBrowserDialog { SelectedPath = BlogFile?.FileDownloadLocation })
            {
                if (dialog.ShowDialog() == DialogResult.OK && BlogFile != null)
                {
                    BlogFile.FileDownloadLocation = dialog.SelectedPath;
                }
            }
        }
    }
}
