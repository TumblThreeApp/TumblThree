using System;
using System.Runtime.ExceptionServices;
using System.Windows;

namespace TumblThree.Applications.Extensions
{
    public static class ExceptionExtension
    {
        public static void ThrowOnDispatcher(this Exception ex)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                // preserve the callstack of the exception
                ExceptionDispatchInfo.Capture(ex).Throw();
            }));
        }
    }
}
