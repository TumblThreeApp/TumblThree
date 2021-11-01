using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TumblThree.Applications.Converter;

namespace TumblThree.Applications.DataModels.TumblrSearchJson
{
    public class SearchJson
    {
        [JsonProperty("routeSet")]
        public string RouteSet { get; set; }

        [JsonProperty("routeUsesPalette")]
        public bool RouteUsesPalette { get; set; }

        [JsonProperty("routeHidesLowerRightContent")]
        public bool RouteHidesLowerRightContent { get; set; }

        [JsonProperty("routeName")]
        public string RouteName { get; set; }

        [JsonProperty("viewport-monitor")]
        public ViewportMonitor ViewportMonitor { get; set; }

        [JsonProperty("chunkNames")]
        public IList<string> ChunkNames { get; set; }

        [JsonProperty("SearchRoute")]
        public SearchRoute SearchRoute { get; set; }

        [JsonProperty("apiUrl")]
        public string ApiUrl { get; set; }

        [JsonProperty("apiFetchStore")]
        public ApiFetchStore ApiFetchStore { get; set; }

        [JsonProperty("languageData")]
        public LanguageData LanguageData { get; set; }

        [JsonProperty("reportingInfo")]
        public ReportingInfo ReportingInfo { get; set; }

        [JsonProperty("analyticsInfo")]
        public AnalyticsInfo AnalyticsInfo { get; set; }

        [JsonProperty("adPlacementConfiguration")]
        public AdPlacementConfiguration AdPlacementConfiguration { get; set; }

        [JsonProperty("privacy")]
        [JsonConverter(typeof(SingleOrArrayConverter<Privacy>))]
        public IList<Privacy> Privacy { get; set; }

        [JsonProperty("endlessScrollingDisabled")]
        public bool? EndlessScrollingDisabled { get; set; }

        [JsonProperty("bestStuffFirstDisabled")]
        public bool? BestStuffFirstDisabled { get; set; }

        [JsonProperty("isLoggedIn")]
        public IsLoggedIn IsLoggedIn { get; set; }

        [JsonProperty("recaptchaV3PublicKey")]
        public RecaptchaV3PublicKey RecaptchaV3PublicKey { get; set; }

        [JsonProperty("obfuscatedFeatures")]
        public string ObfuscatedFeatures { get; set; }

        [JsonProperty("browserInfo")]
        public BrowserInfo BrowserInfo { get; set; }

        [JsonProperty("sessionInfo")]
        public SessionInfo SessionInfo { get; set; }

        [JsonProperty("cssMapUrl")]
        public string CssMapUrl { get; set; }
    }

    public class SearchRoute
    {
        [JsonProperty("searchApiResponse")]
        public SearchApiResponse SearchApiResponse { get; set; }

        [JsonProperty("relatedTags")]
        public IList<string> RelatedTags { get; set; }
    }

