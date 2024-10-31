using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TumblThree.Applications.Converter;

namespace TumblThree.Applications.DataModels.TumblrTaggedSearchJson
{
    [DataContract]
    public class TagSearch
    {
        [DataMember(Name = "cookieBootstrap")]
        public string CookieBootstrap { get; set; }

        [DataMember(Name = "routeSet")]
        public string RouteSet { get; set; }

        [DataMember(Name = "routeUsesPalette")]
        public bool RouteUsesPalette { get; set; }

        [DataMember(Name = "routeHidesLowerRightContent")]
        public bool RouteHidesLowerRightContent { get; set; }

        [DataMember(Name = "routeName")]
        public string RouteName { get; set; }

        [DataMember(Name = "isInitialRequestPeepr")]
        public bool IsInitialRequestPeepr { get; set; }

        [DataMember(Name = "viewport-monitor")]
        public ViewportMonitor ViewportMonitor { get; set; }

        [DataMember(Name = "chunkNames")]
        public IList<string> ChunkNames { get; set; }

        [DataMember(Name = "Tagged")]
        public Tagged Tagged { get; set; }

        [DataMember(Name = "queries")]
        public QueriesType Queries { get; set; }

        [DataMember(Name = "csrfToken")]
        public string CsrfToken { get; set; }

        [DataMember(Name = "apiUrl")]
        public string ApiUrl { get; set; }

        [DataMember(Name = "apiFetchStore")]
        public ApiFetchStore ApiFetchStore { get; set; }

        [DataMember(Name = "cspNonce")]
        public string CspNonce { get; set; }

        [DataMember(Name = "languageData")]
        public LanguageData LanguageData { get; set; }

        [DataMember(Name = "reportingInfo")]
        public ReportingInfo ReportingInfo { get; set; }

        [DataMember(Name = "analyticsInfo")]
        public AnalyticsInfo AnalyticsInfo { get; set; }

        [DataMember(Name = "tumblelogAuthToken")]
        public string TumblelogAuthToken { get; set; }

        [DataMember(Name = "adPlacementConfiguration")]
        public AdPlacementConfiguration AdPlacementConfiguration { get; set; }

        [DataMember(Name = "privacy")]
        [JsonConverter(typeof(EmptyArrayOrDictionaryConverter))]
        public Privacy Privacy { get; set; }

        [DataMember(Name = "endlessScrollingDisabled")]
        public bool? EndlessScrollingDisabled { get; set; }

        [DataMember(Name = "bestStuffFirstDisabled")]
        public bool? BestStuffFirstDisabled { get; set; }

        [DataMember(Name = "colorizedTags")]
        public bool? ColorizedTags { get; set; }

        [DataMember(Name = "timestamps")]
        public bool? Timestamps { get; set; }

        [DataMember(Name = "wwwBaseUrl")]
        public string WwwBaseUrl { get; set; }

        [DataMember(Name = "isLoggedIn")]
        public IsLoggedIn IsLoggedIn { get; set; }

        [DataMember(Name = "recaptchaV3PublicKey")]
        public RecaptchaV3PublicKey RecaptchaV3PublicKey { get; set; }

        [DataMember(Name = "obfuscatedFeatures")]
        public string ObfuscatedFeatures { get; set; }

        [DataMember(Name = "browserInfo")]
        public BrowserInfo BrowserInfo { get; set; }

        [DataMember(Name = "sessionInfo")]
        public SessionInfo SessionInfo { get; set; }

        [DataMember(Name = "cssMapUrl")]
        public string CssMapUrl { get; set; }
    }

    [DataContract]
    public class TumblrTaggedSearchApi
    {
        [DataMember(Name = "meta", EmitDefaultValue = false)]
        public Meta Meta { get; set; }

        [DataMember(Name = "response", EmitDefaultValue = false)]
        public Response Response { get; set; }
    }

    [DataContract]
    public class Meta
    {
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public int Status { get; set; }

        [DataMember(Name = "msg", EmitDefaultValue = false)]
        public string Msg { get; set; }
    }

    [DataContract]
    public class QueriesType
    {
        [DataMember(Name = "mutations", EmitDefaultValue = false)]
        public List<object> Mutations { get; set; }

        [DataMember(Name = "queries", EmitDefaultValue = false)]
        public List<Query> Queries { get; set; }
    }

    [DataContract]
    public class Query
    {
        [DataMember(Name = "state", EmitDefaultValue = false)]
        public State State { get; set; }

        [DataMember(Name = "queryKey", EmitDefaultValue = false)]
        public List<string> QueryKey { get; set; }

        [DataMember(Name = "queryHash", EmitDefaultValue = false)]
        public string QueryHash { get; set; }
    }

    [DataContract]
    public class State
    {
        [DataMember(Name = "data", EmitDefaultValue = false)]
        [JsonConverter(typeof(EmptyArrayOrDictionaryConverter))]
        public DataType Data { get; set; }

        [DataMember(Name = "dataUpdateCount", EmitDefaultValue = false)]
        public int DataUpdateCount { get; set; }

        [DataMember(Name = "dataUpdatedAt", EmitDefaultValue = false)]
        public object DataUpdatedAt { get; set; }

