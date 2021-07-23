using Newtonsoft.Json;
using System.Collections.Generic;

namespace TumblThree.Applications.DataModels.Twitter.TwitterUser
{
    public class TwitterUser
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("errors")]
        public List<Error> Errors { get; set; }
    }

    public class Location
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }
    }

    public class Tracing
    {
        [JsonProperty("trace_id")]
        public string TraceId { get; set; }
    }

    public class Extensions
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("tracing")]
        public Tracing Tracing { get; set; }
    }

    public class Error
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("path")]
        public List<string> Path { get; set; }

        [JsonProperty("locations")]
        public List<Location> Locations { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("tracing")]
        public Tracing Tracing { get; set; }

        [JsonProperty("extensions")]
        public Extensions Extensions { get; set; }
    }

    public class AffiliatesHighlightedLabel
    {
    }

    public class Description
    {
        [JsonProperty("urls")]
        public List<Url2> Urls { get; set; }
    }

    public class Entities
    {
        [JsonProperty("description")]
        public Description Description { get; set; }

        [JsonProperty("url")]
        public Url Url { get; set; }
    }

    public class Url
    {
        [JsonProperty("urls")]
        public List<Url2> Urls { get; set; }
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
        public List<int> Indices { get; set; }
    }

    public class Rgb
    {
        [JsonProperty("blue")]
        public int Blue { get; set; }

        [JsonProperty("green")]
        public int Green { get; set; }

        [JsonProperty("red")]
        public int Red { get; set; }
    }

    public class Palette
    {
        [JsonProperty("percentage")]
        public double Percentage { get; set; }

        [JsonProperty("rgb")]
        public Rgb Rgb { get; set; }
    }

    public class Ok
    {
        [JsonProperty("palette")]
        public List<Palette> Palette { get; set; }
    }

    public class R
    {
        [JsonProperty("ok")]
        public Ok Ok { get; set; }
    }

    public class MediaColor
    {
        [JsonProperty("r")]
        public R R { get; set; }
    }

    public class ProfileImageExtensions
    {
        [JsonProperty("mediaColor")]
        public MediaColor MediaColor { get; set; }
    }

    public class Legacy
    {
        [JsonProperty("blocked_by")]
        public bool BlockedBy { get; set; }

        [JsonProperty("blocking")]
        public bool Blocking { get; set; }

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

        [JsonProperty("follow_request_sent")]
        public bool FollowRequestSent { get; set; }

        [JsonProperty("followed_by")]
        public bool FollowedBy { get; set; }

        [JsonProperty("followers_count")]
        public int FollowersCount { get; set; }

        [JsonProperty("following")]
        public bool Following { get; set; }

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

        [JsonProperty("muting")]
        public bool Muting { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("normal_followers_count")]
        public int NormalFollowersCount { get; set; }

        [JsonProperty("notifications")]
        public bool Notifications { get; set; }

        [JsonProperty("pinned_tweet_ids_str")]
        public List<string> PinnedTweetIdsStr { get; set; }

        [JsonProperty("profile_image_extensions")]
        public ProfileImageExtensions ProfileImageExtensions { get; set; }

        [JsonProperty("profile_image_url_https")]
        public string ProfileImageUrlHttps { get; set; }

        [JsonProperty("profile_interstitial_type")]
        public string ProfileInterstitialType { get; set; }

        [JsonProperty("protected")]
        public bool Protected { get; set; }

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
        public List<string> WithheldInCountries { get; set; }
    }

    public class LegacyExtendedProfile
    {
    }

    public class User
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("rest_id")]
        public string RestId { get; set; }

        [JsonProperty("affiliates_highlighted_label")]
        public AffiliatesHighlightedLabel AffiliatesHighlightedLabel { get; set; }

        [JsonProperty("legacy")]
        public Legacy Legacy { get; set; }

        [JsonProperty("legacy_extended_profile")]
        public LegacyExtendedProfile LegacyExtendedProfile { get; set; }

        [JsonProperty("is_profile_translatable")]
        public bool IsProfileTranslatable { get; set; }
    }

    public class Data
    {
        [JsonProperty("user")]
        public User User { get; set; }
    }
}
