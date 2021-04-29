using System;
using System.Windows;
using TumblThree.Presentation.Views;

namespace TumblThree.Presentation.Exceptions
{
    /// <summary>
    /// This ExceptionHandler implementation opens a new
    /// error window for every unhandled exception that occurs.
    /// </summary>
    internal class WindowExceptionHandler : GlobalExceptionHandlerBase
    {
        /// <summary>
        /// This method opens a new ExceptionWindow with the
        /// passed exception object as datacontext.
        /// </summary>
        public override void OnUnhandledException(Exception e, bool terminate)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var exceptionWindow = new ExceptionWindow();
                exceptionWindow.DataContext = new ExceptionWindowViewModel(e, terminate);
                exceptionWindow.ShowDialog();
            }));
        }
    }
}
