using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TumblThree.Applications.ViewModels.DetailsViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for QueueView.xaml.
    /// </summary>
    [Export("TumblrLikedByView", typeof(IDetailsView))]
    public partial class DetailsTumblrLikedByView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrLikedByViewModel> viewModel;

        public DetailsTumblrLikedByView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrLikedByViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrLikedByViewModel>(this));
        }

        private DetailsTumblrLikedByViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        // FIXME: Implement in proper MVVM.
        private void Preview_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var fullScreenMediaView = new FullScreenMediaView { DataContext = viewModel.Value.BlogFile };
            fullScreenMediaView.ShowDialog();
        }

        private void View_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!((UserControl)sender).IsKeyboardFocusWithin)
                ViewModel.ViewLostFocus();
        }

        private void FilenameTemplate_LostFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.FilenameTemplateValidate(((TextBox)e.Source).Text);
        }
    }
}
