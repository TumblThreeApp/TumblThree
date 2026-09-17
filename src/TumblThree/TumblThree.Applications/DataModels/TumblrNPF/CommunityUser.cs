using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class AgeVerification
    {
        [JsonProperty("blockApp")]
        public bool BlockApp { get; set; }

        [JsonProperty("blockReason")]
        public string BlockReason { get; set; }
    }

    public class BlogC
    {
        [JsonProperty("avatar")]
        public List<Avatar> Avatar { get; private set; } = new List<Avatar>();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("blogViewUrl")]
        public string BlogViewUrl { get; set; }

        [JsonProperty("canMessage")]
        public bool CanMessage { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("isAdult")]
        public bool IsAdult { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("isPrivateChannel")]
        public bool IsPrivateChannel { get; set; }

        [JsonProperty("posts")]
        public int Posts { get; set; }

        [JsonProperty("isGroupChannel")]
        public bool IsGroupChannel { get; set; }

        [JsonProperty("primary")]
        public bool Primary { get; set; }

        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("drafts")]
        public int Drafts { get; set; }

        [JsonProperty("followers")]
        public int Followers { get; set; }

        [JsonProperty("queue")]
        public int Queue { get; set; }

        [JsonProperty("hasFlaggedPosts")]
        public bool HasFlaggedPosts { get; set; }

        [JsonProperty("messages")]
        public int Messages { get; set; }

        [JsonProperty("ask")]
        public bool Ask { get; set; }

        [JsonProperty("mentionKey")]
        public string MentionKey { get; set; }

        [JsonProperty("timezoneOffset")]
        public string TimezoneOffset { get; set; }

        [JsonProperty("isTumblrpayOnboarded")]
        public bool IsTumblrpayOnboarded { get; set; }

        [JsonProperty("theme")]
        public Theme Theme { get; set; }

        [JsonProperty("tumblrmartOrders")]
        public List<object> TumblrmartOrders { get; private set; } = new List<object>();

        [JsonProperty("shouldBlur")]
        public bool ShouldBlur { get; set; }

        [JsonProperty("isAdultLastReporter")]
        public object IsAdultLastReporter { get; set; }

        [JsonProperty("canSubmit")]
        public bool? CanSubmit { get; set; }
    }

    public class CommunityLabelCategories
    {
        [JsonProperty("violence")]
        public string Violence { get; set; }

        [JsonProperty("drugUse")]
        public string DrugUse { get; set; }

        [JsonProperty("sexualThemes")]
        public string SexualThemes { get; set; }
    }

    public class EmailHash
    {
        [JsonProperty("md5")]
        public string Md5 { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("sha1")]
        public string Sha1 { get; set; }
    }

    public class GlobalC
    {
        [JsonProperty("inApp")]
        public bool InApp { get; set; }

        [JsonProperty("pushNotification")]
        public bool PushNotification { get; set; }

        [JsonProperty("web")]
        public bool Web { get; set; }
    }

    public class Notifications
    {
        [JsonProperty("push")]
        public Push Push { get; set; }
    }

    public class Push
    {
        [JsonProperty("marketing")]
        public bool Marketing { get; set; }
    }

    public class Sounds
    {
        [JsonProperty("global")]
        public GlobalC Global { get; set; }
    }

    public class TourGuides
    {
        [JsonProperty("like")]
        public bool Like { get; set; }

        [JsonProperty("follow")]
        public bool Follow { get; set; }

        [JsonProperty("reblog")]
        public bool Reblog { get; set; }

        [JsonProperty("compose")]
        public bool Compose { get; set; }

        [JsonProperty("appearance")]
        public bool Appearance { get; set; }

        [JsonProperty("search")]
        public bool Search { get; set; }
    }

    public class User
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("likes")]
        public int Likes { get; set; }

        [JsonProperty("following")]
        public int Following { get; set; }

        [JsonProperty("defaultPostFormat")]
        public string DefaultPostFormat { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("bornYear")]
        public int BornYear { get; set; }

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("ageVerification")]
        public AgeVerification AgeVerification { get; set; }

        [JsonProperty("savedPostsCount")]
        public int SavedPostsCount { get; set; }

        [JsonProperty("isEmailVerified")]
        public bool IsEmailVerified { get; set; }

        [JsonProperty("safeSearch")]
        public bool SafeSearch { get; set; }

        [JsonProperty("pushNotifications")]
        public bool PushNotifications { get; set; }

        [JsonProperty("tourGuides")]
        public TourGuides TourGuides { get; set; }

        [JsonProperty("crmUuid")]
        public string CrmUuid { get; set; }

        [JsonProperty("adUuid")]
        public string AdUuid { get; set; }

        [JsonProperty("membershipsId")]
        public string MembershipsId { get; set; }

        [JsonProperty("userUuid")]
        public string UserUuid { get; set; }

        [JsonProperty("emailHash")]
        public EmailHash EmailHash { get; set; }

        [JsonProperty("keywords")]
        public List<string> Keywords { get; private set; } = new List<string>();

        [JsonProperty("iabCategories")]
        public List<string> IabCategories { get; private set; } = new List<string>();

        [JsonProperty("notifications")]
        public Notifications Notifications { get; set; }

        [JsonProperty("sounds")]
        public Sounds Sounds { get; set; }

        [JsonProperty("safeMode")]
        public bool SafeMode { get; set; }

        [JsonProperty("canModifySafeMode")]
        public bool CanModifySafeMode { get; set; }

        [JsonProperty("oneid")]
        public int Oneid { get; set; }

        [JsonProperty("isPartial")]
        public bool IsPartial { get; set; }

        [JsonProperty("isPasswordless")]
        public bool IsPasswordless { get; set; }

        [JsonProperty("hasThirdPartyAuth")]
        public bool HasThirdPartyAuth { get; set; }

        [JsonProperty("findByEmail")]
        public bool FindByEmail { get; set; }

        [JsonProperty("showOnlineStatus")]
        public bool ShowOnlineStatus { get; set; }

        [JsonProperty("conversationalNotifications")]
        public bool ConversationalNotifications { get; set; }

        [JsonProperty("hasTumblrPremium")]
        public bool HasTumblrPremium { get; set; }

        [JsonProperty("usedToHaveTumblrPremium")]
        public bool UsedToHaveTumblrPremium { get; set; }

        [JsonProperty("hasTumblrPremiumLight")]
        public bool HasTumblrPremiumLight { get; set; }

        [JsonProperty("usedToHaveTumblrPremiumLight")]
        public bool UsedToHaveTumblrPremiumLight { get; set; }

        [JsonProperty("hasTumblrmartCredit")]
        public bool HasTumblrmartCredit { get; set; }

        [JsonProperty("hasBlazeCredit")]
        public bool HasBlazeCredit { get; set; }

        [JsonProperty("hasDomains")]
        public bool HasDomains { get; set; }

        [JsonProperty("colorizedTags")]
        public bool ColorizedTags { get; set; }

        [JsonProperty("timestamps")]
        public bool Timestamps { get; set; }

        [JsonProperty("swipeTabs")]
        public bool SwipeTabs { get; set; }

        [JsonProperty("viewWithBlogTheme")]
        public bool ViewWithBlogTheme { get; set; }

        [JsonProperty("autoTruncatePosts")]
        public bool AutoTruncatePosts { get; set; }

        [JsonProperty("unopenedGifts")]
        public int UnopenedGifts { get; set; }

        [JsonProperty("unopenedGiftLastRecipient")]
        public object UnopenedGiftLastRecipient { get; set; }

        [JsonProperty("hasActiveSupporterBadgeSubscription")]
        public bool HasActiveSupporterBadgeSubscription { get; set; }

        [JsonProperty("communityLabelVisibilitySetting")]
        public string CommunityLabelVisibilitySetting { get; set; }

        [JsonProperty("communityLabelCategories")]
        public CommunityLabelCategories CommunityLabelCategories { get; set; }

        [JsonProperty("useNewWelcome")]
        public bool UseNewWelcome { get; set; }

        [JsonProperty("hasBlazedBefore")]
        public bool HasBlazedBefore { get; set; }

        [JsonProperty("showAdsAnyway")]
        public bool ShowAdsAnyway { get; set; }

        [JsonProperty("blogs")]
        public List<BlogC> Blogs { get; private set; } = new List<BlogC>();

        [JsonProperty("ownsCustomizedBlogs")]
        public bool OwnsCustomizedBlogs { get; set; }

        [JsonProperty("communities")]
        public List<object> Communities { get; private set; } = new List<object>();
    }
}
