using System.Collections.Generic;
using Newtonsoft.Json;
using TumblThree.Applications.Converter;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    public class Action2
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }

    public class Attribution
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("blog")]
        public Blog Blog { get; set; }
    }

    public class Avatar
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("accessories")]
        public List<object> Accessories { get; private set; } = new List<object>();
    }

    public class Badge
    {
        [JsonProperty("productGroup")]
        public string ProductGroup { get; set; }

        [JsonProperty("urls")]
        public List<string> Urls { get; private set; } = new List<string>();

        [JsonProperty("destinationUrl")]
        public string DestinationUrl { get; set; }
    }

    public class Blog
    {
        [JsonProperty("avatar")]
        public List<Avatar> Avatar { get; private set; } = new List<Avatar>();

        [JsonProperty("blogViewUrl")]
        public string BlogViewUrl { get; set; }

        //[JsonProperty("blog_view_url")]
        //private string BlogViewUrl_ {  set { BlogViewUrl = value; } }

        [JsonProperty("canBeFollowed")]
        public bool CanBeFollowed { get; set; }

        [JsonProperty("canShowBadges")]
        public bool CanShowBadges { get; set; }

        [JsonProperty("descriptionNpf")]
        public List<DescriptionNpf> DescriptionNpf { get; private set; } = new List<DescriptionNpf>();

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("isAdult")]
        public bool IsAdult { get; set; }

        [JsonProperty("isMember")]
        public bool IsMember { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("theme")]
        public Theme Theme { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("tumblrmartAccessories")]
        public TumblrmartAccessories TumblrmartAccessories { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("ask")]
        public bool Ask { get; set; }

        [JsonProperty("canSubmit")]
        public bool CanSubmit { get; set; }

        [JsonProperty("canSubscribe")]
        public bool CanSubscribe { get; set; }

        [JsonProperty("isBlockedFromPrimary")]
        public bool IsBlockedFromPrimary { get; set; }

        [JsonProperty("isPasswordProtected")]
        public bool IsPasswordProtected { get; set; }

        [JsonProperty("shareFollowing")]
        public bool ShareFollowing { get; set; }

        [JsonProperty("shareLikes")]
        public bool ShareLikes { get; set; }

        [JsonProperty("subscribed")]
        public bool Subscribed { get; set; }

        [JsonProperty("canMessage")]
        public bool CanMessage { get; set; }

        [JsonProperty("askPageTitle")]
        public string AskPageTitle { get; set; }

        [JsonProperty("topTags")]
        public List<TopTag> TopTags { get; private set; } = new List<TopTag>();

        [JsonProperty("allowSearchIndexing")]
        public bool AllowSearchIndexing { get; set; }

        [JsonProperty("isHiddenFromBlogNetwork")]
        public bool IsHiddenFromBlogNetwork { get; set; }

        [JsonProperty("shouldShowGift")]
        public bool ShouldShowGift { get; set; }

        [JsonProperty("shouldShowTumblrmartGift")]
        public bool ShouldShowTumblrmartGift { get; set; }

        [JsonProperty("active", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Active { get; set; }

        [JsonProperty("showFollowAction", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ShowFollowAction { get; set; }
    }

    public class ClientSideAd
    {
        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("clientSideAdType")]
        public string ClientSideAdType { get; set; }

        [JsonProperty("mediationCandidateId")]
        public string MediationCandidateId { get; set; }

        [JsonProperty("sponsoredBadgeUrl")]
        public string SponsoredBadgeUrl { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayType")]
        public int DisplayType { get; set; }

        [JsonProperty("adSourceTag")]
        public string AdSourceTag { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("placementId")]
        public string PlacementId { get; set; }
    }

    public class CommunityLabels
    {
        [JsonProperty("hasCommunityLabel")]
        public bool HasCommunityLabel { get; set; }

        [JsonProperty("lastReporter")]
        public string LastReporter { get; set; }

        [JsonProperty("categories")]
        public List<object> Categories { get; private set; } = new List<object>();
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        #region "Text"

        [JsonProperty("subtype", NullValueHandling = NullValueHandling.Ignore)]
        public string Subtype { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("formatting", NullValueHandling = NullValueHandling.Ignore)]
        public List<Format> Formatting { get; private set; } = new List<Format>();

        #endregion

        #region "Image"

        [JsonProperty("media", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SingleOrArrayConverter<Medium>))]
        public List<Medium> Media { get; set; }

        [JsonProperty("alt_text", NullValueHandling = NullValueHandling.Ignore)]
        public string AltText { get; set; }

        [JsonProperty("caption", NullValueHandling = NullValueHandling.Ignore)]
        public string Caption { get; set; }

        [JsonProperty("attribution", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SingleOrArrayConverter<Attribution>))]
        public List<Attribution> Attribution { get; private set; } = new List<Attribution>();

        [JsonProperty("colors", NullValueHandling = NullValueHandling.Ignore)]
        public Colors Colors { get; set; }

        [JsonProperty("exif", NullValueHandling = NullValueHandling.Ignore)]
        public Exif Exif { get; set; }

        [JsonProperty("feedback_token", NullValueHandling = NullValueHandling.Ignore)]
        public string FeedbackToken { get; set; }

        #endregion

        #region "Link"

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public string Author { get; set; }

        [JsonProperty("poster", NullValueHandling = NullValueHandling.Ignore)]
        public List<Poster> Poster { get; set; }

        #endregion

        #region "Audio"

        [JsonProperty("provider", NullValueHandling = NullValueHandling.Ignore)]
        public string Provider { get; set; }

        //public string Title { get; set; }

        [JsonProperty("artist", NullValueHandling = NullValueHandling.Ignore)]
        public string Artist { get; set; }

        [JsonProperty("album", NullValueHandling = NullValueHandling.Ignore)]
        public string Album { get; set; }

        //public List<Medium> Media { get; private set; } = new List<Medium>();

        //public List<Poster> Poster { get; set; }

        //public string Url { get; set; }

        [JsonProperty("embed_html", NullValueHandling = NullValueHandling.Ignore)]
        public string EmbedHtml { get; set; }

        [JsonProperty("embed_url", NullValueHandling = NullValueHandling.Ignore)]
        public string EmbedUrl { get; set; }

        #endregion

        #region "Video"

        //public List<Medium> Media { get; private set; } = new List<Medium>();

        //public List<Poster> Poster { get; set; }

        [JsonProperty("filmstrip", NullValueHandling = NullValueHandling.Ignore)]
        public Filmstrip Filmstrip { get; set; }

        [JsonProperty("can_autoplay_on_cellular", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanAutoplayOnCellular { get; set; }

        //public string Provider { get; set; }
        //public string Url { get; set; }
        //public string EmbedHtml { get; set; }
        //public string EmbedUrl { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Metadata metadata { get; set; }

        //public List<Poster> Poster { get; set; }

        #endregion

        #region "Paywall"

        //public string Type { get; set; }

        #endregion
    }

    public class Context
    {
        [JsonProperty("isFirstPage")]
        public bool IsFirstPage { get; set; }

        [JsonProperty("iabCategories")]
        public List<string> IabCategories { get; private set; } = new List<string>();
    }

    public class DescriptionNpf
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        public List<Format> Formatting { get; private set; } = new List<Format>();
    }

    public class Display
    {
        [JsonProperty("blocks")]
        public List<int> Blocks { get; private set; } = new List<int>();
    }

    public class Exif
    {
        [JsonProperty("Time")]
        public int Time { get; set; }

        [JsonProperty("FocalLength")]
        public int FocalLength { get; set; }

        [JsonProperty("FocalLength35mmEquiv")]
        public int FocalLength35mmEquiv { get; set; }

        [JsonProperty("Aperture")]
        public double Aperture { get; set; }

        [JsonProperty("ExposureTime")]
        public double ExposureTime { get; set; }

        [JsonProperty("ISO")]
        public int ISO { get; set; }

        [JsonProperty("CameraMake")]
        public string CameraMake { get; set; }

        [JsonProperty("CameraModel")]
        public string CameraModel { get; set; }

        [JsonProperty("Lens")]
        public string Lens { get; set; }
    }

    public class Fields
    {
        [JsonProperty("blogs")]
        public string Blogs { get; set; }
    }

    public class Filmstrip
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class Format
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        // color
        [JsonProperty("hex", NullValueHandling = NullValueHandling.Ignore)]
        public string Hex { get; set; }

        // mention
        [JsonProperty("blog", NullValueHandling = NullValueHandling.Ignore)]
        public FormatBlog Blog { get; set; }

        // link
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }
    }

    public class FormatBlog
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }
    }

    public class HeaderCta
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("action")]
        public Action2 Action { get; set; }
    }

    public class Layout
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("blocks", NullValueHandling = NullValueHandling.Ignore)]
        public List<int> Blocks { get; set; }

        [JsonProperty("attribution", NullValueHandling = NullValueHandling.Ignore)]
        public Attribution attribution { get; set; }

        [JsonProperty("display", NullValueHandling = NullValueHandling.Ignore)]
        public List<Display> Display { get; set; }
    }

    public class Links
    {
        [JsonProperty("next", NullValueHandling = NullValueHandling.Ignore)]
        public Next1 Next { get; set; }
    }

    public class Medium
    {
        [JsonProperty("mediaKey", NullValueHandling = NullValueHandling.Ignore)]
        public string MediaKey { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("hasOriginalDimensions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HasOriginalDimensions { get; set; }

        [JsonProperty("cropped", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Cropped { get; set; }
    }

    public class Meta
    {
        [JsonProperty("status", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Status { get; set; }

        [JsonProperty("msg", NullValueHandling = NullValueHandling.Ignore)]
        public string Msg { get; set; }

        [JsonProperty("followTumblelogName")]
        public string FollowTumblelogName { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class Next1
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("queryParams")]
        public QueryParams QueryParams { get; set; }
    }

    public class Post
    {
        [JsonProperty("objectType")]
        public string ObjectType { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("originalType", NullValueHandling = NullValueHandling.Ignore)]
        public string OriginalType { get; set; }

        [JsonProperty("isBlocksPostFormat", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsBlocksPostFormat { get; set; }

        [JsonProperty("blogName", NullValueHandling = NullValueHandling.Ignore)]
        public string BlogName { get; set; }

        [JsonProperty("blog", NullValueHandling = NullValueHandling.Ignore)]
        public Blog Blog { get; set; }

        [JsonProperty("isNsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty("classification", NullValueHandling = NullValueHandling.Ignore)]
        public string Classification { get; set; }

        [JsonProperty("nsfwScore")]
        public int NsfwScore { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("idString", NullValueHandling = NullValueHandling.Ignore)]
        public string IdString { get; set; }

        [JsonProperty("isBlazed")]
        public bool IsBlazed { get; set; }

        [JsonProperty("isBlazePending")]
        public bool IsBlazePending { get; set; }

        [JsonProperty("canIgnite")]
        public bool CanIgnite { get; set; }

        [JsonProperty("canBlazeSingleUser", NullValueHandling = NullValueHandling.Ignore)]
        public bool? CanBlazeSingleUser { get; set; }

        [JsonProperty("canBlaze")]
        public bool CanBlaze { get; set; }

        [JsonProperty("postUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string PostUrl { get; set; }

        [JsonProperty("slug", NullValueHandling = NullValueHandling.Ignore)]
        public string Slug { get; set; }

        [JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)]
        public string Date { get; set; }

        [JsonProperty("timestamp", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Timestamp { get; set; }

        [JsonProperty("state", NullValueHandling = NullValueHandling.Ignore)]
        public string State { get; set; }

        [JsonProperty("reblogKey", NullValueHandling = NullValueHandling.Ignore)]
        public string ReblogKey { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("tagsV2", NullValueHandling = NullValueHandling.Ignore)]
        public List<TagsV2> TagsV2 { get; set; }

        [JsonProperty("shortUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ShortUrl { get; set; }

        [JsonProperty("summary", NullValueHandling = NullValueHandling.Ignore)]
        public string Summary { get; set; }

        [JsonProperty("shouldOpenInLegacy")]
        public bool ShouldOpenInLegacy { get; set; }

        [JsonProperty("recommendedSource")]
        public object RecommendedSource { get; set; }

        [JsonProperty("recommendedColor")]
        public object RecommendedColor { get; set; }

        [JsonProperty("headerContext", NullValueHandling = NullValueHandling.Ignore)]
        public HeaderContext HeaderContext { get; set; }

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("headerCta", NullValueHandling = NullValueHandling.Ignore)]
        public HeaderCta HeaderCta { get; set; }

        [JsonProperty("liked")]
        public bool Liked { get; set; }

        [JsonProperty("noteCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int NoteCount { get; set; }

        [JsonProperty("likeCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int LikeCount { get; set; }

        [JsonProperty("reblogCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ReblogCount { get; set; }

        [JsonProperty("replyCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ReplyCount { get; set; }

        [JsonProperty("isSubscribed")]
        public bool IsSubscribed { get; set; }

        [JsonProperty("canSubscribe", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanSubscribe { get; set; }

        [JsonProperty("askingName", NullValueHandling = NullValueHandling.Ignore)]
        public string AskingName { get; set; }

        [JsonProperty("askingUrl", NullValueHandling = NullValueHandling.Ignore)]
        public object AskingUrl { get; set; }

        [JsonProperty("askingAvatar", NullValueHandling = NullValueHandling.Ignore)]
        public List<Avatar> AskingAvatar { get; set; }

        [JsonProperty("askingIsAdult", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool AskingIsAdult { get; set; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public List<Content> Content { get; set; }

        [JsonProperty("layout", NullValueHandling = NullValueHandling.Ignore)]
        public List<Layout> Layout { get; set; }

        [JsonProperty("trail", NullValueHandling = NullValueHandling.Ignore)]
        public List<Trail> Trail { get; private set; } = new List<Trail>();

        [JsonProperty("canEdit")]
        public bool CanEdit { get; set; }

        [JsonProperty("canDelete")]
        public bool CanDelete { get; set; }

        [JsonProperty("canReply", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanReply { get; set; }

        [JsonProperty("isAdFreeGiftAsk", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsAdFreeGiftAsk { get; set; }

        [JsonProperty("isTumblrmartGiftAsk", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsTumblrmartGiftAsk { get; set; }

        [JsonProperty("isCommercial")]
        public bool IsCommercial { get; set; }

        [JsonProperty("canShare", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanShare { get; set; }

        [JsonProperty("canLike", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanLike { get; set; }

        [JsonProperty("interactabilityReblog", NullValueHandling = NullValueHandling.Ignore)]
        public string InteractabilityReblog { get; set; }

        [JsonProperty("interactabilityBlaze", NullValueHandling = NullValueHandling.Ignore)]
        public string InteractabilityBlaze { get; set; }

        [JsonProperty("canReblog", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanReblog { get; set; }

        [JsonProperty("canSendInMessage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool CanSendInMessage { get; set; }

        [JsonProperty("isPinned", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsPinned { get; set; }

        [JsonProperty("communityLabels", NullValueHandling = NullValueHandling.Ignore)]
        public CommunityLabels CommunityLabels { get; set; }

        [JsonProperty("isBrandSafe")]
        public bool IsBrandSafe { get; set; }

        [JsonProperty("embedUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string EmbedUrl { get; set; }

        [JsonProperty("displayAvatar", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DisplayAvatar { get; set; }

        [JsonProperty("recommendationReason", NullValueHandling = NullValueHandling.Ignore)]
        public object RecommendationReason { get; set; }

        [JsonProperty("dismissal", NullValueHandling = NullValueHandling.Ignore)]
        public object Dismissal { get; set; }

        [JsonProperty("parentPostUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ParentPostUrl { get; set; }

        [JsonProperty("rebloggedFromId", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedFromId { get; set; }

        [JsonProperty("rebloggedFromUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedFromUrl { get; set; }

        [JsonProperty("rebloggedFromName", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedFromName { get; set; }

        [JsonProperty("rebloggedFromTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedFromTitle { get; set; }

        [JsonProperty("rebloggedFromUuid", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedFromUuid { get; set; }

        [JsonProperty("rebloggedFromCanMessage", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedFromCanMessage { get; set; }

        [JsonProperty("rebloggedFromShareLikes", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedFromShareLikes { get; set; }

        [JsonProperty("rebloggedFromShareFollowing", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedFromShareFollowing { get; set; }

        [JsonProperty("rebloggedFromCanBeFollowed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedFromCanBeFollowed { get; set; }

        [JsonProperty("rebloggedFromFollowing", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedFromFollowing { get; set; }

        [JsonProperty("rebloggedRootId", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedRootId { get; set; }

        [JsonProperty("rebloggedRootUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedRootUrl { get; set; }

        [JsonProperty("rebloggedRootName", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedRootName { get; set; }

        [JsonProperty("rebloggedRootTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedRootTitle { get; set; }

        [JsonProperty("rebloggedRootUuid", NullValueHandling = NullValueHandling.Ignore)]
        public string RebloggedRootUuid { get; set; }

        [JsonProperty("rebloggedRootCanMessage", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedRootCanMessage { get; set; }

        [JsonProperty("rebloggedRootShareLikes", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedRootShareLikes { get; set; }

        [JsonProperty("rebloggedRootShareFollowing", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedRootShareFollowing { get; set; }

        [JsonProperty("rebloggedRootCanBeFollowed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedRootCanBeFollowed { get; set; }

        [JsonProperty("rebloggedRootFollowing", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RebloggedRootFollowing { get; set; }

        [JsonProperty("displayType", NullValueHandling = NullValueHandling.Ignore)]
        public int? DisplayType { get; set; }

        [JsonProperty("supplyOpportunityInstanceId", NullValueHandling = NullValueHandling.Ignore)]
        public string SupplyOpportunityInstanceId { get; set; }

        [JsonProperty("supplyProviderId", NullValueHandling = NullValueHandling.Ignore)]
        public string SupplyProviderId { get; set; }

        [JsonProperty("supplyRequestId", NullValueHandling = NullValueHandling.Ignore)]
        public string SupplyRequestId { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public Resources Resources { get; set; }

        [JsonProperty("placementId")]
        public string PlacementId { get; set; }

        [JsonProperty("serveId", NullValueHandling = NullValueHandling.Ignore)]
        public string ServeId { get; set; }

        [JsonProperty("streamGlobalPosition")]
        public int StreamGlobalPosition { get; set; }

        [JsonProperty("streamSessionId")]
        public string StreamSessionId { get; set; }
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

    public class QueryParams
    {
        [JsonProperty("fields")]
        public Fields Fields { get; set; }

        [JsonProperty("npf")]
        public string Npf { get; set; }

        [JsonProperty("reblogInfo")]
        public string ReblogInfo { get; set; }

        [JsonProperty("includePinnedPosts")]
        public string IncludePinnedPosts { get; set; }

        [JsonProperty("tumblelog")]
        public string Tumblelog { get; set; }

        [JsonProperty("pageNumber")]
        public string PageNumber { get; set; }
    }

    public class Resources
    {
        [JsonProperty("clientSideAds")]
        public List<ClientSideAd> ClientSideAds { get; private set; } = new List<ClientSideAd>();

        [JsonProperty("context")]
        public Context Context { get; set; }
    }

    public class TagsV2
    {
        [JsonProperty("name")]
        public string Name { get; set; }
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

        [JsonProperty("bodyFont")]
        public string BodyFont { get; set; }

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

    public class TopTag
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class Trail
    {
        [JsonProperty("content")]
        public List<Content> Content { get; private set; } = new List<Content>();

        [JsonProperty("layout")]
        public List<object> Layout { get; private set; } = new List<object>();

        [JsonProperty("brokenBlog", NullValueHandling = NullValueHandling.Ignore)]
        public Blog BrokenBlog { get; set; }

        [JsonProperty("post")]
        public Post Post { get; set; }

        [JsonProperty("blog", NullValueHandling = NullValueHandling.Ignore)]
        public Blog Blog { get; set; }
    }

    public class TumblrmartAccessories
    {
        [JsonProperty("badges")]
        public List<Badge> Badges { get; private set; } = new List<Badge>();

        [JsonProperty("blueCheckmarkCount")]
        public int BlueCheckmarkCount { get; set; }
    }
}
