using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for CrawlerView.xaml.
    /// </summary>
    [Export(typeof(ICrawlerView))]
    public partial class CrawlerView : ICrawlerView
    {
        private readonly Lazy<CrawlerViewModel> viewModel;

        public CrawlerView()
        {
            InitializeComponent();
            viewModel = new Lazy<CrawlerViewModel>(() => ViewHelper.GetViewModel<CrawlerViewModel>(this));
        }

        private CrawlerViewModel ViewModel
        {
            get { return viewModel.Value; }
        }
        public void HideText()
        {
            textAddBlog.Visibility = System.Windows.Visibility.Collapsed;
            textRemoveBlog.Visibility = System.Windows.Visibility.Collapsed;
            textImportFromBlogListFile.Visibility = System.Windows.Visibility.Collapsed;
            textShowFiles.Visibility = System.Windows.Visibility.Collapsed;
            textAddToQueue.Visibility = System.Windows.Visibility.Collapsed;
            textRemoveFromQueue.Visibility = System.Windows.Visibility.Collapsed;
            textDownload.Visibility = System.Windows.Visibility.Collapsed;
            textResume.Visibility = System.Windows.Visibility.Collapsed;
            textStop.Visibility = System.Windows.Visibility.Collapsed;
            textClearMonitor.Visibility = System.Windows.Visibility.Collapsed;
            textSettings.Visibility = System.Windows.Visibility.Collapsed;
            textAbout.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
