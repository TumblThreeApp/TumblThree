using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for FeedbackView.xaml
    /// </summary>
    [Export(typeof(IFeedbackView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class FeedbackView : IFeedbackView
    {
        private readonly Lazy<FeedbackViewModel> viewModel;

        public FeedbackView()
        {
            InitializeComponent();
            viewModel = new Lazy<FeedbackViewModel>(() => ViewHelper.GetViewModel<FeedbackViewModel>(this));
            AddLink();
        }

        private FeedbackViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }

        private void AddLink()
        {
            var text = IntroductionTextBlock.Text;
            try
            {
                var parts = text.Split('{', '}');
                Run run1 = new Run(parts[0]);
                Run run2 = new Run(parts[1]);
                Run run3 = new Run(parts[2]);

                Hyperlink hyperlink = new Hyperlink(run2)
                {
                    NavigateUri = new Uri("https://github.com/TumblThreeApp/TumblThree/issues")
                };
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                IntroductionTextBlock.Inlines.Clear();
                IntroductionTextBlock.Inlines.Add(run1);
                IntroductionTextBlock.Inlines.Add(hyperlink);
                IntroductionTextBlock.Inlines.Add(run3);
            }
            catch (Exception)
            {
                IntroductionTextBlock.Text = text;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageTextBox.Text);
        }
    }
}
