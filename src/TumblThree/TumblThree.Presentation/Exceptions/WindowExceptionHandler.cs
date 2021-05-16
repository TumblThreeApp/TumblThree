using System;
using System.Windows;
using TumblThree.Domain;
using TumblThree.Presentation.Views;

namespace TumblThree.Presentation.Exceptions
{
    /// <summary>
    /// This ExceptionHandler implementation opens a new
    /// error window for every unhandled exception that occurs.
    /// </summary>
    internal class WindowExceptionHandler : GlobalExceptionHandlerBase
    {
        protected override void Log(string msg)
        {
            Logger.Error(msg);
        }

        /// <summary>
        /// This method opens a new ExceptionWindow with the
        /// passed exception object as datacontext.
        /// </summary>
        public override void OnUnhandledException(Exception ex, bool terminate)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var exceptionWindow = new ExceptionWindow();
                var logService = ((App)App.Current).GetLogService();
                var vm = new ExceptionWindowViewModel(logService, ex, terminate);
                vm.OnRequestClose += (s, e) => exceptionWindow.Close();
                exceptionWindow.DataContext = vm;
                exceptionWindow.ShowDialog(App.Current.MainWindow);
            }));
        }
    }
}
