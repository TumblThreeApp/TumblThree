using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;
using static TumblThree.Domain.Models.Blogs.Blog;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class FullScreenMediaViewModel : ViewModel<IFullScreenMediaView>
    {
        private IDetailsService _detailsService;
        private IBlog _blogFile;

        private string _lastDownloadedPhoto;
        private string _lastDownloadedVideo;
        private PostType _states;

        [ImportingConstructor]
        public FullScreenMediaViewModel(IFullScreenMediaView view, IDetailsService detailsService)
            : base(view)
        {
            _detailsService = detailsService;
            _detailsService.DetailsViewModelChanged += DetailsService_DetailsViewModelChanged;
            _detailsService.FinishedCrawlingLastBlog += DetailsService_FinishedCrawlingLastBlog;
        }

        public string LastDownloadedPhoto => _lastDownloadedPhoto;

        public string LastDownloadedVideo => _lastDownloadedVideo;

        public PostType States => _states;

        public void ShowDialog(object owner)
        {
            ViewCore.ShowDialog(owner);
        }

        private void DetailsService_DetailsViewModelChanged(object sender, System.EventArgs e)
        {
            _blogFile = _detailsService.DetailsViewModel.BlogFile;
            _blogFile.PropertyChanged += Blog_PropertyChanged;
        }

        private void DetailsService_FinishedCrawlingLastBlog(object sender, EventArgs e)
        {
            ((IFullScreenMediaView)View).Close();
        }

        private void Blog_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "LastDownloadedPhoto" && !string.IsNullOrEmpty(_blogFile?.LastDownloadedPhoto))
            {
                _lastDownloadedPhoto = _blogFile.LastDownloadedPhoto;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(LastDownloadedPhoto)));
            }
            else if (e.PropertyName == "LastDownloadedVideo" && !string.IsNullOrEmpty(_blogFile?.LastDownloadedVideo))
            {
                _lastDownloadedVideo = _blogFile.LastDownloadedVideo;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(LastDownloadedVideo)));
            }
            else if (e.PropertyName == "States")
            {
                _states = _blogFile.States;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(States)));
            }
        }
    }
}
