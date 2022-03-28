using System;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels
{
    [Serializable]
    public class LogData
    {
        public LogData(bool isLongPathSupported,
            string winVersion, string winEdition, string winBitness, string winReleaseId, string winVersionNumber,
            string tumblThreeVersion, string tumblThreeBitness,
            string defaultBrowser, string defaultBrowserVersion,
            string winRegionCulture, string winRegionCountry,
            string netFrameworkVersion, string netFrameworkBitness, string netVersionSupport,
            string machHash, string usrHash, DateTime timestamp)
        {
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
            NetVersionSupport = netVersionSupport;
            MachHash = machHash;
            UsrHash = usrHash;
            Timestamp = timestamp;
        }

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

        public string NetVersionSupport { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string MachHash { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string UsrHash { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
