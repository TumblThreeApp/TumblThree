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
    [Export("TumblrTagSearchView", typeof(IDetailsView))]
    public partial class DetailsTumblrTagSearchView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrTagSearchViewModel> viewModel;

        public DetailsTumblrTagSearchView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrTagSearchViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrTagSearchViewModel>(this));
        }

        private DetailsTumblrTagSearchViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public int TabsCount => this.Tabs.Items.Count;

        // FIXME: Implement in proper MVVM.
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
