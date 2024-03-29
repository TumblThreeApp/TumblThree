﻿using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Controls;
using System.Windows.Input;
using TumblThree.Applications.ViewModels.DetailsViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for QueueView.xaml.
    /// </summary>
    [Export("AllView", typeof(IDetailsView))]
    public partial class DetailsAllView : IDetailsView
    {
        private readonly Lazy<DetailsAllViewModel> viewModel;

        public DetailsAllView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsAllViewModel>(() => ViewHelper.GetViewModel<DetailsAllViewModel>(this));
        }

        private DetailsAllViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public int TabsCount => this.Tabs.Items.Count;

        private void Preview_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.ViewFullScreenMedia();
        }

        private void FilenameTemplate_PreviewLostKeyboardFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            e.Handled = !ViewModel.FilenameTemplateValidate(((TextBox)e.Source).Text);
        }
    }
}
