using System.Collections.Generic;
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
    [ExportMetadata("BlogType", typeof(TumblrBlog))]
    public class DetailsTumblrBlogViewModel : ViewModel<IDetailsView>, IDetailsViewModel
    {
        private readonly DelegateCommand _browseFileDownloadLocationCommand;
        private readonly DelegateCommand _copyUrlCommand;

        private readonly IClipboardService _clipboardService;
        private readonly IDetailsService _detailsService;
        private IBlog _blogFile;
        private int _count;

        [ImportingConstructor]
        public DetailsTumblrBlogViewModel([Import("TumblrBlogView", typeof(IDetailsView))] IDetailsView view, IClipboardService clipboardService, IDetailsService detailsService,
            ICrawlerService crawlerService)
            : base(view)
        {
            _clipboardService = clipboardService;
            _detailsService = detailsService;
            _copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
            _browseFileDownloadLocationCommand = new DelegateCommand(BrowseFileDownloadLocation);
            Collections = CollectionViewSource.GetDefaultView(crawlerService.Collections);
        }

        public ICommand CopyUrlCommand => _copyUrlCommand;

        public ICommand BrowseFileDownloadLocationCommand => _browseFileDownloadLocationCommand;

        public ICollectionView Collections { get; }

        public void ViewFullScreenMedia()
        {
            _detailsService.ViewFullScreenMedia();
        }

        public void ViewLostFocus()
        {
            if (Count == 1) BlogFile?.Save();
        }

        public bool FilenameTemplateValidate(string enteredFilenameTemplate)
        {
            return _detailsService.FilenameTemplateValidate(enteredFilenameTemplate);
        }

        public int CollectionId
        {
            get
            {
                if (_blogFile == null) return -1;
                return _blogFile.CollectionId;
            }
            set
            {
                if (_blogFile == null) return;
                Collection oldItem = ((IEnumerable<Collection>)Collections.SourceCollection).FirstOrDefault(x => x.Id == _blogFile.CollectionId);
                Collection newItem = ((IEnumerable<Collection>)Collections.SourceCollection).FirstOrDefault(x => x.Id == value);
                var changed = _detailsService.ChangeCollection(_blogFile, oldItem, newItem);
            }
        }

        public IBlog BlogFile
        {
            get => _blogFile;
            set
            {
                SetProperty(ref _blogFile, value);
                if (value != null) RaisePropertyChanged(nameof(CollectionId));
            }
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