    public class TumblrSearchApi
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("response")]
        public Response2 Response { get; set; }
    }

    public class SearchApiResponse
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("response")]
        public Response Response { get; set; }
    }

    public class Data
    {
        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("originalType")]
        public string OriginalType { get; set; }

        [JsonProperty("blogName")]
        public string BlogName { get; set; }

        [JsonProperty("blog")]
        public Blog Blog { get; set; }

        [JsonProperty("isNsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        [JsonProperty("nsfwScore")]
        public int NsfwScore { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("idString")]
        public string IdString { get; set; }

        [JsonProperty("postUrl")]
        public string PostUrl { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("reblogKey")]
        public string ReblogKey { get; set; }

        [JsonProperty("tags")]
        public IList<string> Tags { get; set; }

        [JsonProperty("shortUrl")]
        public string ShortUrl { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("shouldOpenInLegacy")]
        public bool ShouldOpenInLegacy { get; set; }

        [JsonProperty("recommendedSource")]
        public object RecommendedSource { get; set; }

        [JsonProperty("recommendedColor")]
        public object RecommendedColor { get; set; }

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("postAuthor")]
        public string PostAuthor { get; set; }

        [JsonProperty("postAuthorAvatar")]
        public IList<PostAuthorAvatar> PostAuthorAvatar { get; set; }

        [JsonProperty("sourceAttribution")]
        public SourceAttribution SourceAttribution { get; set; }

        [JsonProperty("liked")]
        public bool Liked { get; set; }

        [JsonProperty("noteCount")]
        public int NoteCount { get; set; }

        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; }

        [JsonProperty("sourceTitle")]
        public string SourceTitle { get; set; }

        [JsonProperty("sourceUrlRaw")]
        public string SourceUrlRaw { get; set; }

        [JsonProperty("content")]
        public IList<Content> Content { get; set; }

        [JsonProperty("layout")]
        public IList<Layout> Layout { get; set; }

        [JsonProperty("trail")]
        public IList<object> Trail { get; set; }

        [JsonProperty("placementId")]
        public string PlacementId { get; set; }

        [JsonProperty("canEdit")]
        public bool CanEdit { get; set; }

        [JsonProperty("canDelete")]
        public bool CanDelete { get; set; }

        [JsonProperty("canReply")]
        public bool CanReply { get; set; }

        [JsonProperty("canLike")]
        public bool CanLike { get; set; }

        [JsonProperty("canReblog")]
        public bool CanReblog { get; set; }

        [JsonProperty("canSendInMessage")]
        public bool CanSendInMessage { get; set; }

        [JsonProperty("discardWithoutPosts")]
        public bool DiscardWithoutPosts { get; set; }

        [JsonProperty("embedUrl")]
        public string EmbedUrl { get; set; }

        [JsonProperty("displayAvatar")]
        public bool DisplayAvatar { get; set; }

        [JsonProperty("recommendationReason")]
        public string RecommendationReason { get; set; }

        [JsonProperty("dismissal")]
        public string Dismissal { get; set; }

        [JsonProperty("serveId")]
        public string ServeId { get; set; }
    }

    public class Meta
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

    public class Avatar
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class DescriptionNpf
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Blog
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("avatar")]
        public IList<Avatar> Avatar { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("isAdult")]
        public bool IsAdult { get; set; }

        [JsonProperty("isMember")]
        public bool IsMember { get; set; }

        [JsonProperty("descriptionNpf")]
        public IList<DescriptionNpf> DescriptionNpf { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("canBeFollowed")]
        public bool CanBeFollowed { get; set; }

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("theme")]
        public Theme Theme { get; set; }

        [JsonProperty("shareFollowing")]
        public bool ShareFollowing { get; set; }

        [JsonProperty("shareLikes")]
        public bool ShareLikes { get; set; }

        [JsonProperty("ask")]
        public bool Ask { get; set; }
    }

    public class Theme
    {
        [JsonProperty("headerFullWidth")]
        public int HeaderFullWidth { get; set; }

        [JsonProperty("headerFullHeight")]
        public int HeaderFullHeight { get; set; }

        [JsonProperty("headerFocusWidth")]
        public int HeaderFocusWidth { get; set; }

        [JsonProperty("headerFocusHeight")]
        public int HeaderFocusHeight { get; set; }

        [JsonProperty("avatarShape")]
        public string AvatarShape { get; set; }

        [JsonProperty("backgroundColor")]
        public string BackgroundColor { get; set; }

        [JsonProperty("headerBounds")]
        public string HeaderBounds { get; set; }

        [JsonProperty("headerImage")]
        public string HeaderImage { get; set; }

        [JsonProperty("headerImageFocused")]
        public string HeaderImageFocused { get; set; }

        [JsonProperty("headerImagePoster")]
        public string HeaderImagePoster { get; set; }

        [JsonProperty("headerImageScaled")]
        public string HeaderImageScaled { get; set; }

        [JsonProperty("headerStretch")]
        public bool HeaderStretch { get; set; }

        [JsonProperty("linkColor")]
        public string LinkColor { get; set; }

        [JsonProperty("showAvatar")]
        public bool ShowAvatar { get; set; }

        [JsonProperty("showDescription")]
        public bool ShowDescription { get; set; }

        [JsonProperty("showHeaderImage")]
        public bool ShowHeaderImage { get; set; }

        [JsonProperty("showTitle")]
        public bool ShowTitle { get; set; }

        [JsonProperty("titleColor")]
        public string TitleColor { get; set; }

        [JsonProperty("titleFont")]
        public string TitleFont { get; set; }

        [JsonProperty("titleFontWeight")]
        public string TitleFontWeight { get; set; }
    }

    public class Colors
    {
        [JsonProperty("c0")]
        public string c0 { get; set; }

        [JsonProperty("c1")]
        public string c1 { get; set; }
    }

    public class Medium
    {
        [JsonProperty("mediaKey")]
        public string MediaKey { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("colors")]
        public Colors Colors { get; set; }

        [JsonProperty("cropped")]
        public bool? Cropped { get; set; }

        [JsonProperty("hasOriginalDimensions")]
        public bool? HasOriginalDimensions { get; set; }
    }

    public class Poster
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class EmbedIframe
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Attribution
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("app_name")]
        public string AppName { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("display_text")]
        public string DisplayText { get; set; }
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("media")]
        [JsonConverter(typeof(SingleOrArrayConverter<Medium>))]
        public IList<Medium> Media { get; set; }

        [JsonProperty("colors")]
        public Colors Colors { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("embed_html")]
        public string EmbedHtml { get; set; }

        [JsonProperty("poster")]
        public IList<Poster> Poster { get; set; }

        [JsonProperty("embed_iframe")]
        public EmbedIframe EmbedIframe { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("attribution")]
        [JsonConverter(typeof(SingleOrArrayConverter<Attribution>))]
        public IList<Attribution> Attribution { get; set; }
    }

    public class Posts
    {
        [JsonProperty("data")]
        public IList<Data> Data { get; set; }

        [JsonProperty("links")]
        public Links Links { get; set; }
    }

    public class Response
    {
        [JsonProperty("posts")]
        public Posts Posts { get; set; }

        [JsonProperty("psa")]
        public string Psa { get; set; }

        [JsonProperty("__headers__")]
        public object Headers { get; set; }
    }

    public class Response2
    {
        [JsonProperty("posts")]
        public Posts2 Posts { get; set; }

        [JsonProperty("psa")]
        public string Psa { get; set; }
    }

    public class ViewportMonitor
    {
        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }
    }

    public class DisplayText
    {
        [JsonProperty("install")]
        public string Install { get; set; }

        [JsonProperty("open")]
        public string Open { get; set; }

        [JsonProperty("madeWith")]
        public string MadeWith { get; set; }
    }

    public class AppStoreIds
    {
        [JsonProperty("ios")]
        public string Ios { get; set; }

        [JsonProperty("android")]
        public string Android { get; set; }
    }

    public class SourceAttribution
    {
        [JsonProperty("displayText")]
        public DisplayText DisplayText { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("appStoreUrl")]
        public string AppStoreUrl { get; set; }

        [JsonProperty("playStoreUrl")]
        public string PlayStoreUrl { get; set; }

        [JsonProperty("syndicationId")]
        public object SyndicationId { get; set; }

        [JsonProperty("deepLinks")]
        public IList<object> DeepLinks { get; set; }

        [JsonProperty("appStoreIds")]
        public AppStoreIds AppStoreIds { get; set; }
    }

    public class Exif
    {
        [JsonProperty("Time")]
        public object Time { get; set; }

        [JsonProperty("CameraMake")]
        public string CameraMake { get; set; }

        [JsonProperty("CameraModel")]
        public string CameraModel { get; set; }
    }

    public class Display
    {
        [JsonProperty("blocks")]
        public IList<int> Blocks { get; set; }
    }

    public class Layout
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("display")]
        public IList<Display> Display { get; set; }
    }

    public class PostAuthorAvatar
    {

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class TaggedPost
    {
        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("originalType")]
        public string OriginalType { get; set; }

        [JsonProperty("blogName")]
        public string BlogName { get; set; }

        [JsonProperty("blog")]
        public Blog Blog { get; set; }

        [JsonProperty("isNsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        [JsonProperty("nsfwScore")]
        public int NsfwScore { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("idString")]
        public string IdString { get; set; }

        [JsonProperty("postUrl")]
        public string PostUrl { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("reblogKey")]
        public string ReblogKey { get; set; }

        [JsonProperty("tags")]
        public IList<string> Tags { get; set; }

        [JsonProperty("shortUrl")]
        public string ShortUrl { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("shouldOpenInLegacy")]
        public bool ShouldOpenInLegacy { get; set; }

        [JsonProperty("recommendedSource")]
        public object RecommendedSource { get; set; }

        [JsonProperty("recommendedColor")]
        public object RecommendedColor { get; set; }

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("sourceAttribution")]
        public SourceAttribution SourceAttribution { get; set; }

        [JsonProperty("liked")]
        public bool Liked { get; set; }

        [JsonProperty("noteCount")]
        public int NoteCount { get; set; }

        [JsonProperty("content")]
        public IList<Content> Content { get; set; }

        [JsonProperty("layout")]
        public IList<Layout> Layout { get; set; }

        [JsonProperty("trail")]
        public IList<object> Trail { get; set; }

        [JsonProperty("placementId")]
        public string PlacementId { get; set; }

        [JsonProperty("canEdit")]
        public bool CanEdit { get; set; }

        [JsonProperty("canDelete")]
        public bool CanDelete { get; set; }

        [JsonProperty("canLike")]
        public bool CanLike { get; set; }

        [JsonProperty("canReblog")]
        public bool CanReblog { get; set; }

        [JsonProperty("canSendInMessage")]
        public bool CanSendInMessage { get; set; }

        [JsonProperty("canReply")]
        public bool CanReply { get; set; }

        [JsonProperty("displayAvatar")]
        public bool DisplayAvatar { get; set; }

        [JsonProperty("postAuthor")]
        public string PostAuthor { get; set; }

        [JsonProperty("postAuthorAvatar")]
        public IList<PostAuthorAvatar> PostAuthorAvatar { get; set; }

        [JsonProperty("sourceUrl")]
        public string SourceUrl { get; set; }

        [JsonProperty("sourceTitle")]
        public string SourceTitle { get; set; }

        [JsonProperty("sourceUrlRaw")]
        public string SourceUrlRaw { get; set; }
    }

    public class Datum
    {
        [JsonProperty("object_type")]
        public string ObjectType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("original_type")]
        public string OriginalType { get; set; }

        [JsonProperty("blog_name")]
        public string BlogName { get; set; }

        [JsonProperty("blog")]
        public Blog blog { get; set; }

        [JsonProperty("is_nsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        [JsonProperty("nsfw_score")]
        public int NsfwScore { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("id_string")]
        public string IdString { get; set; }

        [JsonProperty("post_url")]
        public string PostUrl { get; set; }

        [JsonProperty("slug")]
        public string Slug { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("reblog_key")]
        public string ReblogKey { get; set; }

        [JsonProperty("tags")]
        public IList<string> Tags { get; set; }

        [JsonProperty("short_url")]
        public string ShortUrl { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("should_open_in_legacy")]
        public bool ShouldOpenInLegacy { get; set; }

        [JsonProperty("recommended_source")]
        public object RecommendedSource { get; set; }

        [JsonProperty("recommended_color")]
        public object RecommendedColor { get; set; }

        [JsonProperty("note_count")]
        public int NoteCount { get; set; }

        [JsonProperty("content")]
        public IList<Content> Content { get; set; }

        [JsonProperty("layout")]
        public IList<object> Layout { get; set; }

        [JsonProperty("trail")]
        public IList<object> Trail { get; set; }

        [JsonProperty("can_edit")]
        public bool CanEdit { get; set; }

        [JsonProperty("can_delete")]
        public bool CanDelete { get; set; }

        [JsonProperty("can_like")]
        public bool CanLike { get; set; }

        [JsonProperty("can_reblog")]
        public bool CanReblog { get; set; }

        [JsonProperty("can_send_in_message")]
        public bool CanSendInMessage { get; set; }

        [JsonProperty("can_reply")]
        public bool CanReply { get; set; }

        [JsonProperty("display_avatar")]
        public bool DisplayAvatar { get; set; }
    }

    public class Fields
    {
        [JsonProperty("blogs")]
        public string Blogs { get; set; }
    }

    public class QueryParams
    {
        [JsonProperty("fields")]
        public Fields Fields { get; set; }

        [JsonProperty("reblogInfo")]
        public string ReblogInfo { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("limit")]
        public string Limit { get; set; }

        [JsonProperty("blogLimit")]
        public string BlogLimit { get; set; }

        [JsonProperty("postOffset")]
        public string PostOffset { get; set; }

        [JsonProperty("postLimit")]
        public string PostLimit { get; set; }
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

    public class Posts2
    {
        [JsonProperty("data")]
        public IList<Datum> Data { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }

    public class ApiFetchStore
    {
        [JsonProperty("API_TOKEN")]
        public string APITOKEN { get; set; }

        [JsonProperty("extraHeaders")]
        public string ExtraHeaders { get; set; }
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

    public class ReportingInfo
    {
        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }

    public class Ads
    {
        [JsonProperty("hashedUserId")]
        public string HashedUserId { get; set; }
    }

    public class Automattic
    {
        [JsonProperty("id")]
        public string Id { get; set; }
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

    public class ConfigRef
    {
        [JsonProperty("flags")]
        public string Flags { get; set; }
    }

    public class Kraken
    {
        [JsonProperty("basePage")]
        public string BasePage { get; set; }

        [JsonProperty("routeSet")]
        public string RouteSet { get; set; }

        [JsonProperty("krakenHost")]
        public string KrakenHost { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("clientDetails")]
        public ClientDetails ClientDetails { get; set; }

        [JsonProperty("configRef")]
        public ConfigRef ConfigRef { get; set; }
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

    public class TeadsHydraSource
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

    public class TeadsTestHydraSource
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

    public class FlurryHydraSource
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

    public class OneMobileHydraSource
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

    public class IponwebMrecHydraSource
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

    public class TeadsDashboardTop
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

    public class TeadsDashboard
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

    public class Placements
    {
        [JsonProperty("teadsHydraSource")]
        public TeadsHydraSource TeadsHydraSource { get; set; }

        [JsonProperty("teadsTestHydraSource")]
        public TeadsTestHydraSource TeadsTestHydraSource { get; set; }

        [JsonProperty("flurryHydraSource")]
        public FlurryHydraSource FlurryHydraSource { get; set; }

        [JsonProperty("oneMobileHydraSource")]
        public OneMobileHydraSource OneMobileHydraSource { get; set; }

        [JsonProperty("iponwebMrecHydraSource")]
        public IponwebMrecHydraSource IponwebMrecHydraSource { get; set; }

        [JsonProperty("teadsDashboardTop")]
        public TeadsDashboardTop TeadsDashboardTop { get; set; }

        [JsonProperty("teadsDashboard")]
        public TeadsDashboard TeadsDashboard { get; set; }
    }

    public class AdPlacementConfiguration
    {
        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("placements")]
        public Placements Placements { get; set; }
    }

    public class Privacy
    {
        [JsonProperty("ccpaPrivacyString")]
        public string CcpaPrivacyString { get; set; }
    }

    public class IsLoggedIn
    {
        [JsonProperty("isLoggedIn")]
        public bool isLoggedIn { get; set; }

        [JsonProperty("isPartiallyRegistered")]
        public bool isPartiallyRegistered { get; set; }
    }

    public class RecaptchaV3PublicKey
    {
        [JsonProperty("value")]
        public string Value { get; set; }
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

    public class Engine
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class Os
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class Device
    {
    }

    public class Cpu
    {
        [JsonProperty("architecture")]
        public string Architecture { get; set; }
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

    public class SessionInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Formatting
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Urls
    {
        [JsonProperty("web")]
        public string Web { get; set; }

        [JsonProperty("ios")]
        public string Ios { get; set; }

        [JsonProperty("android")]
        public string Android { get; set; }
    }

    public class Action
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("backgroundColor")]
        public string BackgroundColor { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("textColor")]
        public string TextColor { get; set; }

        [JsonProperty("urls")]
        public Urls Urls { get; set; }
    }

    public class NextRequest
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("queryParams")]
        public QueryParams QueryParams { get; set; }
    }

    public class Links
    {
        [JsonProperty("next")]
        public NextRequest Next { get; set; }
    }
}
