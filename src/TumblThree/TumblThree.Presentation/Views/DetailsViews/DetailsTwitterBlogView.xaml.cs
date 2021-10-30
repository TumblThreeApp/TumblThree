using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TumblThree.Applications.ViewModels.DetailsViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for QueueView.xaml.
    /// </summary>
    [Export("TwitterBlogView", typeof(IDetailsView))]
    public partial class DetailsTwitterBlogView : IDetailsView
    {
        private readonly Lazy<DetailsTwitterBlogViewModel> viewModel;

        public DetailsTwitterBlogView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTwitterBlogViewModel>(() => ViewHelper.GetViewModel<DetailsTwitterBlogViewModel>(this));
        }

        private DetailsTwitterBlogViewModel ViewModel
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
                ViewModel?.ViewLostFocus();
        }

        private void FilenameTemplate_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = !ViewModel.FilenameTemplateValidate(((TextBox)e.Source).Text);
        }

        private static bool ignoreEvent = false;

        private void Collection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignoreEvent) return;

            e.Handled = !ViewModel.CollectionChanged(new List<Collection>(e.RemovedItems.Cast<Collection>()), new List<Collection>(e.AddedItems.Cast<Collection>()));
            if (e.Handled && e.RemovedItems != null && e.RemovedItems.Count != 0)
            {
                ignoreEvent = true;
                CollectionComboBox.SelectedItem = e.RemovedItems[0];
                ignoreEvent = false;
            }
        }
    }
}
