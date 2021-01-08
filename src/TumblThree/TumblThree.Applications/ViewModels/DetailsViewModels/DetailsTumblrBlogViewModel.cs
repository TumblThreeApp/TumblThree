using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Forms;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
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
        private IBlog _blogFile;
        private int _count = 0;

        [ImportingConstructor]
        public DetailsTumblrBlogViewModel([Import("TumblrBlogView", typeof(IDetailsView))] IDetailsView view, IClipboardService clipboardService)
            : base(view)
        {
            _clipboardService = clipboardService;
            _copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
            _browseFileDownloadLocationCommand = new DelegateCommand(BrowseFileDownloadLocation);
        }

        public ICommand CopyUrlCommand => _copyUrlCommand;

        public ICommand BrowseFileDownloadLocationCommand => _browseFileDownloadLocationCommand;

        public IBlog BlogFile
        {
            get => _blogFile;
            set => SetProperty(ref _blogFile, value);
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
            var dialog = new FolderBrowserDialog { SelectedPath = BlogFile.FileDownloadLocation };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                BlogFile.FileDownloadLocation = dialog.SelectedPath;
            }
        }
    }
}
