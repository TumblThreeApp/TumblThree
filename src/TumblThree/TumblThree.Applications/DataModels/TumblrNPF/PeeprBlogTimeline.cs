using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    public class Main
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

        [JsonProperty("PeeprRoute")]
        public PeeprRoute PeeprRoute { get; set; }

        [JsonProperty("queries")]
        public Queries2 Queries { get; set; }

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

    public class AdPlacementConfiguration
    {
        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("placements")]
        public Placements Placements { get; set; }
    }

    public class Ads
    {
        [JsonProperty("hashedUserId")]
        public string HashedUserId { get; set; }
    }

    public class AnalyticsInfo
    {
        [JsonProperty("ads")]
        public Ads Ads { get; set; }

        [JsonProperty("automattic")]
        public Automattic Automattic { get; set; }

        [JsonProperty("kraken")]
        public Kraken Kraken { get; set; }
    }

    public class ApiFetchStore
    {
        [JsonProperty("API_TOKEN")]
        public string APITOKEN { get; set; }

        [JsonProperty("extraHeaders")]
        public string ExtraHeaders { get; set; }
    }

    public class Automattic
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Browser
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("major")]
        public string Major { get; set; }
    }

    public class BrowserInfo
    {
        [JsonProperty("userAgent")]
        public UserAgent UserAgent { get; set; }

        [JsonProperty("deviceType")]
        public string DeviceType { get; set; }

        [JsonProperty("isSupported")]
        public bool IsSupported { get; set; }

        [JsonProperty("isCrawler")]
        public bool IsCrawler { get; set; }
    }

    public class ClientDetails
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("os_name")]
        public string OsName { get; set; }

        [JsonProperty("os_version")]
        public string OsVersion { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("build_version")]
        public string BuildVersion { get; set; }

        [JsonProperty("form_factor")]
        public string FormFactor { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("connection")]
        public string Connection { get; set; }

        [JsonProperty("carrier")]
        public string Carrier { get; set; }

        [JsonProperty("browser_name")]
        public string BrowserName { get; set; }

        [JsonProperty("browser_version")]
        public string BrowserVersion { get; set; }
    }

    public class Colors
    {
        [JsonProperty("c0")]
        public string C0 { get; set; }

        [JsonProperty("c1")]
        public string C1 { get; set; }

        [JsonProperty("c2")]
        public string C2 { get; set; }

        [JsonProperty("c3")]
        public string C3 { get; set; }

        [JsonProperty("c4")]
        public string C4 { get; set; }
    }

    public class ConfigRef
    {
        [JsonProperty("saberKey")]
        public string SaberKey { get; set; }

        [JsonProperty("saberEndpoint")]
        public string SaberEndpoint { get; set; }

        [JsonProperty("cslEndpoint")]
        public string CslEndpoint { get; set; }

        [JsonProperty("cslCookie")]
        public string CslCookie { get; set; }

        [JsonProperty("cslPerformanceHeaders")]
        public string CslPerformanceHeaders { get; set; }

        [JsonProperty("searchFilterDef")]
        public string SearchFilterDef { get; set; }

        [JsonProperty("fanPlacementId")]
        public string FanPlacementId { get; set; }

        [JsonProperty("unreadPostsCountUrl")]
        public string UnreadPostsCountUrl { get; set; }

        [JsonProperty("nsfwScoreThreshold")]
        public string NsfwScoreThreshold { get; set; }

        [JsonProperty("displayIoMaxAdCount")]
        public string DisplayIoMaxAdCount { get; set; }

        [JsonProperty("displayIoMaxAdLoadingCount")]
        public string DisplayIoMaxAdLoadingCount { get; set; }

        [JsonProperty("displayIoPlacementId")]
        public string DisplayIoPlacementId { get; set; }

        [JsonProperty("displayIoTestPlacementId")]
        public string DisplayIoTestPlacementId { get; set; }

        [JsonProperty("displayIoInterscrollerDisplayTestPlacementId")]
        public string DisplayIoInterscrollerDisplayTestPlacementId { get; set; }

        [JsonProperty("displayIoInterscrollerVideoTestPlacementId")]
        public string DisplayIoInterscrollerVideoTestPlacementId { get; set; }

        [JsonProperty("takeoverLogoUrl")]
        public string TakeoverLogoUrl { get; set; }

        [JsonProperty("flags")]
        public string Flags { get; set; }

        [JsonProperty("lsFlushSize")]
        public string LsFlushSize { get; set; }

        [JsonProperty("lsFlushTime")]
        public string LsFlushTime { get; set; }

        [JsonProperty("lsPerfFlushSize")]
        public string LsPerfFlushSize { get; set; }

        [JsonProperty("lsPerfFlushTime")]
        public string LsPerfFlushTime { get; set; }

        [JsonProperty("autoTruncatePosts")]
        public string AutoTruncatePosts { get; set; }

        [JsonProperty("viewWithBlogTheme")]
        public string ViewWithBlogTheme { get; set; }

        [JsonProperty("tumblrmartLastUpdated")]
        public int TumblrmartLastUpdated { get; set; }

        [JsonProperty("rewardedAdTimeoutSeconds")]
        public int RewardedAdTimeoutSeconds { get; set; }

        [JsonProperty("vungleAdTokenSyncSeconds")]
        public int VungleAdTokenSyncSeconds { get; set; }
    }

    public class Cpu
    {
        [JsonProperty("architecture")]
        public string Architecture { get; set; }
    }

    public class Data
    {
    }

    public class Device
    {
    }

    public class Engine
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class GoogleNativeBlogNetworkHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeBlogsHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeCommunitiesHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeCommunityHubsHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeDashboardForYouHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeDashboardHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeDashboardYourTagsHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeExploreStaffPicksHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativePermalinkHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class GoogleNativeSearchHydraSource
    {
        [JsonProperty("adSource")]
        public string AdSource { get; set; }

        [JsonProperty("adPlacementId")]
        public string AdPlacementId { get; set; }

        [JsonProperty("maxAdCount")]
        public int MaxAdCount { get; set; }

        [JsonProperty("maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [JsonProperty("expireTime")]
        public int ExpireTime { get; set; }

        [JsonProperty("timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [JsonProperty("loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    public class HeaderContext
    {
        [JsonProperty("label")]
        public Label Label { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }

    public class InitialTimeline
    {
        [JsonProperty("objects")]
        public List<Post> Objects { get; private set; } = new List<Post>();

        [JsonProperty("nextLink")]
        public NextLink NextLink { get; set; }
    }

    public class IsLoggedIn2
    {
        [JsonProperty("isPartiallyRegistered")]
        public bool IsPartiallyRegistered { get; set; }

        [JsonProperty("isLoggedIn")]
        public bool IsLoggedIn { get; set; }
    }

    public class Kraken
    {
        [JsonProperty("basePage")]
        public string BasePage { get; set; }

        [JsonProperty("routeSet")]
        public string RouteSet { get; set; }

        [JsonProperty("krakenBaseUrl")]
        public string KrakenBaseUrl { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("clientDetails")]
        public ClientDetails ClientDetails { get; set; }

        [JsonProperty("configRef")]
        public ConfigRef ConfigRef { get; set; }
    }

    public class Label
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class LabsSettings
    {
        [JsonProperty("optIn")]
        public bool OptIn { get; set; }

        [JsonProperty("experiment426")]
        public bool Experiment426 { get; set; }

        [JsonProperty("experiment422")]
        public bool Experiment422 { get; set; }

        [JsonProperty("experiment425")]
        public bool Experiment425 { get; set; }

        [JsonProperty("experiment423")]
        public bool Experiment423 { get; set; }

        [JsonProperty("experiment424")]
        public bool Experiment424 { get; set; }

        [JsonProperty("experiment13")]
        public bool Experiment13 { get; set; }

        [JsonProperty("experiment31")]
        public bool Experiment31 { get; set; }

        [JsonProperty("experiment214")]
        public bool Experiment214 { get; set; }

        [JsonProperty("experiment420")]
        public bool Experiment420 { get; set; }

        [JsonProperty("experiment500")]
        public bool Experiment500 { get; set; }
    }

    public class LanguageData
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; }
    }

    public class NextLink
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("queryParams")]
        public QueryParams QueryParams { get; set; }
    }

    public class Os
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class PeeprRoute
    {
        [JsonProperty("initialTimeline")]
        public InitialTimeline InitialTimeline { get; set; }
    }

    public class Placements
    {
        [JsonProperty("googleNativeDashboardHydraSource")]
        public GoogleNativeDashboardHydraSource GoogleNativeDashboardHydraSource { get; set; }

        [JsonProperty("googleNativeDashboardForYouHydraSource")]
        public GoogleNativeDashboardForYouHydraSource GoogleNativeDashboardForYouHydraSource { get; set; }

        [JsonProperty("googleNativeDashboardYourTagsHydraSource")]
        public GoogleNativeDashboardYourTagsHydraSource GoogleNativeDashboardYourTagsHydraSource { get; set; }

        [JsonProperty("googleNativeExploreStaffPicksHydraSource")]
        public GoogleNativeExploreStaffPicksHydraSource GoogleNativeExploreStaffPicksHydraSource { get; set; }

        [JsonProperty("googleNativeCommunityHubsHydraSource")]
        public GoogleNativeCommunityHubsHydraSource GoogleNativeCommunityHubsHydraSource { get; set; }

        [JsonProperty("googleNativeSearchHydraSource")]
        public GoogleNativeSearchHydraSource GoogleNativeSearchHydraSource { get; set; }

        [JsonProperty("googleNativeBlogsHydraSource")]
        public GoogleNativeBlogsHydraSource GoogleNativeBlogsHydraSource { get; set; }

        [JsonProperty("googleNativePermalinkHydraSource")]
        public GoogleNativePermalinkHydraSource GoogleNativePermalinkHydraSource { get; set; }

        [JsonProperty("googleNativeCommunitiesHydraSource")]
        public GoogleNativeCommunitiesHydraSource GoogleNativeCommunitiesHydraSource { get; set; }

        [JsonProperty("googleNativeBlogNetworkHydraSource")]
        public GoogleNativeBlogNetworkHydraSource GoogleNativeBlogNetworkHydraSource { get; set; }
    }

    public class Privacy
    {
        [JsonProperty("ccpaPrivacyString")]
        public string CcpaPrivacyString { get; set; }

        [JsonProperty("personalizedAdvertisingGdpr")]
        public bool PersonalizedAdvertisingGdpr { get; set; }

        [JsonProperty("personalizedAdvertisingCcpa")]
        public bool PersonalizedAdvertisingCcpa { get; set; }

        [JsonProperty("personalizedAdvertisingIos")]
        public bool PersonalizedAdvertisingIos { get; set; }

        [JsonProperty("personalizedAdvertising")]
        public bool PersonalizedAdvertising { get; set; }
    }

    public class Queries2
    {
        [JsonProperty("mutations")]
        public List<object> Mutations { get; private set; } = new List<object>();

        [JsonProperty("queries")]
        public List<Query> Queries { get; private set; } = new List<Query>();

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public State State { get; set; }

        [JsonProperty("queryKey", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> QueryKey { get; private set; } = new List<object>();

        [JsonProperty("queryHash", NullValueHandling = NullValueHandling.Ignore)]
        public string QueryHash { get; set; }
    }

    public class Query
    {
        [JsonProperty("state")]
        public State State { get; set; }

        [JsonProperty("queryKey")]
        public List<object> QueryKey { get; private set; } = new List<object>();

        [JsonProperty("queryHash")]
        public string QueryHash { get; set; }
    }

    public class RecaptchaV3PublicKey
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class RecommendationReason
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("loggingReason")]
        public string LoggingReason { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("links")]
        public Links Links { get; set; }
    }

    public class ReportingInfo
    {
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }

    public class SessionInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class State
    {
        [JsonProperty("data")]
        public Blog Data { get; set; }

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

    public class UserAgent
    {
        [JsonProperty("ua")]
        public string Ua { get; set; }

        [JsonProperty("browser")]
        public Browser Browser { get; set; }

        [JsonProperty("engine")]
        public Engine Engine { get; set; }

        [JsonProperty("os")]
        public Os Os { get; set; }

        [JsonProperty("device")]
        public Device Device { get; set; }

        [JsonProperty("cpu")]
        public Cpu Cpu { get; set; }
    }

    public class VapidPublicKey
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ViewportMonitor
    {
        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }
    }
}
