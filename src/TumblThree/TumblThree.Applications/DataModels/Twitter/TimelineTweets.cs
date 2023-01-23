using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace TumblThree.Applications.DataModels.Twitter.TimelineTweets
{
    public class TimelineTweets
    {
        [JsonProperty("globalObjects")]
        public GlobalObjects GlobalObjects { get; set; }

        [JsonProperty("timeline")]
        public Timeline Timeline { get; set; }
    }

    public class Tweet
    {
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("full_text")]
        public string FullText { get; set; }

        [JsonProperty("truncated")]
        public bool Truncated { get; set; }

        [JsonProperty("display_text_range")]
        public List<int> DisplayTextRange { get; set; }

        [JsonProperty("entities")]
        public Entities Entities { get; set; }

        [JsonProperty("extended_entities")]
        public ExtendedEntities ExtendedEntities { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("in_reply_to_status_id_str")]
        public string InReplyToStatusIdStr { get; set; }

        [JsonProperty("in_reply_to_user_id_str")]
        public string InReplyToUserIdStr { get; set; }

        [JsonProperty("in_reply_to_screen_name")]
        public string InReplyToScreenName { get; set; }

        [JsonProperty("user_id_str")]
        public string UserIdStr { get; set; }

        [JsonProperty("user")]
        public User User { get; set; }

        [JsonProperty("is_quote_status")]
        public bool IsQuoteStatus { get; set; }

        [JsonProperty("retweeted_status_id_str")]
        public string RetweetedStatusIdStr { get; set; }

        [JsonProperty("retweet_count")]
        public int RetweetCount { get; set; }

        [JsonProperty("favorite_count")]
        public int FavoriteCount { get; set; }

        [JsonProperty("reply_count")]
        public int ReplyCount { get; set; }

        [JsonProperty("quote_count")]
        public int QuoteCount { get; set; }

        [JsonProperty("conversation_id")]
        public long ConversationId { get; set; }

        [JsonProperty("conversation_id_str")]
        public string ConversationIdStr { get; set; }

        [JsonProperty("favorited")]
        public bool Favorited { get; set; }

        [JsonProperty("retweeted")]
        public bool Retweeted { get; set; }

        [JsonProperty("possibly_sensitive")]
        public bool PossiblySensitive { get; set; }

        [JsonProperty("possibly_sensitive_editable")]
        public bool PossiblySensitiveEditable { get; set; }

        [JsonProperty("lang")]
        public string Lang { get; set; }

        [JsonProperty("supplemental_language")]
        public string SupplementalLanguage { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class FocusRect
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

    public class OriginalInfo
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("focus_rects")]
        public List<FocusRect> FocusRects { get; set; }
    }

    public class Thumb
    {
        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class Medium
    {
        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class Large
    {
        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class Small
    {
        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("resize")]
        public string Resize { get; set; }
    }

    public class Sizes
    {
        [JsonProperty("thumb")]
        public Thumb Thumb { get; set; }

        [JsonProperty("medium")]
        public Medium Medium { get; set; }

        [JsonProperty("large")]
        public Large Large { get; set; }

        [JsonProperty("small")]
        public Small Small { get; set; }
    }

    public class Media
    {
        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; set; }

        [JsonProperty("media_url")]
        public string MediaUrl { get; set; }

        [JsonProperty("media_url_https")]
        public string MediaUrlHttps { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("display_url")]
        public string DisplayUrl { get; set; }

        [JsonProperty("expanded_url")]
        public string ExpandedUrl { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("original_info")]
        public OriginalInfo OriginalInfo { get; set; }

        [JsonProperty("sizes")]
        public Sizes Sizes { get; set; }

        [JsonProperty("source_status_id_str")]
        public string SourceStatusIdStr { get; set; }

        [JsonProperty("source_user_id_str")]
        public string SourceUserIdStr { get; set; }

        [JsonProperty("video_info")]
        public VideoInfo VideoInfo { get; set; }

        [JsonProperty("media_key")]
        public string MediaKey { get; set; }

        [JsonProperty("ext_alt_text")]
        public object ExtAltText { get; set; }

        [JsonProperty("ext_media_availability")]
        public ExtMediaAvailability ExtMediaAvailability { get; set; }

        [JsonProperty("ext_media_color")]
        public ExtMediaColor ExtMediaColor { get; set; }

        [JsonProperty("ext")]
        public Ext Ext { get; set; }

        [JsonProperty("additional_media_info")]
        public AdditionalMediaInfo AdditionalMediaInfo { get; set; }
    }

    public class ProfileImageExtensions
    {
        [JsonProperty("mediaStats")]
        public MediaStats MediaStats { get; set; }
    }

    public class ProfileBannerExtensionsMediaColor
    {
        [JsonProperty("palette")]
        public List<Palette> Palette { get; set; }
    }

    public class ProfileBannerExtensions
    {
        [JsonProperty("mediaStats")]
        public MediaStats MediaStats { get; set; }
    }

    public class SourceUser
    {
        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("entities")]
        public Entities Entities { get; set; }

        [JsonProperty("followers_count")]
        public int FollowersCount { get; set; }

        [JsonProperty("fast_followers_count")]
        public int FastFollowersCount { get; set; }

        [JsonProperty("normal_followers_count")]
        public int NormalFollowersCount { get; set; }

        [JsonProperty("friends_count")]
        public int FriendsCount { get; set; }

        [JsonProperty("listed_count")]
        public int ListedCount { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("favourites_count")]
        public int FavouritesCount { get; set; }

        [JsonProperty("statuses_count")]
        public int StatusesCount { get; set; }

        [JsonProperty("media_count")]
        public int MediaCount { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImageUrlHttps { get; set; }

        [JsonProperty("profile_banner_url")]
        public string ProfileBannerUrl { get; set; }

        [JsonProperty("profile_image_extensions_alt_text")]
        public object ProfileImageExtensionsAltText { get; set; }

        [JsonProperty("profile_image_extensions_media_availability")]
        public object ProfileImageExtensionsMediaAvailability { get; set; }

        [JsonProperty("profile_image_extensions_media_color")]
        public ProfileImageExtensionsMediaColor ProfileImageExtensionsMediaColor { get; set; }

        [JsonProperty("profile_image_extensions")]
        public ProfileImageExtensions ProfileImageExtensions { get; set; }

        [JsonProperty("profile_banner_extensions_media_color")]
        public ProfileBannerExtensionsMediaColor ProfileBannerExtensionsMediaColor { get; set; }

        [JsonProperty("profile_banner_extensions_alt_text")]
        public object ProfileBannerExtensionsAltText { get; set; }

        [JsonProperty("profile_banner_extensions_media_availability")]
        public object ProfileBannerExtensionsMediaAvailability { get; set; }

        [JsonProperty("profile_banner_extensions")]
        public ProfileBannerExtensions ProfileBannerExtensions { get; set; }

        [JsonProperty("profile_link_color")]
        public string ProfileLinkColor { get; set; }

        [JsonProperty("default_profile")]
        public bool DefaultProfile { get; set; }

        [JsonProperty("pinned_tweet_ids")]
        public List<object> PinnedTweetIds { get; set; }

        [JsonProperty("pinned_tweet_ids_str")]
        public List<object> PinnedTweetIdsStr { get; set; }

        [JsonProperty("advertiser_account_type")]
        public string AdvertiserAccountType { get; set; }

        [JsonProperty("advertiser_account_service_levels")]
        public List<string> AdvertiserAccountServiceLevels { get; set; }

        [JsonProperty("profile_interstitial_type")]
        public string ProfileInterstitialType { get; set; }

        [JsonProperty("business_profile_state")]
        public string BusinessProfileState { get; set; }

        [JsonProperty("translator_type")]
        public string TranslatorType { get; set; }

        [JsonProperty("withheld_in_countries")]
        public List<object> WithheldInCountries { get; set; }

        [JsonProperty("ext")]
        public Ext Ext { get; set; }
    }

    public class VisitSite
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class CallToActions
    {
        [JsonProperty("visit_site")]
        public VisitSite VisitSite { get; set; }
    }

    public class AdditionalMediaInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("call_to_actions")]
        public CallToActions CallToActions { get; set; }

        [JsonProperty("embeddable")]
        public bool Embeddable { get; set; }

        [JsonProperty("monetizable")]
        public bool Monetizable { get; set; }

        [JsonProperty("source_user")]
        public SourceUser SourceUser { get; set; }
    }

    public class Entities
    {
        [JsonProperty("hashtags")]
        public List<Hashtag> Hashtags { get; set; }

        [JsonProperty("symbols")]
        public List<Symbol> Symbols { get; set; }

        [JsonProperty("user_mentions")]
        public List<UserMention> UserMentions { get; set; }

        [JsonProperty("urls")]
        public List<Url2> Urls { get; set; }

        [JsonProperty("media")]
        public List<Media> Media { get; set; }
    }

    public class Symbol
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; set; }
    }

    public class Url2
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("expanded_url")]
        public string ExpandedUrl { get; set; }

        [JsonProperty("display_url")]
        public string DisplayUrl { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; set; }
    }

    public class UserMention
    {
        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; set; }
    }

    public class UserEntities
    {
        [JsonProperty("description")]
        public Description Description { get; set; }
    }

    public class ExtMediaAvailability
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class Rgb
    {
        [JsonProperty("red")]
        public int Red { get; set; }

        [JsonProperty("green")]
        public int Green { get; set; }

        [JsonProperty("blue")]
        public int Blue { get; set; }
    }

    public class Palette
    {
        [JsonProperty("rgb")]
        public Rgb Rgb { get; set; }

        [JsonProperty("percentage")]
        public double Percentage { get; set; }
    }

    public class ExtMediaColor
    {
        [JsonProperty("palette")]
        public List<Palette> Palette { get; set; }
    }

    public class MediaStats
    {
        [JsonProperty("r")]
        public object R { get; set; }

        [JsonProperty("ttl")]
        public int Ttl { get; set; }
    }

    public class MediaStatsR
    {
        [JsonProperty("missing")]
        public string Missing { get; set; }
    }

    public class Ext
    {
        [JsonProperty("mediaStats")]
        public MediaStats MediaStats { get; set; }

        [JsonProperty("highlightedLabel")]
        public HighlightedLabel HighlightedLabel { get; set; }
    }

    public class ExtendedEntities
    {
        [JsonProperty("media")]
        public List<Media> Media { get; set; }
    }

    public class Description
    {
    }

    public class ProfileImageExtensionsMediaColor
    {
        [JsonProperty("palette")]
        public List<Palette> Palette { get; set; }
    }

    public class R
    {
        [JsonProperty("missing")]
        public object Missing { get; set; }

        [JsonProperty("ok")]
        public Ok Ok { get; set; }
    }

    public class VideoInfo
    {
        [JsonProperty("aspect_ratio")]
        public List<int> AspectRatio { get; set; }

        [JsonProperty("duration_millis")]
        public int DurationMillis { get; set; }

        [JsonProperty("variants")]
        public List<Variant> Variants { get; set; }
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

    public class Ok
    {
    }

    public class HighlightedLabel
    {
        [JsonProperty("r")]
        public R R { get; set; }

        [JsonProperty("ttl")]
        public int Ttl { get; set; }
    }

    public class User
    {
        [JsonProperty("id_str")]
        public string IdStr { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("screen_name")]
        public string ScreenName { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("entities")]
        public UserEntities Entities { get; set; }

        [JsonProperty("followers_count")]
        public int FollowersCount { get; set; }

        [JsonProperty("fast_followers_count")]
        public int FastFollowersCount { get; set; }

        [JsonProperty("normal_followers_count")]
        public int NormalFollowersCount { get; set; }

        [JsonProperty("friends_count")]
        public int FriendsCount { get; set; }

        [JsonProperty("listed_count")]
        public int ListedCount { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("favourites_count")]
        public int FavouritesCount { get; set; }

        [JsonProperty("statuses_count")]
        public int StatusesCount { get; set; }

        [JsonProperty("media_count")]
        public int MediaCount { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImageUrlHttps { get; set; }

        [JsonProperty("profile_image_extensions_alt_text")]
        public object ProfileImageExtensionsAltText { get; set; }

        [JsonProperty("profile_image_extensions_media_availability")]
        public object ProfileImageExtensionsMediaAvailability { get; set; }

        [JsonProperty("profile_image_extensions_media_color")]
        public ProfileImageExtensionsMediaColor ProfileImageExtensionsMediaColor { get; set; }

        [JsonProperty("profile_image_extensions")]
        public ProfileImageExtensions ProfileImageExtensions { get; set; }

        [JsonProperty("profile_link_color")]
        public string ProfileLinkColor { get; set; }

        [JsonProperty("has_extended_profile")]
        public bool HasExtendedProfile { get; set; }

        [JsonProperty("default_profile")]
        public bool DefaultProfile { get; set; }

        [JsonProperty("pinned_tweet_ids")]
        public List<long> PinnedTweetIds { get; set; }

        [JsonProperty("pinned_tweet_ids_str")]
        public List<string> PinnedTweetIdsStr { get; set; }

        [JsonProperty("advertiser_account_type")]
        public string AdvertiserAccountType { get; set; }

        [JsonProperty("advertiser_account_service_levels")]
        public List<object> AdvertiserAccountServiceLevels { get; set; }

        [JsonProperty("profile_interstitial_type")]
        public string ProfileInterstitialType { get; set; }

        [JsonProperty("business_profile_state")]
        public string BusinessProfileState { get; set; }

        [JsonProperty("translator_type")]
        public string TranslatorType { get; set; }

        [JsonProperty("withheld_in_countries")]
        public List<object> WithheldInCountries { get; set; }

        [JsonProperty("ext")]
        public Ext Ext { get; set; }
    }

    public class Moments
    {
    }

    public class Cards
    {
    }

    public class Places
    {
    }

    public class Broadcasts
    {
    }

    public class Topics
    {
    }

    public class Lists
    {
    }

    public class GlobalObjects
    {
        [JsonProperty("tweets")]
        public Dictionary<string, Tweet> Tweets { get; set; }

        [JsonProperty("users")]
        public Dictionary<string, User> Users { get; set; }

        [JsonProperty("moments")]
        public Moments Moments { get; set; }

        [JsonProperty("cards")]
        public Cards Cards { get; set; }

        [JsonProperty("places")]
        public Places Places { get; set; }

        [JsonProperty("media")]
        public Media2 Media { get; set; }

        [JsonProperty("broadcasts")]
        public Broadcasts Broadcasts { get; set; }

        [JsonProperty("topics")]
        public Topics Topics { get; set; }

        [JsonProperty("lists")]
        public Lists Lists { get; set; }
    }

    public class Media2
    { 
    }

    public class ContentTweet
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayType")]
        public string DisplayType { get; set; }

        [JsonProperty("socialContext", NullValueHandling = NullValueHandling.Ignore)]
        public SocialContext SocialContext { get; set; }
    }

    public class ItemContent
    {
        [JsonProperty("tweet")]
        public ContentTweet Tweet { get; set; }
    }

    public class Item
    {
        [JsonProperty("content")]
        public ItemContent Content { get; set; }

        [JsonProperty("clientEventInfo", NullValueHandling = NullValueHandling.Ignore)]
        public ClientEventInfo ClientEventInfo { get; set; }
    }

    public class Cursor
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("cursorType")]
        public string CursorType { get; set; }

        [JsonProperty("stopOnEmptyResponse")]
        public bool? StopOnEmptyResponse { get; set; }
    }

    public class Operation
    {
        [JsonProperty("cursor")]
        public Cursor Cursor { get; set; }
    }

    public class EntryContent
    {
        [JsonProperty("item")]
        public Item Item { get; set; }

        [JsonProperty("operation")]
        public Operation Operation { get; set; }
    }

    public class Entry
    {
        [JsonProperty("entryId")]
        public string EntryId { get; set; }

        [JsonProperty("sortIndex")]
        public string SortIndex { get; set; }

        [JsonProperty("content")]
        public EntryContent Content { get; set; }
    }

    public class AddEntries
    {
        [JsonProperty("entries")]
        public List<Entry> Entries { get; set; }
    }

    public class GeneralContext
    {
        [JsonProperty("contextType")]
        public string ContextType { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class SocialContext
    {
        [JsonProperty("generalContext")]
        public GeneralContext GeneralContext { get; set; }
    }

    public class TimelinesDetails
    {
        [JsonProperty("injectionType")]
        public string InjectionType { get; set; }
    }

    public class Details
    {
        [JsonProperty("timelinesDetails")]
        public TimelinesDetails TimelinesDetails { get; set; }
    }

    public class ClientEventInfo
    {
        [JsonProperty("component")]
        public string Component { get; set; }

        [JsonProperty("details")]
        public Details Details { get; set; }
    }

    public class PinEntryContent
    {
        [JsonProperty("item")]
        public Item Item { get; set; }
    }

    public class PinEntryEntry
    {
        [JsonProperty("entryId")]
        public string EntryId { get; set; }

        [JsonProperty("sortIndex")]
        public string SortIndex { get; set; }

        [JsonProperty("content")]
        public PinEntryContent Content { get; set; }
    }

    public class PinEntry
    {
        [JsonProperty("entry")]
        public PinEntryEntry Entry { get; set; }
    }

    public class Instruction
    {
        [JsonProperty("clearCache")]
        public object ClearCache { get; set; }

        [JsonProperty("addEntries")]
        public AddEntries AddEntries { get; set; }

        [JsonProperty("pinEntry")]
        public PinEntry PinEntry { get; set; }

        [JsonProperty("replaceEntry")]
        public ReplaceEntry ReplaceEntry { get; set; }
    }

    public class ReplaceEntry
    {
        [JsonProperty("entryIdToReplace")]
        public string EntryIdToReplace { get; set; }

        [JsonProperty("entry")]
        public Entry Entry { get; set; }
    }

    public class FeedbackActions
    {
    }

    public class ResponseObjects
    {
        [JsonProperty("feedbackActions")]
        public FeedbackActions FeedbackActions { get; set; }
    }

    public class Timeline
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("instructions")]
        public List<Instruction> Instructions { get; set; }

        [JsonProperty("responseObjects")]
        public ResponseObjects ResponseObjects { get; set; }
    }

    public class Hashtag
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("indices")]
        public List<int> Indices { get; set; }
    }
}
