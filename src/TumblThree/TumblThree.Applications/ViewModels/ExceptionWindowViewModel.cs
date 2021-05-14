using System;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Input;
using TumblThree.Applications.Services;

namespace TumblThree.Presentation.Exceptions
{
    public class ExceptionWindowViewModel
    {
        private readonly AsyncDelegateCommand _executeCommand;
        private bool allowClosing;
        private ILogService _logService;

        public Exception Exception { get; }

        public string ExceptionType
        {
            get
            {
                return Exception.GetType().FullName;
            }
        }

        public string TumblThreeVersionString => _logService.TumblThreeVersionString;

        public string WindowsVersionString => _logService.WindowsVersionString;

        public string DefaultBrowserString => _logService.DefaultBrowserString;

        public string RegionSettingsString => _logService.RegionSettingsString;

        public string NetFrameworkVersionString => _logService.NetFrameworkVersionString;

        public bool IsTerminating { get; }

        public bool IsSendErrorDetailsEnabled { get; set; }

        public bool AllowClosing => allowClosing;

        public string ButtonText
        {
            get
            {
                return IsTerminating ? "Exit Application" : "Continue";
            }
        }

        public ExceptionWindowViewModel(ILogService logService, Exception exception, bool isTerminating)
        {
            _logService = logService;
            Exception = exception;
            IsTerminating = isTerminating;
            IsSendErrorDetailsEnabled = true;
            _executeCommand = new AsyncDelegateCommand(Execute);
        }

        public event EventHandler OnRequestClose;

        public ICommand ExecuteCommand => _executeCommand;

        private async Task Execute()
        {
            if (IsTerminating)
            {
                if (IsSendErrorDetailsEnabled)
                    await SendErrorDetails();
                Application.Current.Shutdown();
            }
            else
            {
                allowClosing = true;
                OnRequestClose(this, EventArgs.Empty);
                if (IsSendErrorDetailsEnabled)
                    await SendErrorDetails();
            }
        }

        public async Task SendErrorDetails()
        {
            await _logService.SendErrorDetails(Exception);
        }
    }
}
