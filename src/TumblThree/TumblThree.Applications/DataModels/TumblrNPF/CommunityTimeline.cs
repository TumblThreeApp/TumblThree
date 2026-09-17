using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    /*
    /// <summary>
    /// Communities data classes
    /// </summary>
    /// <remarks>
    /// Only contains adapted and new classes, the rest is used from PeeprBlogTimeline/Common.
    /// </remarks>
    */

    public class CommunityTimeline
    {
        [JsonProperty("routeSet")]
        public string RouteSet { get; set; }

        [JsonProperty("routeUsesPalette")]
        public bool RouteUsesPalette { get; set; }

        [JsonProperty("routeHidesLowerRightContent")]
        public bool RouteHidesLowerRightContent { get; set; }

        [JsonProperty("routeName")]
        public string RouteName { get; set; }

        [JsonProperty("isInitialRequestPeepr")]
        public bool IsInitialRequestPeepr { get; set; }

        [JsonProperty("isInitialRequestSSRModal")]
        public bool IsInitialRequestSSRModal { get; set; }

        [JsonProperty("viewport-monitor")]
        public ViewportMonitor ViewportMonitor { get; set; }

        [JsonProperty("randomNumber")]
        public double RandomNumber { get; set; }

        [JsonProperty("chunkNames")]
        public List<string> ChunkNames { get; private set; } = new List<string>();

        [JsonProperty("communities")]
        public object Communities { get; set; }

        [JsonProperty("queries")]
        public QueriesC Queries { get; set; }

        [JsonProperty("csrfToken")]
        public string CsrfToken { get; set; }

        [JsonProperty("apiUrl")]
        public string ApiUrl { get; set; }

        [JsonProperty("apiFetchStore")]
        public ApiFetchStore ApiFetchStore { get; set; }

        [JsonProperty("cspNonce")]
        public string CspNonce { get; set; }

        [JsonProperty("languageData")]
        public LanguageData LanguageData { get; set; }

        [JsonProperty("configRef")]
        public ConfigRef ConfigRef { get; set; }

        [JsonProperty("reportingInfo")]
        public ReportingInfo ReportingInfo { get; set; }

        [JsonProperty("analyticsInfo")]
        public AnalyticsInfo AnalyticsInfo { get; set; }

        [JsonProperty("adPlacementConfiguration")]
        public AdPlacementConfiguration AdPlacementConfiguration { get; set; }

        [JsonProperty("privacy")]
        public Privacy Privacy { get; set; }

        [JsonProperty("endlessScrollingDisabled")]
        public bool EndlessScrollingDisabled { get; set; }

        [JsonProperty("bestStuffFirstDisabled")]
        public bool BestStuffFirstDisabled { get; set; }

        [JsonProperty("colorizedTags")]
        public bool ColorizedTags { get; set; }

        [JsonProperty("autoTruncatingPosts")]
        public bool AutoTruncatingPosts { get; set; }

        [JsonProperty("timestamps")]
        public bool Timestamps { get; set; }

        [JsonProperty("communityLabelVisibilitySetting")]
        public string CommunityLabelVisibilitySetting { get; set; }

        [JsonProperty("labsSettings")]
        public LabsSettings LabsSettings { get; set; }

        [JsonProperty("isLoggedIn")]
        public IsLoggedIn2 IsLoggedIn { get; set; }

        [JsonProperty("recaptchaV3PublicKey")]
        public RecaptchaV3PublicKey RecaptchaV3PublicKey { get; set; }

        [JsonProperty("vapidPublicKey")]
        public VapidPublicKey VapidPublicKey { get; set; }

        [JsonProperty("obfuscatedFeatures")]
        public string ObfuscatedFeatures { get; set; }

        [JsonProperty("browserInfo")]
        public BrowserInfo BrowserInfo { get; set; }

        [JsonProperty("sessionInfo")]
        public SessionInfo SessionInfo { get; set; }

        [JsonProperty("cssMapUrl")]
        public string CssMapUrl { get; set; }
    }
        
    public class QueriesC
    {
        [JsonProperty("mutations")]
        public List<object> Mutations { get; private set; } = new List<object>();

        [JsonProperty("queries")]
        public List<QueryC> Queries { get; private set; } = new List<QueryC>();
    }

    public class QueryC
    {
        [JsonProperty("dehydratedAt")]
        public long DehydratedAt { get; set; }

        [JsonProperty("state")]
        public StateC State { get; set; }

        [JsonProperty("queryKey")]
        public List<object> QueryKey { get; private set; } = new List<object>();

        [JsonProperty("queryHash")]
        public string QueryHash { get; set; }

        [JsonProperty("queryType", NullValueHandling = NullValueHandling.Ignore)]
        public string QueryType { get; set; }
    }
       
    public class StateC
    {
        [JsonProperty("data")]
        [JsonConverter(typeof(StateDataConverter))]
        public object Data { get; set; }

        [JsonProperty("dataUpdateCount")]
        public int DataUpdateCount { get; set; }

        [JsonProperty("dataUpdatedAt")]
        public long DataUpdatedAt { get; set; }

        [JsonProperty("error")]
        public object Error { get; set; }

        [JsonProperty("errorUpdateCount")]
        public int ErrorUpdateCount { get; set; }

        [JsonProperty("errorUpdatedAt")]
        public long ErrorUpdatedAt { get; set; }

        [JsonProperty("fetchFailureCount")]
        public int FetchFailureCount { get; set; }

        [JsonProperty("fetchFailureReason")]
        public object FetchFailureReason { get; set; }

        [JsonProperty("fetchMeta")]
        public object FetchMeta { get; set; }

        [JsonProperty("isInvalidated")]
        public bool IsInvalidated { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("fetchStatus")]
        public string FetchStatus { get; set; }
    }

    public class DataUser
    {
        [JsonProperty("isLoggedIn")]
        public bool IsLoggedIn { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }
    }

    public class Datum
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("timelineUri")]
        public string TimelineUri { get; set; }

        [JsonProperty("searchTimelineUri")]
        public string SearchTimelineUri { get; set; }

        [JsonProperty("loggingId")]
        public string LoggingId { get; set; }
    }
}
