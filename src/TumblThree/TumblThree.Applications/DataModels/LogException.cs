using System;

namespace TumblThree.Applications.DataModels
{
    [Serializable]
    public class LogException : LogData
    {
        public LogException(Exception ex, bool isLongPathSupported, bool terminating,
            string winVersion, string winEdition, string winBitness, string winReleaseId, string winVersionNumber,
            string tumblThreeVersion, string tumblThreeBitness,
            string defaultBrowser, string defaultBrowserVersion,
            string winRegionCulture, string winRegionCountry,
            string netFrameworkVersion, string netFrameworkBitness,
            string netVersionSupport,
            DateTime timestamp)
            : base(isLongPathSupported, winVersion, winEdition, winBitness, winReleaseId, winVersionNumber,
                  tumblThreeVersion, tumblThreeBitness, defaultBrowser, defaultBrowserVersion, winRegionCulture, winRegionCountry,
                  netFrameworkVersion, netFrameworkBitness, netVersionSupport, null, null, timestamp)
        {
            ExceptionMessage = ex?.Message;
            ExceptionType = ex.GetType().FullName;
            Callstack = ex.StackTrace;
            InnerException = ex.InnerException?.ToString();
            Terminating = terminating;
        }

        public string ExceptionMessage { get; set; }

        public string ExceptionType { get; set; }

        public string Callstack { get; set; }

        public string InnerException { get; set; }

        public bool Terminating { get; set; }
    }
}
