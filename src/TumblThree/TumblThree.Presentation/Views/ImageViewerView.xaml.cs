using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for AboutView.xaml.
    /// </summary>
    [Export(typeof(IImageViewerView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ImageViewerView : Window, IImageViewerView
    {
        private readonly Lazy<ImageViewerViewModel> viewModel;

        public ImageViewerView()
        {
            InitializeComponent();
            viewModel = new Lazy<ImageViewerViewModel>(() => ViewHelper.GetViewModel<ImageViewerViewModel>(this));
        }

        private ImageViewerViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            var folder = viewModel.Value.ImageFolder;
            WpfImageViewer.MainWindow wnd = new WpfImageViewer.MainWindow(folder);
            _ = wnd.ShowDialogAsync(owner as Window);
        }
    }
}
