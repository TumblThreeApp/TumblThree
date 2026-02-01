using Newtonsoft.Json;
using System.Collections.Generic;

namespace TumblThree.Applications.DataModels.Twitter.TimelineTweets
{
    public class TimelineTweets
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore)]
        public List<Error> Errors { get; } = new List<Error>();

        [JsonIgnore]
        public Timeline Timeline => Data.User?.Result.TimelineV2.Timeline ?? Data.SearchByRawQuery?.SearchTimeline.Timeline;
    }

    public class AdditionalMediaInfo
    {
        [JsonProperty("monetizable")]
        public bool Monetizable { get; set; }

        [JsonProperty("source_user")]
        public SourceUser SourceUser { get; set; }
    }

    public class AdvertiserResults
    {
        [JsonProperty("result")]
        public User Result { get; set; }
    }

    public class AffiliatesHighlightedLabel
    {
    }

    public class Audience
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class BindingValue
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public Value Value { get; set; }
    }

    public class Card
    {
        [JsonProperty("rest_id")]
        public string RestId { get; set; }

        [JsonProperty("legacy")]
        public CardLegacy Legacy { get; set; }
    }

    public class CardLegacy
    {
        [JsonProperty("binding_values")]
        public List<BindingValue> BindingValues { get; } = new List<BindingValue>();

        [JsonProperty("card_platform")]
        public CardPlatform CardPlatform { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("user_refs_results")]
        public List<object> UserRefsResults { get; } = new List<object>();
    }

    public class CardPlatform
    {
        [JsonProperty("platform")]
        public Platform Platform { get; set; }
    }

    public class ClickTrackingInfo
    {
        [JsonProperty("urlParams")]
        public List<UrlParam> UrlParams { get; } = new List<UrlParam>();
    }

    public class ClientEventInfo
    {
        [JsonProperty("component")]
        public string Component { get; set; }

        [JsonProperty("element", NullValueHandling = NullValueHandling.Ignore)]
        public string Element { get; set; }

        [JsonProperty("details", NullValueHandling = NullValueHandling.Ignore)]
        public Details Details { get; set; }
    }

    public class Content
    {
        [JsonProperty("entryType")]
        public string EntryType { get; set; }

        [JsonProperty("__typename")]
        public string Typename { get; set; }

        // TimelineTimelineItem

        [JsonProperty("itemContent", NullValueHandling = NullValueHandling.Ignore)]
        public ItemContent ItemContent { get; set; }

        [JsonProperty("clientEventInfo", NullValueHandling = NullValueHandling.Ignore)]
        public ClientEventInfo ClientEventInfo { get; set; }

        // TimelineTimelineModule

        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<ContentItem> Items { get; } = new List<ContentItem>();

        [JsonProperty("displayType", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayType { get; set; }

        [JsonProperty("header", NullValueHandling = NullValueHandling.Ignore)]
        public Header Header { get; set; }

        [JsonProperty("footer", NullValueHandling = NullValueHandling.Ignore)]
        public Footer Footer { get; set; }

        //public ClientEventInfo ClientEventInfo { get; set; }

        // TimelineTimelineCursor

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        [JsonProperty("cursorType", NullValueHandling = NullValueHandling.Ignore)]
        public string CursorType { get; set; }
    }

    public class ContentItem
    {
        [JsonProperty("entryId")]
        public string EntryId { get; set; }

        [JsonProperty("item")]
        public Item Item { get; set; }
    }

    public class ConversationControl
    {
        [JsonProperty("policy")]
        public string Policy { get; set; }

        [JsonProperty("conversation_owner_results")]
        public ConversationOwnerResults ConversationOwnerResults { get; set; }
    }

    public class ConversationDetails
    {
        [JsonProperty("conversationSection")]
        public string ConversationSection { get; set; }
    }

    public class ConversationOwnerResult
    {
        [JsonProperty("__typename")]
        public string Typename { get; set; }

        [JsonProperty("legacy")]
        public ConversationOwnerUserLegacy Legacy { get; set; }
    }

    public class ConversationOwnerResults
    {
        [JsonProperty("result")]
        public ConversationOwnerResult Result { get; set; }
    }

    public class ConversationOwnerUserLegacy
    {
        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }
    }

    public class Core
    {
        [JsonProperty("user_results")]
        public UserResults UserResults { get; set; }
    }

    public class Data
    {
        [JsonProperty("user")]
        public DataUser User { get; set; }

        // ... replies

        [JsonProperty("threaded_conversation_with_injections_v2", NullValueHandling = NullValueHandling.Ignore)]
        public ThreadedConversationWithInjectionsV2 ThreadedConversationWithInjectionsV2 { get; set; }

        // extended search

        [JsonProperty("search_by_raw_query", NullValueHandling = NullValueHandling.Ignore)]
        public SearchByRawQuery SearchByRawQuery { get; set; }
    }

    public class DataUser
    {
        [JsonProperty("result")]
        public UserResult Result { get; set; }
    }

    public class Description
    {
        [JsonProperty("urls")]
        public List<object> Urls { get; } = new List<object>();
    }

    public class Details
    {
        [JsonProperty("conversationDetails", NullValueHandling = NullValueHandling.Ignore)]
        public ConversationDetails ConversationDetails { get; set; }

        [JsonProperty("timelinesDetails", NullValueHandling = NullValueHandling.Ignore)]
        public TimelinesDetails TimelinesDetails { get; set; }
    }

    public class Device
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class EditControl
    {
        [JsonProperty("edit_tweet_ids")]
        public List<string> EditTweetIds { get; } = new List<string>();

        [JsonProperty("editable_until_msecs")]
        public string EditableUntilMsecs { get; set; }

        [JsonProperty("is_edit_eligible")]
        public bool IsEditEligible { get; set; }

        [JsonProperty("edits_remaining")]
        public string EditsRemaining { get; set; }
    }

    public class EditPerspective
    {
        [JsonProperty("favorited")]
        public bool Favorited { get; set; }

        [JsonProperty("retweeted")]
        public bool Retweeted { get; set; }
    }

    public class Entities
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public Description Description { get; set; }

        [JsonProperty("media", NullValueHandling = NullValueHandling.Ignore)]
        public List<Media> Media { get; } = new List<Media>();

        [JsonProperty("user_mentions", NullValueHandling = NullValueHandling.Ignore)]
        public List<UserMention> UserMentions { get; } = new List<UserMention>();

        [JsonProperty("urls", NullValueHandling = NullValueHandling.Ignore)]
        public List<Url2> Urls { get; } = new List<Url2>();

        [JsonProperty("hashtags", NullValueHandling = NullValueHandling.Ignore)]
        public List<Hashtag> Hashtags { get; } = new List<Hashtag>();

        [JsonProperty("symbols", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> Symbols { get; } = new List<object>();

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public Url Url { get; set; }
    }

    public class Entry
    {
        [JsonProperty("entryId")]
        public string EntryId { get; set; }

        [JsonProperty("sortIndex")]
        public string SortIndex { get; set; }

        [JsonProperty("content")]
        public Content Content { get; set; }
    }

    public class Error
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("path")]
        public List<object> Path { get; } = new List<object>();

        [JsonProperty("locations")]
        public List<Location> Locations { get; } = new List<Location>();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("retry_after")]
        public int RetryAfter { get; set; }

        [JsonProperty("tracing")]
        public Tracing Tracing { get; set; }

        [JsonProperty("extensions")]
        public Extensions Extensions { get; set; }
    }

    public class ExperimentValue
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ExtendedEntities
    {
        [JsonProperty("media")]
        public List<Media> Media { get; } = new List<Media>();
    }

    public class Extensions
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("retry_after")]
        public int RetryAfter { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("tracing")]
        public Tracing Tracing { get; set; }
    }

    public class ExtMediaAvailability
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class Face
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }
    }

    public class FeatureLarge
    {
        [JsonProperty("faces")]
        public List<Face> Faces { get; } = new List<Face>();
    }

    public class FeatureMedium
    {
        [JsonProperty("faces")]
        public List<Face> Faces { get; } = new List<Face>();
    }

    public class Features
    {
        [JsonProperty("large")]
        public FeatureLarge Large { get; set; }

        [JsonProperty("medium")]
        public FeatureMedium Medium { get; set; }

        [JsonProperty("small")]
        public FeatureSmall Small { get; set; }

        [JsonProperty("orig")]
        public Orig Orig { get; set; }
    }

    public class FeatureSmall
    {
        [JsonProperty("faces")]
        public List<Face> Faces { get; } = new List<Face>();
    }

    public class FocusRect
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }
    }

    public class Footer
    {
        [JsonProperty("displayType")]
        public string DisplayType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("landingUrl")]
        public LandingUrl LandingUrl { get; set; }
    }

    public class Hashtag
    {
        [JsonProperty("indices")]
        public List<int> Indices { get; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Header
    {
        [JsonProperty("displayType")]
        public string DisplayType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("sticky")]
        public bool Sticky { get; set; }
    }

    public class Headline
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("entities")]
        public List<object> Entities { get; } = new List<object>();
    }

    public class Instruction
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("entries", NullValueHandling = NullValueHandling.Ignore)]
        public List<Entry> Entries { get; } = new List<Entry>();

        [JsonProperty("direction", NullValueHandling = NullValueHandling.Ignore)]
        public string Direction { get; set; }

        // TimelinePinEntry

        [JsonProperty("entry", NullValueHandling = NullValueHandling.Ignore)]
        public Entry Entry { get; set; }
    }

    public class Item
    {
        [JsonProperty("itemContent")]
        public ItemContent ItemContent { get; set; }

        [JsonProperty("clientEventInfo")]
        public ClientEventInfo ClientEventInfo { get; set; }
    }

    public class ItemContent
    {
        [JsonProperty("itemType")]
        public string ItemType { get; set; }

        [JsonProperty("__typename")]
        public string Typename { get; set; }

        // TimelineTweet / EmphasizedPromotedTweet

        [JsonProperty("tweet_results", NullValueHandling = NullValueHandling.Ignore)]
        public TweetResults TweetResults { get; set; }

        [JsonProperty("tweetDisplayType", NullValueHandling = NullValueHandling.Ignore)]
        public string TweetDisplayType { get; set; }

        // ... replies

        [JsonProperty("hasModeratedReplies", NullValueHandling = NullValueHandling.Ignore)]
        public bool HasModeratedReplies { get; set; }

        // TimelineUser

        [JsonProperty("user_results", NullValueHandling = NullValueHandling.Ignore)]
        public UserResults UserResults { get; set; }

        [JsonProperty("userDisplayType", NullValueHandling = NullValueHandling.Ignore)]
        public string UserDisplayType { get; set; }

        // TimelineUser + TimelinePinEntry

        [JsonProperty("socialContext", NullValueHandling = NullValueHandling.Ignore)]
        public SocialContext SocialContext { get; set; }

        // EmphasizedPromotedTweet

        [JsonProperty("promotedMetadata", NullValueHandling = NullValueHandling.Ignore)]
        public PromotedMetadata PromotedMetadata { get; set; }
    }

    public class LandingUrl
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("urlType")]
        public string UrlType { get; set; }
    }

    public class Legacy
    {
        [JsonProperty("bookmark_count")]
        public int BookmarkCount { get; set; }

        [JsonProperty("bookmarked")]
        public bool Bookmarked { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("conversation_id_str")]
        public string ConversationIdStr { get; set; }

        [JsonProperty("display_text_range")]
        public List<int> DisplayTextRange { get; } = new List<int>();

        [JsonProperty("entities")]
        public Entities Entities { get; set; }

        [JsonProperty("extended_entities")]
        public ExtendedEntities ExtendedEntities { get; set; }

        [JsonProperty("favorite_count")]
        public int FavoriteCount { get; set; }

        [JsonProperty("favorited")]
        public bool Favorited { get; set; }

        [JsonProperty("full_text")]
        public string FullText { get; set; }

        [JsonProperty("is_quote_status")]
        public bool IsQuoteStatus { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("possibly_sensitive")]
        public bool PossiblySensitive { get; set; }

        [JsonProperty("possibly_sensitive_editable")]
        public bool PossiblySensitiveEditable { get; set; }

        [JsonProperty("quote_count")]
        public int QuoteCount { get; set; }

        [JsonProperty("reply_count")]
        public int ReplyCount { get; set; }

        [JsonProperty("retweet_count")]
        public int RetweetCount { get; set; }

        [JsonProperty("retweeted")]
        public bool Retweeted { get; set; }

        [JsonProperty("user_id_str")]
        public string UserIdStr { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        // with replies

        [JsonProperty("in_reply_to_screen_name", NullValueHandling = NullValueHandling.Ignore)]
        public string InReplyToScreenName { get; set; }

        [JsonProperty("in_reply_to_status_id_str", NullValueHandling = NullValueHandling.Ignore)]
        public string InReplyToStatusIdStr { get; set; }

        [JsonProperty("in_reply_to_user_id_str", NullValueHandling = NullValueHandling.Ignore)]
        public string InReplyToUserIdStr { get; set; }

        // retweets

        [JsonProperty("retweeted_status_result", NullValueHandling = NullValueHandling.Ignore)]
        public RetweetedStatusResult RetweetedStatusResult { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        // EmphasizedPromotedTweet

        [JsonProperty("conversation_control", NullValueHandling = NullValueHandling.Ignore)]
        public ConversationControl ConversationControl { get; set; }

        [JsonProperty("limited_actions", NullValueHandling = NullValueHandling.Ignore)]
        public string LimitedActions { get; set; }

        [JsonProperty("scopes", NullValueHandling = NullValueHandling.Ignore)]
        public Scopes Scopes { get; set; }
    }

    public class LimitedAction
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("prompt")]
        public Prompt Prompt { get; set; }
    }

    public class LimitedActionResults
    {
        [JsonProperty("limited_actions")]
        public List<LimitedAction> LimitedActions { get; } = new List<LimitedAction>();
    }

    public class Location
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }
    }

    public class MediaStats
    {
        [JsonProperty("viewCount")]
        public int ViewCount { get; set; }
    }

    public class Media
    {
        [JsonProperty("display_url")]
        public string DisplayUrl { get; set; }

        [JsonProperty("expanded_url")]
        public string ExpandedUrl { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; } = new List<int>();

        [JsonProperty("media_url_https")]
        public string MediaUrlHttps { get; set; }

        [JsonProperty("source_status_id_str")]
        public string SourceStatusIdStr { get; set; }

        [JsonProperty("source_user_id_str")]
        public string SourceUserIdStr { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("features")]
        public Features Features { get; set; }

        [JsonProperty("sizes")]
        public Sizes Sizes { get; set; }

        [JsonProperty("original_info")]
        public OriginalInfo OriginalInfo { get; set; }

        [JsonProperty("media_key")]
        public string MediaKey { get; set; }

        [JsonProperty("additional_media_info")]
        public AdditionalMediaInfo AdditionalMediaInfo { get; set; }

        [JsonProperty("mediaStats")]
        public MediaStats MediaStats { get; set; }

        [JsonProperty("ext_media_availability")]
        public ExtMediaAvailability ExtMediaAvailability { get; set; }

        [JsonProperty("video_info")]
        public VideoInfo VideoInfo { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("scribeConfig")]
        public ScribeConfig ScribeConfig { get; set; }
    }

    public class Orig
    {
        [JsonProperty("faces")]
        public List<Face> Faces { get; } = new List<Face>();
    }

    public class OriginalInfo
    {
        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("focus_rects")]
        public List<FocusRect> FocusRects { get; } = new List<FocusRect>();
    }

    public class Platform
    {
        [JsonProperty("audience")]
        public Audience Audience { get; set; }

        [JsonProperty("device")]
        public Device Device { get; set; }
    }

    public class Professional
    {
        [JsonProperty("rest_id")]
        public string RestId { get; set; }

        [JsonProperty("professional_type")]
        public string ProfessionalType { get; set; }

        [JsonProperty("category")]
        public List<object> Category { get; } = new List<object>();
    }

    public class PromotedMetadata
    {
        [JsonProperty("advertiser_results")]
        public AdvertiserResults AdvertiserResults { get; set; }

        [JsonProperty("disclosureType")]
        public string DisclosureType { get; set; }

        [JsonProperty("experimentValues")]
        public List<ExperimentValue> ExperimentValues { get; } = new List<ExperimentValue>();

        [JsonProperty("impressionId")]
        public string ImpressionId { get; set; }

        [JsonProperty("impressionString")]
        public string ImpressionString { get; set; }

        [JsonProperty("clickTrackingInfo")]
        public ClickTrackingInfo ClickTrackingInfo { get; set; }
    }

    public class Prompt
    {
        [JsonProperty("__typename")]
        public string Typename { get; set; }

        [JsonProperty("cta_type")]
        public string CtaType { get; set; }

        [JsonProperty("headline")]
        public Headline Headline { get; set; }

        [JsonProperty("subtext")]
        public Subtext Subtext { get; set; }
    }

    public class QuickPromoteEligibility
    {
        [JsonProperty("eligibility")]
        public string Eligibility { get; set; }
    }

    public class RetweetedStatusResult
    {
        [JsonProperty("result")]
        public Tweet Result { get; set; }
    }

    public class Scopes
    {
        [JsonProperty("followers")]
        public bool Followers { get; set; }
    }

    public class ScribeConfig
    {
        [JsonProperty("page")]
        public string Page { get; set; }
    }

    public class SearchByRawQuery
    {
        [JsonProperty("search_timeline", NullValueHandling = NullValueHandling.Ignore)]
        public TimelineV2 SearchTimeline { get; set; }
    }

    public class SizeLarge
    {
        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class SizeMedium
    {
        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class Sizes
    {
        [JsonProperty("large")]
        public SizeLarge Large { get; set; }

        [JsonProperty("medium")]
        public SizeMedium Medium { get; set; }

        [JsonProperty("small")]
        public SizeSmall Small { get; set; }

        [JsonProperty("thumb")]
        public Thumb Thumb { get; set; }
    }

    public class SizeSmall
    {
        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class SocialContext
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("contextType")]
        public string ContextType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class SourceUser
    {
        [JsonProperty("user_results")]
        public UserResults UserResults { get; set; }
    }

    public class Subtext
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("entities")]
        public List<object> Entities { get; } = new List<object>();
    }

    public class ThreadedConversationWithInjectionsV2
    {
        [JsonProperty("instructions")]
        public List<Instruction> Instructions { get; } = new List<Instruction>();
    }

    public class Thumb
    {
        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class Timeline
    {
        [JsonProperty("instructions")]
        public List<Instruction> Instructions { get; } = new List<Instruction>();

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Metadata Metadata { get; set; }
    }

    public class TimelinesDetails
    {
        [JsonProperty("injectionType")]
        public string InjectionType { get; set; }

        [JsonProperty("controllerData")]
        public string ControllerData { get; set; }

        [JsonProperty("sourceData", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceData { get; set; }
    }

    public class TimelineV2
    {
        [JsonProperty("timeline")]
        public Timeline Timeline { get; set; }
    }

    public class Tracing
    {
        [JsonProperty("trace_id")]
        public string TraceId { get; set; }
    }

    public class Tweet
    {
        [JsonProperty("__typename")]
        public string Typename { get; set; }

        [JsonProperty("rest_id")]
        public string RestId { get; set; }

        [JsonProperty("has_birdwatch_notes", NullValueHandling = NullValueHandling.Ignore)]
        public bool HasBirdwatchNotes { get; set; }

        [JsonProperty("core")]
        public Core Core { get; set; }

        [JsonProperty("card", NullValueHandling = NullValueHandling.Ignore)]
        public Card Card { get; set; }

        [JsonProperty("unified_card", NullValueHandling = NullValueHandling.Ignore)]
        public UnifiedCard UnifiedCard { get; set; }

        [JsonProperty("edit_control")]
        public EditControl EditControl { get; set; }

        [JsonProperty("edit_perspective")]
        public EditPerspective EditPerspective { get; set; }

        [JsonProperty("is_translatable")]
        public bool IsTranslatable { get; set; }

        [JsonProperty("views")]
        public Views Views { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("legacy")]
        public Legacy Legacy { get; set; }

        [JsonProperty("quick_promote_eligibility")]
        public QuickPromoteEligibility QuickPromoteEligibility { get; set; }

        // TweetWithVisibilityResults

        [JsonProperty("tweet", NullValueHandling = NullValueHandling.Ignore)]
        public Tweet TweetWithVisibilityResults { get; set; }

        [JsonProperty("limitedActionResults", NullValueHandling = NullValueHandling.Ignore)]
        public LimitedActionResults LimitedActionResults { get; set; }

        [JsonIgnore]
        public User User => Core?.UserResults.Result ?? TweetWithVisibilityResults.Core.UserResults.Result;
    }

    public class TweetResults
    {
        [JsonProperty("result")]
        public Tweet Result { get; set; }   //TweetResult

        [JsonIgnore]
        public Tweet Tweet => Result?.TweetWithVisibilityResults ?? Result;
    }

    public class UnifiedCard
    {
        [JsonProperty("card_fetch_state")]
        public string CardFetchState { get; set; }
    }

    public class Url
    {
        [JsonProperty("urls")]
        public List<Url2> Urls { get; } = new List<Url2>();
    }

    public class Url2
    {
        [JsonProperty("display_url")]
        public string DisplayUrl { get; set; }

        [JsonProperty("expanded_url")]
        public string ExpandedUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; } = new List<int>();
    }

    public class UrlParam
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class User
    {
        [JsonProperty("__typename")]
        public string Typename { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("rest_id")]
        public string RestId { get; set; }

        [JsonProperty("affiliates_highlighted_label")]
        public AffiliatesHighlightedLabel AffiliatesHighlightedLabel { get; set; }

        [JsonProperty("has_graduated_access")]
        public bool HasGraduatedAccess { get; set; }

        [JsonProperty("is_blue_verified")]
        public bool IsBlueVerified { get; set; }

        [JsonProperty("profile_image_shape")]
        public string ProfileImageShape { get; set; }

        [JsonProperty("legacy")]
        public UserLegacy Legacy { get; set; }

        [JsonProperty("professional", NullValueHandling = NullValueHandling.Ignore)]
        public Professional Professional { get; set; }
    }

    public class UserLegacy
    {
        [JsonProperty("following")]
        public bool Following { get; set; }

        [JsonProperty("can_dm")]
        public bool CanDm { get; set; }

        [JsonProperty("can_media_tag")]
        public bool CanMediaTag { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("default_profile")]
        public bool DefaultProfile { get; set; }

        [JsonProperty("default_profile_image")]
        public bool DefaultProfileImage { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("entities")]
        public Entities Entities { get; set; }

        [JsonProperty("fast_followers_count")]
        public int FastFollowersCount { get; set; }

        [JsonProperty("favourites_count")]
        public int FavouritesCount { get; set; }

        [JsonProperty("followers_count")]
        public int FollowersCount { get; set; }

        [JsonProperty("friends_count")]
        public int FriendsCount { get; set; }

        [JsonProperty("has_custom_timelines")]
        public bool HasCustomTimelines { get; set; }

        [JsonProperty("is_translator")]
        public bool IsTranslator { get; set; }

        [JsonProperty("listed_count")]
        public int ListedCount { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("media_count")]
        public int MediaCount { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("normal_followers_count")]
        public int NormalFollowersCount { get; set; }

        [JsonProperty("pinned_tweet_ids_str")]
        public List<object> PinnedTweetIdsStr { get; } = new List<object>();

        [JsonProperty("possibly_sensitive")]
        public bool PossiblySensitive { get; set; }

        [JsonProperty("profile_banner_url")]
        public string ProfileBannerUrl { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImageUrlHttps { get; set; }

        [JsonProperty("profile_interstitial_type")]
        public string ProfileInterstitialType { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("statuses_count")]
        public int StatusesCount { get; set; }

        [JsonProperty("translator_type")]
        public string TranslatorType { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }

        [JsonProperty("want_retweets")]
        public bool WantRetweets { get; set; }

        [JsonProperty("withheld_in_countries")]
        public List<object> WithheldInCountries { get; } = new List<object>();
    }

    public class UserMention
    {
        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; } = new List<int>();
    }

    public class UserResult
    {
        [JsonProperty("__typename")]
        public string Typename { get; set; }

        [JsonProperty("timeline_v2")]
        public TimelineV2 TimelineV2 { get; set; }
    }

    public class UserResults
    {
        [JsonProperty("result")]
        public User Result { get; set; }    //UserResult
    }

    public class Value
    {
        [JsonProperty("string_value")]
        public string StringValue { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("scribe_key")]
        public string ScribeKey { get; set; }
    }

    public class Variant
    {
        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("bitrate")]
        public int? Bitrate { get; set; }
    }

    public class VideoInfo
    {
        [JsonProperty("aspect_ratio")]
        public List<int> AspectRatio { get; } = new List<int>();

        [JsonProperty("duration_millis")]
        public int DurationMillis { get; set; }

        [JsonProperty("variants")]
        public List<Variant> Variants { get; } = new List<Variant>();
    }

    public class Views
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("count")]
        public string Count { get; set; }
    }
}
