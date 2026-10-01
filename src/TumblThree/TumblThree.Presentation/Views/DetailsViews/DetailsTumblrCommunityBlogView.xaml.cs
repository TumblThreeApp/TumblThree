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
    [Export("TumblrCommunityBlogView", typeof(IDetailsView))]
    public partial class DetailsTumblrCommunityBlogView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrCommunityBlogViewModel> viewModel;

        public DetailsTumblrCommunityBlogView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrCommunityBlogViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrCommunityBlogViewModel>(this));
        }

        private DetailsTumblrCommunityBlogViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public int TabsCount => this.Tabs.Items.Count;

        private void Preview_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.ViewFullScreenMedia();
        }

        private void View_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!((UserControl)sender).IsKeyboardFocusWithin)
                ViewModel.ViewLostFocus();
        }

        private void FilenameTemplate_PreviewLostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = !ViewModel.FilenameTemplateValidate(((TextBox)e.Source).Text);
        }
    }
}
