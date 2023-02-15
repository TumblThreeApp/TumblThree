using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Security;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain;

namespace TumblThree.Presentation.Views
{
    [Export(typeof(IShellView))]
    public partial class ShellWindow : Window, IShellView
    {
        private const string TaskbarListInterfaceGuid = "56FDF342-FD6D-11d0-958A-006097C9A090";
        private const string TaskbarListObjectGuid = "56FDF344-FD6D-11d0-958A-006097C9A090";

        private readonly Lazy<ShellViewModel> viewModel;
        private bool isForceClose;

        [SecurityCritical, SuppressUnmanagedCodeSecurity]
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(TaskbarListInterfaceGuid)]
        private interface ITaskbarList
        {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
        }

        private static bool IsWindows7orNewer => Environment.OSVersion.Version >= new Version(6, 1);

        [SecuritySafeCritical]
        private static bool IsTaskbarListAvailable()
        {
            try
            {
                ITaskbarList taskbarList = (ITaskbarList)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid(TaskbarListObjectGuid)));
                try
                {
                    taskbarList.HrInit();
                }
                catch (NotImplementedException)
                {
                    return false;
                }
                finally
                {
                    _ = Marshal.ReleaseComObject(taskbarList);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public ShellWindow()
        {
            InitializeComponent();
            viewModel = new Lazy<ShellViewModel>(() => ViewHelper.GetViewModel<ShellViewModel>(this));

            try
            {
                if (IsWindows7orNewer && IsTaskbarListAvailable())
                {
                    var taskbarItemInfo = new TaskbarItemInfo();
                    taskbarItemInfo.ThumbButtonInfos.Add(new ThumbButtonInfo() { ImageSource = Application.Current.Resources["PlayButtonImage"] as DrawingImage });
                    taskbarItemInfo.ThumbButtonInfos.Add(new ThumbButtonInfo() { ImageSource = Application.Current.Resources["PauseButtonImage"] as DrawingImage });
                    taskbarItemInfo.ThumbButtonInfos.Add(new ThumbButtonInfo() { ImageSource = Application.Current.Resources["ResumeButtonImage"] as DrawingImage });
                    taskbarItemInfo.ThumbButtonInfos.Add(new ThumbButtonInfo() { ImageSource = Application.Current.Resources["StopButtonImage"] as DrawingImage });
                    TaskbarItemInfo = taskbarItemInfo;
                }
            }
            catch (NotImplementedException ex)
            {
                Logger.Error("ShellWindow.ShellWindow(): {0}", ex);
            }

            Closing += ShellWindow_Closing;
        }

        private void ShellWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isForceClose)
            {
                e.Cancel = false;
                return;
            }

            e.Cancel = true;
            ((App)Application.Current).RequestShutdown();
        }

        public void CloseForced()
        {
            isForceClose = true;
            Close();
        }

        public void SetThumbButtonInfoCommands()
        {
            if (TaskbarItemInfo is null) { return; }

            TaskbarItemInfo.ThumbButtonInfos[0].Command = viewModel.Value.CrawlerService.CrawlCommand;
            TaskbarItemInfo.ThumbButtonInfos[1].Command = viewModel.Value.CrawlerService.PauseCommand;
            TaskbarItemInfo.ThumbButtonInfos[2].Command = viewModel.Value.CrawlerService.ResumeCommand;
            TaskbarItemInfo.ThumbButtonInfos[3].Command = viewModel.Value.CrawlerService.StopCommand;
        }

        private ShellViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public double VirtualScreenWidth
        {
            get { return SystemParameters.VirtualScreenWidth; }
        }

        public double VirtualScreenHeight
        {
            get { return SystemParameters.VirtualScreenHeight; }
        }

        public bool IsMaximized
        {
            get { return WindowState == WindowState.Maximized; }

            set
            {
                if (value)
                {
                    WindowState = WindowState.Maximized;
                }
                else if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
            }
        }

        public double GridSplitterPosition
        {
            get { return grid.ColumnDefinitions[2].Width.Value; }
            set { grid.ColumnDefinitions[2].Width = new GridLength(value, GridUnitType.Pixel); }
        }
    }
}
