using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TumblThree.Presentation
{
    public abstract class GlobalExceptionHandlerBase
    {
        public GlobalExceptionHandlerBase()
        {
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            Application.Current.Dispatcher.UnhandledExceptionFilter += OnFilterDispatcherException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// This methods gets invoked for every unhandled excption
        /// that is raise on the application Dispatcher, the AppDomain
        /// or by the GC cleaning up a faulted Task.
        /// </summary>
        /// <param name="e">The unhandled exception</param>
        /// <param name="terminate">whether continue or terminate app</param>
        public abstract void OnUnhandledException(Exception e, bool terminate);

        /// <summary>
        /// Override this method to decide if the <see cref="OnUnhandledException(Exception, bool)"/>
        /// method should be called for the passes Dispatcher exception.
        /// </summary>
        /// <param name="exception">The unhandled excpetion on the applications dispatcher.</param>
        /// <returns>True if the <see cref="OnUnhandledException(Exception, bool)"/> method should
        /// be called. False if not</returns>
        protected virtual bool CatchDispatcherException(Exception exception) => true;

        /// <summary>
        /// Override this method to change the Log output of this
        /// class from the Debug.WriteLine to your needs.
        /// </summary>
        /// <param name="msg">The message to be logged.</param>
        protected virtual void Log(string msg) => Debug.WriteLine(msg);


        /// <summary>
        /// This method is invoked whenever there is an unhandled
        /// exception on a delegate that was posted to be executed
        /// on the UI-thread (Dispatcher) of a WPF application.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"Unhandled exception on application dispatcher: {e.Exception}");
            OnUnhandledException(e.Exception, false);
            e.Handled = true;
        }

        /// <summary>
        /// This event is invoked whenever there is an unhandled
        /// exception in the default AppDomain. It is invoked for
        /// exceptions on any thread that was created on the AppDomain.
        /// </summary>
        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = ExtractExceptionObject(e);
            Log($"Unhandled exception on current AppDomain (IsTerminating = {e.IsTerminating}): {ex}");
            OnUnhandledException(ex, e.IsTerminating);

            Thread.CurrentThread.IsBackground = true;
            Thread.CurrentThread.Name = "Dead thread";
            while (true)
                Thread.Sleep(TimeSpan.FromHours(1));
        }


        /// <summary>
        /// This method is called when a faulted task, which has the
        /// exception object set, gets collected by the GC. This is useful
        /// to track Exceptions in asnync methods where the caller forgets
        /// to await the returning task
        /// </summary>
        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log($"Unobserved task exception: {e.Exception}");
            //OnUnhandledException(e.Exception, false);
            e.SetObserved();
        }

        /// <summary>
        /// The method gets called for any unhandled exception on the
        /// Dispatcher. When e.RequestCatch is set to true, the exception
        /// is catched by the Dispatcher and the DispatcherUnhandledException
        /// event will be invoked.
        /// </summary>
        private void OnFilterDispatcherException(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            e.RequestCatch = CatchDispatcherException(e.Exception);
        }

        /// <summary>
        /// This method extracts the exception instance of the AppDomains 
        /// <see cref="UnhandledExceptionEventArgs"/> object. If the exception
        /// is not of type System.Exception, it will be wrapped in a new Exception object.
        /// </summary>
        private static Exception ExtractExceptionObject(UnhandledExceptionEventArgs e)
        {
            return e.ExceptionObject as Exception ??
                new Exception($"AppDomainUnhandledException: Unknown exceptionobject: {e.ExceptionObject}");
        }
    }
}
