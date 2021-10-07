using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for SettingsView.xaml.
    /// </summary>
    [Export(typeof(ISettingsView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class SettingsView : ISettingsView
    {
        private readonly Lazy<SettingsViewModel> viewModel;

        public SettingsView()
        {
            InitializeComponent();
            viewModel = new Lazy<SettingsViewModel>(() => ViewHelper.GetViewModel<SettingsViewModel>(this));
        }

        private SettingsViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }

        private void closeWindow(object sender, RoutedEventArgs e)
        {
            if (!SettingsViewModel.CollectionNameValidate(CollectionName.Text))
            {
                e.Handled = true;
            }
            else
            {
                Close();
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            MinWidth = ActualWidth;
            MinHeight = ActualHeight;
            MaxHeight = ActualHeight;
        }

        private void FilenameTemplate_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = !ViewModel.FilenameTemplateValidate(((TextBox)e.Source).Text);
        }

        private void FocusCollectionNameOnButtonClick(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(CollectionName);
        }

        private void CollectionName_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = !SettingsViewModel.CollectionNameValidate(((TextBox)e.Source).Text);
        }
    }
}