        [DataMember(Name = "error", EmitDefaultValue = false)]
        public object Error { get; set; }

        [DataMember(Name = "errorUpdateCount", EmitDefaultValue = false)]
        public int ErrorUpdateCount { get; set; }

        [DataMember(Name = "errorUpdatedAt", EmitDefaultValue = false)]
        public int ErrorUpdatedAt { get; set; }

        [DataMember(Name = "fetchFailureCount", EmitDefaultValue = false)]
        public int FetchFailureCount { get; set; }

        [DataMember(Name = "fetchFailureReason", EmitDefaultValue = false)]
        public object FetchFailureReason { get; set; }

        [DataMember(Name = "fetchMeta", EmitDefaultValue = false)]
        public object FetchMeta { get; set; }

        [DataMember(Name = "isInvalidated", EmitDefaultValue = false)]
        public bool IsInvalidated { get; set; }

        [DataMember(Name = "status", EmitDefaultValue = false)]
        public string Status { get; set; }

        [DataMember(Name = "fetchStatus", EmitDefaultValue = false)]
        public string FetchStatus { get; set; }
    }

    [DataContract]
    public class DataType
    {
        [DataMember(Name = "pages", EmitDefaultValue = false)]
        public List<Page> Pages { get; set; }

        [DataMember(Name = "pageParams", EmitDefaultValue = false)]
        public List<object> PageParams { get; set; }
    }


    [DataContract]
    public class Page
    {
        [DataMember(Name = "items")]
        public List<TaggedPost> Items { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }

        [DataMember(Name = "nextLink")]
        public string NextLink { get; set; }
    }

