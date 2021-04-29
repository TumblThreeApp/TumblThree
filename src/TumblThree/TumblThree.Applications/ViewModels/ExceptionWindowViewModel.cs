using System;

namespace TumblThree.Presentation.Exceptions
{
    public class ExceptionWindowViewModel
    {
        public Exception Exception { get; }

        public string ExceptionType { get; }

        public bool IsTerminating { get; }

        public string ButtonText
        {
            get
            {
                return IsTerminating ? "Exit Application" : "Continue";
            }
        }

        public ExceptionWindowViewModel(Exception ex, bool terminate)
        {
            Exception = ex;
            ExceptionType = ex.GetType().FullName;
            IsTerminating = terminate;
        }
    }
}
