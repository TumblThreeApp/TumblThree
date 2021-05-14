using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TumblThree.Applications.Views;
using TumblThree.Presentation.Exceptions;
using TumblThree.Presentation.Extensions;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow : Window, IExceptionWindowView, ICloseable
    {
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;

        public ExceptionWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
        }

        private ExceptionWindowViewModel ViewModel
        {
            get { return (ExceptionWindowViewModel)this.DataContext; }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (ViewModel.AllowClosing) return;
            e.Cancel = true;
        }

        private void OnSourceInitialized(object sender, EventArgs e)
        {
            var hWnd = new WindowInteropHelper(this);
            var sysMenu = NativeMethods.GetSystemMenu(hWnd.Handle, false);
            NativeMethods.EnableMenuItem(sysMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
        }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;

            PresentationSource srcDevice = PresentationSource.FromVisual(Owner);
            Matrix m = srcDevice.CompositionTarget.TransformToDevice;
            double factorDPIWidth = m.M11;
            double factorDPIHeight = m.M22;
            double screenWidthInDPI = SystemParameters.PrimaryScreenWidth * factorDPIWidth;
            double screenHeightInDPI = SystemParameters.PrimaryScreenHeight * factorDPIHeight;

            Left = (screenWidthInDPI - Width) / 2;
            Top = (screenHeightInDPI - MaxHeight) / 2;

            base.ShowDialog();
        }
    }
}
