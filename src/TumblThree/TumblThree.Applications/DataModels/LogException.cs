using System;

namespace TumblThree.Applications.DataModels
{
    [Serializable]
    public class LogException
    {
        public LogException(Exception ex, bool isLongPathSupported,
            string winVersion, string winEdition, string winBitness, string winReleaseId, string winVersionNumber,
            string tumblThreeVersion, string tumblThreeBitness,
            string defaultBrowser, string defaultBrowserVersion,
            string winRegionCulture, string winRegionCountry,
            string netFrameworkVersion, string netFrameworkBitness,
            DateTime timestamp)
        {
            ExceptionMessage = ex?.Message;
            ExceptionType = ex.GetType().FullName;
            Callstack = ex.StackTrace;
            InnerException = ex.InnerException?.ToString();
            IsLongPathSupported = isLongPathSupported;
            WinVersion = winVersion;
            WinEdition = winEdition;
            WinBitness = winBitness;
            WinReleaseId = winReleaseId;
            WinVersionNumber = winVersionNumber;
            TumblThreeVersion = tumblThreeVersion;
            TumblThreeBitness = tumblThreeBitness;
            DefaultBrowser = defaultBrowser;
            DefaultBrowserVersion = defaultBrowserVersion;
            WinRegionCulture = winRegionCulture;
            WinRegionCountry = winRegionCountry;
            NetFrameworkVersion = netFrameworkVersion;
            NetFrameworkBitness = netFrameworkBitness;
            Timestamp = timestamp;
        }

        public string ExceptionMessage { get; set; }

        public string ExceptionType { get; set; }

        public string Callstack { get; set; }

        public string InnerException { get; set; }

        public bool IsLongPathSupported { get; set; }

        public string WinVersion { get; set; }

        public string WinEdition { get; set; }

        public string WinBitness { get; set; }

        public string WinReleaseId { get; set; }

        public string WinVersionNumber { get; set; }

        public string TumblThreeBitness { get; set; }

        public string TumblThreeVersion { get; set; }

        public string DefaultBrowser { get; set; }

        public string DefaultBrowserVersion { get; set; }

        public string WinRegionCulture { get; set; }

        public string WinRegionCountry { get; set; }

        public string NetFrameworkVersion { get; set; }

        public string NetFrameworkBitness { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
