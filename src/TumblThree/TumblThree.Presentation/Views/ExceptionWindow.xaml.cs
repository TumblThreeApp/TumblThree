using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using TumblThree.Presentation.Exceptions;
using TumblThree.Presentation.Extensions;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow : Window
    {
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;

        private bool allowClosing;

        public ExceptionWindow()
        {
            InitializeComponent();
            this.Owner = App.Current.MainWindow;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (((ExceptionWindowViewModel)DataContext).IsTerminating)
                Application.Current.Shutdown();
            else
            {
                allowClosing = true;
                Close();
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (allowClosing) return;
            e.Cancel = true;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var hWnd = new WindowInteropHelper(this);
            var sysMenu = NativeMethods.GetSystemMenu(hWnd.Handle, false);
            NativeMethods.EnableMenuItem(sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
        }
    }
}