    [DataContract]
    public class Avatar
    {
        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }
    }

    [DataContract]
    public class DescriptionNpf
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "text", EmitDefaultValue = false)]
        public string Text { get; set; }

        [DataMember(Name = "formatting", EmitDefaultValue = false)]
        public List<Formatting> Formatting { get; set; }
    }

    [DataContract]
    public class Blog
    {
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "avatar", EmitDefaultValue = false)]
        public IList<Avatar> Avatar { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "is_adult", EmitDefaultValue = false)]
        public bool IsAdult { get; set; }

        [DataMember(Name = "description_npf", EmitDefaultValue = false)]
        public IList<DescriptionNpf> DescriptionNpf { get; set; }

        [DataMember(Name = "uuid", EmitDefaultValue = false)]
        public string Uuid { get; set; }

        [DataMember(Name = "can_be_followed", EmitDefaultValue = false)]
        public bool CanBeFollowed { get; set; }
    }

    [DataContract]
    public class Colors
    {
        [DataMember(Name = "c0", EmitDefaultValue = false)]
        public string c0 { get; set; }

        [DataMember(Name = "c1", EmitDefaultValue = false)]
        public string c1 { get; set; }
    }

    [DataContract]
    public class Medium
    {
        [DataMember(Name = "media_key", EmitDefaultValue = false)]
        public string MediaKey { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "colors", EmitDefaultValue = false)]
        public Colors Colors { get; set; }

        [DataMember(Name = "cropped", EmitDefaultValue = false)]
        public bool? Cropped { get; set; }

        [DataMember(Name = "has_original_dimensions", EmitDefaultValue = false)]
        public bool? HasOriginalDimensions { get; set; }
    }

    [DataContract]
    public class Poster
    {
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }
    }

    [DataContract]
    public class EmbedIframe
    {
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "width", EmitDefaultValue = false)]
        public int Width { get; set; }

        [DataMember(Name = "height", EmitDefaultValue = false)]
        public int Height { get; set; }
    }

    [DataContract]
    public class Metadata
    {
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }
    }

    [DataContract]
    public class Attribution
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "app_name", EmitDefaultValue = false)]
        public string AppName { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "display_text", EmitDefaultValue = false)]
        public string DisplayText { get; set; }
    }

    [DataContract]
    public class Content
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "media", EmitDefaultValue = false)]
        [JsonConverter(typeof(SingleOrArrayConverter<Medium>))]
        public IList<Medium> Media { get; set; }

        [DataMember(Name = "colors", EmitDefaultValue = false)]
        public Colors Colors { get; set; }

        [DataMember(Name = "text", EmitDefaultValue = false)]
        public string Text { get; set; }

        [DataMember(Name = "provider", EmitDefaultValue = false)]
        public string Provider { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "embed_html", EmitDefaultValue = false)]
        public string EmbedHtml { get; set; }

        [DataMember(Name = "poster", EmitDefaultValue = false)]
        public IList<Poster> Poster { get; set; }

        [DataMember(Name = "embed_iframe", EmitDefaultValue = false)]
        public EmbedIframe EmbedIframe { get; set; }

        [DataMember(Name = "metadata", EmitDefaultValue = false)]
        public Metadata Metadata { get; set; }

        [DataMember(Name = "attribution", EmitDefaultValue = false)]
        [JsonConverter(typeof(SingleOrArrayConverter<Attribution>))]
        public Attribution Attribution { get; set; }
    }

    [DataContract]
    public class Posts
    {
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public IList<Datum> Data { get; set; }

        [DataMember(Name = "_links")]
        public Links Links { get; set; }
    }

    [DataContract]
    public class Response
    {
        [DataMember(Name = "timeline", EmitDefaultValue = false)]
        public Timeline2 Timeline { get; set; }
    }

    [DataContract]
    public class ViewportMonitor
    {
        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }
    }

    [DataContract]
    public class DisplayText
    {
        [DataMember(Name = "install")]
        public string Install { get; set; }

        [DataMember(Name = "open")]
        public string Open { get; set; }

        [DataMember(Name = "madeWith")]
        public string MadeWith { get; set; }
    }

    [DataContract]
    public class AppStoreIds
    {
        [DataMember(Name = "ios")]
        public string Ios { get; set; }

        [DataMember(Name = "android")]
        public string Android { get; set; }
    }

    [DataContract]
    public class SourceAttribution
    {
        [DataMember(Name = "displayText")]
        public DisplayText DisplayText { get; set; }

        [DataMember(Name = "icon")]
        public string Icon { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "appStoreUrl")]
        public string AppStoreUrl { get; set; }

        [DataMember(Name = "playStoreUrl")]
        public string PlayStoreUrl { get; set; }

        [DataMember(Name = "syndicationId")]
        public object SyndicationId { get; set; }

        [DataMember(Name = "deepLinks")]
        public IList<object> DeepLinks { get; set; }

        [DataMember(Name = "appStoreIds")]
        public AppStoreIds AppStoreIds { get; set; }
    }

    [DataContract]
    public class Exif
    {
        [DataMember(Name = "Time")]
        public object Time { get; set; }

        [DataMember(Name = "CameraMake")]
        public string CameraMake { get; set; }

        [DataMember(Name = "CameraModel")]
        public string CameraModel { get; set; }
    }

    [DataContract]
    public class Display
    {
        [DataMember(Name = "blocks")]
        public IList<int> Blocks { get; set; }
    }

    [DataContract]
    public class Layout
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "display")]
        public IList<Display> Display { get; set; }
    }

    [DataContract]
    public class PostAuthorAvatar
    {

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }

    [DataContract]
    public class TaggedPost
    {
        [DataMember(Name = "objectType")]
        public string ObjectType { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "originalType")]
        public string OriginalType { get; set; }

        [DataMember(Name = "blogName")]
        public string BlogName { get; set; }

        [DataMember(Name = "blog")]
        public Blog Blog { get; set; }

        [DataMember(Name = "isNsfw")]
        public bool IsNsfw { get; set; }

        [DataMember(Name = "classification")]
        public string Classification { get; set; }

        [DataMember(Name = "nsfwScore")]
        public int NsfwScore { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "idString")]
        public string IdString { get; set; }

        [DataMember(Name = "postUrl")]
        public string PostUrl { get; set; }

        [DataMember(Name = "slug")]
        public string Slug { get; set; }

        [DataMember(Name = "date")]
        public string Date { get; set; }

        [DataMember(Name = "timestamp")]
        public int Timestamp { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "reblogKey")]
        public string ReblogKey { get; set; }

        [DataMember(Name = "tags")]
        public IList<string> Tags { get; set; }

        [DataMember(Name = "shortUrl")]
        public string ShortUrl { get; set; }

        [DataMember(Name = "summary")]
        public string Summary { get; set; }

        [DataMember(Name = "shouldOpenInLegacy")]
        public bool ShouldOpenInLegacy { get; set; }

        [DataMember(Name = "recommendedSource")]
        public object RecommendedSource { get; set; }

        [DataMember(Name = "recommendedColor")]
        public object RecommendedColor { get; set; }

        [DataMember(Name = "followed")]
        public bool Followed { get; set; }

        [DataMember(Name = "sourceAttribution")]
        public SourceAttribution SourceAttribution { get; set; }

        [DataMember(Name = "liked")]
        public bool Liked { get; set; }

        [DataMember(Name = "noteCount")]
        public int NoteCount { get; set; }

        [DataMember(Name = "content")]
        public IList<Content> Content { get; set; }

        [DataMember(Name = "layout")]
        public IList<Layout> Layout { get; set; }

        [DataMember(Name = "trail")]
        public IList<object> Trail { get; set; }

        [DataMember(Name = "placementId")]
        public string PlacementId { get; set; }

        [DataMember(Name = "canEdit")]
        public bool CanEdit { get; set; }

        [DataMember(Name = "canDelete")]
        public bool CanDelete { get; set; }

        [DataMember(Name = "canLike")]
        public bool CanLike { get; set; }

        [DataMember(Name = "canReblog")]
        public bool CanReblog { get; set; }

        [DataMember(Name = "canSendInMessage")]
        public bool CanSendInMessage { get; set; }

        [DataMember(Name = "canReply")]
        public bool CanReply { get; set; }

        [DataMember(Name = "displayAvatar")]
        public bool DisplayAvatar { get; set; }

        [DataMember(Name = "postAuthor")]
        public string PostAuthor { get; set; }

        [DataMember(Name = "postAuthorAvatar")]
        public IList<PostAuthorAvatar> PostAuthorAvatar { get; set; }

        [DataMember(Name = "sourceUrl")]
        public string SourceUrl { get; set; }

        [DataMember(Name = "sourceTitle")]
        public string SourceTitle { get; set; }

        [DataMember(Name = "sourceUrlRaw")]
        public string SourceUrlRaw { get; set; }
    }

    [DataContract]
    public class Datum
    {
        [DataMember(Name = "object_type", EmitDefaultValue = false)]
        public string ObjectType { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "original_type", EmitDefaultValue = false)]
        public string OriginalType { get; set; }

        [DataMember(Name = "blog_name", EmitDefaultValue = false)]
        public string BlogName { get; set; }

        [DataMember(Name = "blog", EmitDefaultValue = false)]
        public Blog blog { get; set; }

        [DataMember(Name = "is_nsfw", EmitDefaultValue = false)]
        public bool IsNsfw { get; set; }

        [DataMember(Name = "classification", EmitDefaultValue = false)]
        public string Classification { get; set; }

        [DataMember(Name = "nsfw_score", EmitDefaultValue = false)]
        public int NsfwScore { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "id_string", EmitDefaultValue = false)]
        public string IdString { get; set; }

        [DataMember(Name = "post_url", EmitDefaultValue = false)]
        public string PostUrl { get; set; }

        [DataMember(Name = "slug", EmitDefaultValue = false)]
        public string Slug { get; set; }

        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public int Timestamp { get; set; }

        [DataMember(Name = "state", EmitDefaultValue = false)]
        public string State { get; set; }

        [DataMember(Name = "reblog_key", EmitDefaultValue = false)]
        public string ReblogKey { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public IList<string> Tags { get; set; }

        [DataMember(Name = "short_url", EmitDefaultValue = false)]
        public string ShortUrl { get; set; }

        [DataMember(Name = "summary", EmitDefaultValue = false)]
        public string Summary { get; set; }

        [DataMember(Name = "should_open_in_legacy", EmitDefaultValue = false)]
        public bool ShouldOpenInLegacy { get; set; }

        [DataMember(Name = "recommended_source", EmitDefaultValue = false)]
        public object RecommendedSource { get; set; }

        [DataMember(Name = "recommended_color", EmitDefaultValue = false)]
        public object RecommendedColor { get; set; }

        [DataMember(Name = "note_count", EmitDefaultValue = false)]
        public int NoteCount { get; set; }

        [DataMember(Name = "content", EmitDefaultValue = false)]
        public IList<Content> Content { get; set; }

        [DataMember(Name = "layout", EmitDefaultValue = false)]
        public IList<object> Layout { get; set; }

        [DataMember(Name = "trail", EmitDefaultValue = false)]
        public IList<object> Trail { get; set; }

        [DataMember(Name = "can_edit", EmitDefaultValue = false)]
        public bool CanEdit { get; set; }

        [DataMember(Name = "can_delete", EmitDefaultValue = false)]
        public bool CanDelete { get; set; }

        [DataMember(Name = "can_like", EmitDefaultValue = false)]
        public bool CanLike { get; set; }

        [DataMember(Name = "can_reblog", EmitDefaultValue = false)]
        public bool CanReblog { get; set; }

        [DataMember(Name = "can_send_in_message", EmitDefaultValue = false)]
        public bool CanSendInMessage { get; set; }

        [DataMember(Name = "can_reply", EmitDefaultValue = false)]
        public bool CanReply { get; set; }

        [DataMember(Name = "display_avatar", EmitDefaultValue = false)]
        public bool DisplayAvatar { get; set; }
    }

    [DataContract]
    public class SidebarPost
    {
        [DataMember(Name = "objectType", EmitDefaultValue = false)]
        public string ObjectType { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "originalType", EmitDefaultValue = false)]
        public string OriginalType { get; set; }

        [DataMember(Name = "blogName", EmitDefaultValue = false)]
        public string BlogName { get; set; }

        [DataMember(Name = "blog", EmitDefaultValue = false)]
        public Blog blog { get; set; }

        [DataMember(Name = "isNsfw", EmitDefaultValue = false)]
        public bool IsNsfw { get; set; }

        [DataMember(Name = "classification", EmitDefaultValue = false)]
        public string Classification { get; set; }

        [DataMember(Name = "nsfwScore", EmitDefaultValue = false)]
        public int NsfwScore { get; set; }

        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "idString", EmitDefaultValue = false)]
        public string IdString { get; set; }

        [DataMember(Name = "postUrl", EmitDefaultValue = false)]
        public string PostUrl { get; set; }

        [DataMember(Name = "slug", EmitDefaultValue = false)]
        public string Slug { get; set; }

        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public int Timestamp { get; set; }

        [DataMember(Name = "state", EmitDefaultValue = false)]
        public string State { get; set; }

        [DataMember(Name = "reblogKey", EmitDefaultValue = false)]
        public string ReblogKey { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public IList<string> Tags { get; set; }

        [DataMember(Name = "shortUrl", EmitDefaultValue = false)]
        public string ShortUrl { get; set; }

        [DataMember(Name = "summary", EmitDefaultValue = false)]
        public string Summary { get; set; }

        [DataMember(Name = "shouldOpenInLegacy", EmitDefaultValue = false)]
        public bool ShouldOpenInLegacy { get; set; }

        [DataMember(Name = "recommendedSource", EmitDefaultValue = false)]
        public object RecommendedSource { get; set; }

        [DataMember(Name = "recommendedColor", EmitDefaultValue = false)]
        public object RecommendedColor { get; set; }

        [DataMember(Name = "noteCount", EmitDefaultValue = false)]
        public int NoteCount { get; set; }

        [DataMember(Name = "likeCount", EmitDefaultValue = false)]
        public int LikeCount { get; set; }

        [DataMember(Name = "reblogCount", EmitDefaultValue = false)]
        public int ReblogCount { get; set; }

        [DataMember(Name = "replyCount", EmitDefaultValue = false)]
        public int ReplyCount { get; set; }

        [DataMember(Name = "content", EmitDefaultValue = false)]
        public IList<Content> Content { get; set; }

        [DataMember(Name = "layout", EmitDefaultValue = false)]
        public IList<object> Layout { get; set; }

        [DataMember(Name = "trail", EmitDefaultValue = false)]
        public IList<object> Trail { get; set; }

        [DataMember(Name = "canEdit", EmitDefaultValue = false)]
        public bool CanEdit { get; set; }

        [DataMember(Name = "canDelete", EmitDefaultValue = false)]
        public bool CanDelete { get; set; }

        [DataMember(Name = "canReply", EmitDefaultValue = false)]
        public bool CanReply { get; set; }

        [DataMember(Name = "canLike", EmitDefaultValue = false)]
        public bool CanLike { get; set; }

        [DataMember(Name = "interactabilityReblog", EmitDefaultValue = false)]
        public string InteractabilityReblog { get; set; }

        [DataMember(Name = "canReblog", EmitDefaultValue = false)]
        public bool CanReblog { get; set; }

        [DataMember(Name = "canSendInMessage", EmitDefaultValue = false)]
        public bool CanSendInMessage { get; set; }

        [DataMember(Name = "embedUrl", EmitDefaultValue = false)]
        public string EmbedUrl { get; set; }

        [DataMember(Name = "displayAvatar", EmitDefaultValue = false)]
        public bool DisplayAvatar { get; set; }
    }

    [DataContract]
    public class Fields
    {
        [DataMember(Name = "blogs")]
        public string Blogs { get; set; }
    }

    [DataContract]
    public class QueryParams
    {
        [DataMember(Name = "fields")]
        public Fields Fields { get; set; }

        [DataMember(Name = "reblogInfo")]
        public string ReblogInfo { get; set; }

        [DataMember(Name = "mode")]
        public string Mode { get; set; }

        [DataMember(Name = "query")]
        public string Query { get; set; }

        [DataMember(Name = "limit")]
        public string Limit { get; set; }

        [DataMember(Name = "blogLimit")]
        public string BlogLimit { get; set; }

        [DataMember(Name = "postOffset")]
        public string PostOffset { get; set; }

        [DataMember(Name = "postLimit")]
        public string PostLimit { get; set; }
    }

    [DataContract]
    public class NextLink
    {
        [DataMember(Name = "href")]
        public string Href { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "queryParams")]
        public QueryParams QueryParams { get; set; }
    }

    [DataContract]
    public class Header
    {
        [DataMember(Name = "hubName")]
        public string HubName { get; set; }

        [DataMember(Name = "tagId")]
        public string TagId { get; set; }

        [DataMember(Name = "backgroundColor")]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "headerImage")]
        public string HeaderImage { get; set; }

        [DataMember(Name = "headerImageWidth")]
        public int? HeaderImageWidth { get; set; }

        [DataMember(Name = "headerImageHeight")]
        public int? HeaderImageHeight { get; set; }

        [DataMember(Name = "headerLink")]
        public string HeaderLink { get; set; }

        [DataMember(Name = "postId")]
        public string PostId { get; set; }

        [DataMember(Name = "blog")]
        public string Blog { get; set; }

        [DataMember(Name = "blogAvatarUrl")]
        public string BlogAvatarUrl { get; set; }

        [DataMember(Name = "blogName")]
        public string BlogName { get; set; }

        [DataMember(Name = "isFollowed")]
        public bool IsFollowed { get; set; }

        [DataMember(Name = "isTrending")]
        public bool IsTrending { get; set; }

        [DataMember(Name = "followersCount")]
        public string FollowersCount { get; set; }

        [DataMember(Name = "followersCountInt")]
        public int FollowersCountInt { get; set; }

        [DataMember(Name = "showFollowersCount")]
        public bool ShowFollowersCount { get; set; }

        [DataMember(Name = "newPostsCount")]
        public string NewPostsCount { get; set; }

        [DataMember(Name = "newPostsCountInt")]
        public int NewPostsCountInt { get; set; }

        [DataMember(Name = "showNewPostsCount")]
        public bool ShowNewPostsCount { get; set; }
    }

    [DataContract]
    public class Tagged
    {
        [DataMember(Name = "header", EmitDefaultValue = false)]
        public Header Header { get; set; }

        [DataMember(Name = "emptyResultsTextSeed", EmitDefaultValue = false)]
        public string EmptyResultsTextSeed { get; set; }

        [DataMember(Name = "relatedTags", EmitDefaultValue = false)]
        public List<string> RelatedTags { get; set; }

        [DataMember(Name = "timeline", EmitDefaultValue = false)]
        public Timeline Timeline { get; set; }

        [DataMember(Name = "sidebarTimelineObject", EmitDefaultValue = false)]
        public SidebarTimelineObject SidebarTimelineObject { get; set; }

        [DataMember(Name = "shouldRedirect", EmitDefaultValue = false)]
        public bool ShouldRedirect { get; set; }
    }

    [DataContract]
    public class SidebarTimelineObject
    {
        [DataMember(Name = "objectType")]
        public string ObjectType { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "serveId")]
        public string ServeId { get; set; }

        [DataMember(Name = "display")]
        public Display Display { get; set; }

        [DataMember(Name = "recommendationReason")]
        public object RecommendationReason { get; set; }

        [DataMember(Name = "dismissal")]
        public object Dismissal { get; set; }

        [DataMember(Name = "links")]
        public Links Links { get; set; }

        [DataMember(Name = "elements")]
        public List<Element> Elements { get; set; }

        [DataMember(Name = "hero")]
        public bool Hero { get; set; }

        [DataMember(Name = "autoAdvance")]
        public bool AutoAdvance { get; set; }

        [DataMember(Name = "isGo90Carousel")]
        public bool IsGo90Carousel { get; set; }

        [DataMember(Name = "blogCardDisplayBucket")]
        public bool BlogCardDisplayBucket { get; set; }
    }

    [DataContract]
    public class BodyParams
    {
        [DataMember(Name = "tumblelog")]
        public string Tumblelog { get; set; }
    }

    [DataContract]
    public class Link
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "href")]
        public string Href { get; set; }

        [DataMember(Name = "bodyParams")]
        public BodyParams BodyParams { get; set; }
    }

    [DataContract]
    public class OptionType
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "destructive")]
        public bool Destructive { get; set; }

        [DataMember(Name = "link")]
        public Link Link { get; set; }
    }

    [DataContract]
    public class Dismissal
    {
        [DataMember(Name = "options")]
        public List<OptionType> Options { get; set; }
    }

    [DataContract]
    public class Element
    {
        [DataMember(Name = "objectType")]
        public string ObjectType { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "serveId")]
        public string ServeId { get; set; }

        [DataMember(Name = "display")]
        public Display Display { get; set; }

        [DataMember(Name = "recommendationReason")]
        public object RecommendationReason { get; set; }

        [DataMember(Name = "dismissal")]
        public Dismissal Dismissal { get; set; }

        [DataMember(Name = "resources")]
        public List<Resource> Resources { get; set; }

        [DataMember(Name = "resourceIds")]
        public List<string> ResourceIds { get; set; }

        [DataMember(Name = "parentServeId")]
        public string ParentServeId { get; set; }
    }

    [DataContract]
    public class CheckoutLabels
    {
        [DataMember(Name = "monthly")]
        public string Monthly { get; set; }

        [DataMember(Name = "yearly")]
        public string Yearly { get; set; }

        [DataMember(Name = "support")]
        public string Support { get; set; }
    }

    [DataContract]
    public class SubscriptionPlan
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "currencyCode")]
        public string CurrencyCode { get; set; }

        [DataMember(Name = "monthlyPrice")]
        public int MonthlyPrice { get; set; }

        [DataMember(Name = "yearlyPrice")]
        public object YearlyPrice { get; set; }

        [DataMember(Name = "memberPerks")]
        public List<string> MemberPerks { get; set; }

        [DataMember(Name = "subscriptionLabel")]
        public string SubscriptionLabel { get; set; }

        [DataMember(Name = "checkoutLabels")]
        public CheckoutLabels CheckoutLabels { get; set; }

        [DataMember(Name = "isValid")]
        public bool IsValid { get; set; }
    }

    [DataContract]
    public class Theme
    {
        [DataMember(Name = "headerFullWidth")]
        public int HeaderFullWidth { get; set; }

        [DataMember(Name = "headerFullHeight")]
        public int HeaderFullHeight { get; set; }

        [DataMember(Name = "headerFocusWidth")]
        public int HeaderFocusWidth { get; set; }

        [DataMember(Name = "headerFocusHeight")]
        public int HeaderFocusHeight { get; set; }

        [DataMember(Name = "avatarShape")]
        public string AvatarShape { get; set; }

        [DataMember(Name = "backgroundColor")]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "bodyFont")]
        public string BodyFont { get; set; }

        [DataMember(Name = "headerBounds")]
        public string HeaderBounds { get; set; }

        [DataMember(Name = "headerImage")]
        public string HeaderImage { get; set; }

        [DataMember(Name = "headerImageFocused")]
        public string HeaderImageFocused { get; set; }

        [DataMember(Name = "headerImagePoster")]
        public string HeaderImagePoster { get; set; }

        [DataMember(Name = "headerImageScaled")]
        public string HeaderImageScaled { get; set; }

        [DataMember(Name = "headerStretch")]
        public bool HeaderStretch { get; set; }

        [DataMember(Name = "linkColor")]
        public string LinkColor { get; set; }

        [DataMember(Name = "showAvatar")]
        public bool ShowAvatar { get; set; }

        [DataMember(Name = "showDescription")]
        public bool ShowDescription { get; set; }

        [DataMember(Name = "showHeaderImage")]
        public bool ShowHeaderImage { get; set; }

        [DataMember(Name = "showTitle")]
        public bool ShowTitle { get; set; }

        [DataMember(Name = "titleColor")]
        public string TitleColor { get; set; }

        [DataMember(Name = "titleFont")]
        public string TitleFont { get; set; }

        [DataMember(Name = "titleFontWeight")]
        public string TitleFontWeight { get; set; }
    }

    [DataContract]
    public class Resource
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "avatar")]
        public List<Avatar> Avatar { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "isAdult")]
        public bool IsAdult { get; set; }

        [DataMember(Name = "isMember")]
        public bool IsMember { get; set; }

        [DataMember(Name = "descriptionNpf")]
        public List<DescriptionNpf> DescriptionNpf { get; set; }

        [DataMember(Name = "uuid")]
        public string Uuid { get; set; }

        [DataMember(Name = "canBeFollowed")]
        public bool CanBeFollowed { get; set; }

        [DataMember(Name = "followed")]
        public bool Followed { get; set; }

        [DataMember(Name = "theme")]
        public Theme Theme { get; set; }

        [DataMember(Name = "isPaywallOn")]
        public bool IsPaywallOn { get; set; }

        [DataMember(Name = "paywallAccess")]
        public string PaywallAccess { get; set; }

        [DataMember(Name = "subscriptionPlan")]
        public SubscriptionPlan SubscriptionPlan { get; set; }

        [DataMember(Name = "shareLikes")]
        public bool ShareLikes { get; set; }

        [DataMember(Name = "shareFollowing")]
        public bool ShareFollowing { get; set; }

        [DataMember(Name = "canSubscribe")]
        public bool CanSubscribe { get; set; }

        [DataMember(Name = "subscribed")]
        public bool Subscribed { get; set; }

        [DataMember(Name = "ask")]
        public bool Ask { get; set; }

        [DataMember(Name = "isBlockedFromPrimary")]
        public bool IsBlockedFromPrimary { get; set; }

        [DataMember(Name = "posts")]
        public List<SidebarPost> Posts { get; set; }

        [DataMember(Name = "canSubmit")]
        public bool? CanSubmit { get; set; }
    }

    [DataContract]
    public class Timeline
    {
        [DataMember(Name = "elements")]
        public IList<TaggedPost> Elements { get; set; }

        [DataMember(Name = "links")]
        public Links Links { get; set; }
    }

    [DataContract]
    public class Timeline2
    {
        [DataMember(Name = "elements")]
        public IList<Datum> Elements { get; set; }

        [DataMember(Name = "_links")]
        public Links Links { get; set; }
    }

    [DataContract]
    public class ApiFetchStore
    {
        [DataMember(Name = "API_TOKEN")]
        public string APITOKEN { get; set; }

        [DataMember(Name = "csrfToken")]
        public string CsrfToken { get; set; }
    }

    [DataContract]
    public class Data
    {
    }

    [DataContract]
    public class LanguageData
    {
        [DataMember(Name = "code")]
        public string Code { get; set; }

        [DataMember(Name = "data")]
        public Data Data { get; set; }
    }

    [DataContract]
    public class ReportingInfo
    {
        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "token")]
        public string Token { get; set; }
    }

    [DataContract]
    public class Ads
    {
        [DataMember(Name = "hashedUserId")]
        public string HashedUserId { get; set; }
    }

    [DataContract]
    public class Automattic
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
    }

    [DataContract]
    public class ClientDetails
    {
        [DataMember(Name = "platform")]
        public string Platform { get; set; }

        [DataMember(Name = "os_name")]
        public string OsName { get; set; }

        [DataMember(Name = "os_version")]
        public string OsVersion { get; set; }

        [DataMember(Name = "language")]
        public string Language { get; set; }

        [DataMember(Name = "build_version")]
        public string BuildVersion { get; set; }

        [DataMember(Name = "form_factor")]
        public string FormFactor { get; set; }

        [DataMember(Name = "model")]
        public string Model { get; set; }

        [DataMember(Name = "connection")]
        public string Connection { get; set; }

        [DataMember(Name = "carrier")]
        public string Carrier { get; set; }

        [DataMember(Name = "browser_name")]
        public string BrowserName { get; set; }

        [DataMember(Name = "browser_version")]
        public string BrowserVersion { get; set; }
    }

    [DataContract]
    public class ConfigRef
    {
        [DataMember(Name = "flags")]
        public string Flags { get; set; }
    }

    [DataContract]
    public class Kraken
    {
        [DataMember(Name = "basePage")]
        public string BasePage { get; set; }

        [DataMember(Name = "routeSet")]
        public string RouteSet { get; set; }

        [DataMember(Name = "krakenHost")]
        public string KrakenHost { get; set; }

        [DataMember(Name = "sessionId")]
        public string SessionId { get; set; }

        [DataMember(Name = "clientDetails")]
        public ClientDetails ClientDetails { get; set; }

        [DataMember(Name = "configRef")]
        public ConfigRef ConfigRef { get; set; }
    }

    [DataContract]
    public class AnalyticsInfo
    {
        [DataMember(Name = "ads")]
        public Ads Ads { get; set; }

        [DataMember(Name = "automattic")]
        public Automattic Automattic { get; set; }

        [DataMember(Name = "kraken")]
        public Kraken Kraken { get; set; }
    }

    [DataContract]
    public class TeadsHydraSource
    {
        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class TeadsTestHydraSource
    {

        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class FlurryHydraSource
    {

        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class OneMobileHydraSource
    {
        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class IponwebMrecHydraSource
    {
        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class TeadsDashboardTop
    {
        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class TeadsDashboard
    {

        [DataMember(Name = "adSource")]
        public string AdSource { get; set; }

        [DataMember(Name = "adPlacementId")]
        public string AdPlacementId { get; set; }

        [DataMember(Name = "maxAdCount")]
        public int MaxAdCount { get; set; }

        [DataMember(Name = "maxAdLoadingCount")]
        public int MaxAdLoadingCount { get; set; }

        [DataMember(Name = "expireTime")]
        public int ExpireTime { get; set; }

        [DataMember(Name = "timeBetweenSuccessfulRequests")]
        public int TimeBetweenSuccessfulRequests { get; set; }

        [DataMember(Name = "loadingStrategy")]
        public int LoadingStrategy { get; set; }
    }

    [DataContract]
    public class Placements
    {
        [DataMember(Name = "teadsHydraSource")]
        public TeadsHydraSource TeadsHydraSource { get; set; }

        [DataMember(Name = "teadsTestHydraSource")]
        public TeadsTestHydraSource TeadsTestHydraSource { get; set; }

        [DataMember(Name = "flurryHydraSource")]
        public FlurryHydraSource FlurryHydraSource { get; set; }

        [DataMember(Name = "oneMobileHydraSource")]
        public OneMobileHydraSource OneMobileHydraSource { get; set; }

        [DataMember(Name = "iponwebMrecHydraSource")]
        public IponwebMrecHydraSource IponwebMrecHydraSource { get; set; }

        [DataMember(Name = "teadsDashboardTop")]
        public TeadsDashboardTop TeadsDashboardTop { get; set; }

        [DataMember(Name = "teadsDashboard")]
        public TeadsDashboard TeadsDashboard { get; set; }
    }

    [DataContract]
    public class AdPlacementConfiguration
    {
        [DataMember(Name = "signature")]
        public string Signature { get; set; }

        [DataMember(Name = "placements")]
        public Placements Placements { get; set; }
    }

    [DataContract]
    public class Privacy
    {
        [DataMember(Name = "ccpaPrivacyString")]
        public string CcpaPrivacyString { get; set; }
    }

    [DataContract]
    public class IsLoggedIn
    {
        [DataMember(Name = "isLoggedIn")]
        public bool isLoggedIn { get; set; }
    }

    [DataContract]
    public class RecaptchaV3PublicKey
    {
        [DataMember(Name = "value")]
        public string Value { get; set; }
    }

    [DataContract]
    public class Browser
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "major")]
        public string Major { get; set; }
    }

    [DataContract]
    public class Engine
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }

    [DataContract]
    public class Os
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "version")]
        public string Version { get; set; }
    }

    [DataContract]
    public class Device
    {
    }

    [DataContract]
    public class Cpu
    {
        [DataMember(Name = "architecture")]
        public string Architecture { get; set; }
    }

    [DataContract]
    public class UserAgent
    {
        [DataMember(Name = "ua")]
        public string Ua { get; set; }

        [DataMember(Name = "browser")]
        public Browser Browser { get; set; }

        [DataMember(Name = "engine")]
        public Engine Engine { get; set; }

        [DataMember(Name = "os")]
        public Os Os { get; set; }

        [DataMember(Name = "device")]
        public Device Device { get; set; }

        [DataMember(Name = "cpu")]
        public Cpu Cpu { get; set; }
    }

    [DataContract]
    public class BrowserInfo
    {
        [DataMember(Name = "userAgent")]
        public UserAgent UserAgent { get; set; }

        [DataMember(Name = "deviceType")]
        public string DeviceType { get; set; }

        [DataMember(Name = "isSupported")]
        public bool IsSupported { get; set; }
    }

    [DataContract]
    public class SessionInfo
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }
    }

    [DataContract]
    public class Formatting
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "start")]
        public int Start { get; set; }

        [DataMember(Name = "end")]
        public int End { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }

    [DataContract]
    public class Urls
    {
        [DataMember(Name = "web")]
        public string Web { get; set; }

        [DataMember(Name = "ios")]
        public string Ios { get; set; }

        [DataMember(Name = "android")]
        public string Android { get; set; }
    }

    [DataContract]
    public class Action
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "backgroundColor")]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "price")]
        public string Price { get; set; }

        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "textColor")]
        public string TextColor { get; set; }

        [DataMember(Name = "urls")]
        public Urls Urls { get; set; }
    }

    [DataContract]
    public class NextRequest
    {
        [DataMember(Name = "href")]
        public string Href { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; }

        [DataMember(Name = "queryParams")]
        public QueryParams QueryParams { get; set; }
    }

    [DataContract]
    public class Links
    {
        [DataMember(Name = "next")]
        public NextRequest Next { get; set; }
    }
}
