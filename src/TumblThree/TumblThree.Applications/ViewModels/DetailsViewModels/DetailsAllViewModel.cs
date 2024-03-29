﻿using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Forms;
using System.Windows.Input;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
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

        [ImportingConstructor]
        public DetailsAllViewModel([Import("AllView", typeof(IDetailsView))] IDetailsView view, IClipboardService clipboardService, IDetailsService detailsService)
            : base(view)
        {
            _clipboardService = clipboardService;
            _detailsService = detailsService;
            _copyUrlCommand = new DelegateCommand(CopyUrlToClipboard);
            _browseFileDownloadLocationCommand = new DelegateCommand(BrowseFileDownloadLocation);
        }

        public ICommand CopyUrlCommand => _copyUrlCommand;

        public ICommand BrowseFileDownloadLocationCommand => _browseFileDownloadLocationCommand;

        public void ViewFullScreenMedia()
        {
            _detailsService.ViewFullScreenMedia();
        }

        public bool FilenameTemplateValidate(string enteredFilenameTemplate)
        {
            return _detailsService.FilenameTemplateValidate(enteredFilenameTemplate);
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
