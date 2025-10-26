using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrSvcJson2.BlogInfo
{
    public class BlogInfo
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("response")]
        public Response Response { get; set; }
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
        public List<object> Accessories { get; } = new List<object>();
    }

    public class Blog
    {
        [JsonProperty("ask")]
        public bool Ask { get; set; }

        [JsonProperty("ask_anon")]
        public bool AskAnon { get; set; }

        [JsonProperty("ask_page_title")]
        public string AskPageTitle { get; set; }

        [JsonProperty("asks_allow_media")]
        public bool AsksAllowMedia { get; set; }

        [JsonProperty("avatar")]
        public List<Avatar> Avatar { get; } = new List<Avatar>();

        [JsonProperty("blog_view_url")]
        public string BlogViewUrl { get; set; }

        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("can_chat")]
        public bool CanChat { get; set; }

        [JsonProperty("can_send_fan_mail")]
        public bool CanSendFanMail { get; set; }

        [JsonProperty("can_submit")]
        public bool CanSubmit { get; set; }

        [JsonProperty("can_subscribe")]
        public bool CanSubscribe { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("followed")]
        public bool Followed { get; set; }

        [JsonProperty("is_adult")]
        public bool IsAdult { get; set; }

        [JsonProperty("is_blocked_from_primary")]
        public bool IsBlockedFromPrimary { get; set; }

        [JsonProperty("is_group_channel")]
        public bool IsGroupChannel { get; set; }

        [JsonProperty("is_nsfw")]
        public bool IsNsfw { get; set; }

        [JsonProperty("is_private_channel")]
        public bool IsPrivateChannel { get; set; }

        [JsonProperty("likes")]
        public int Likes { get; set; }

        [JsonProperty("linked_accounts")]
        public List<object> LinkedAccounts { get; } = new List<object>();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("placement_id")]
        public string PlacementId { get; set; }

        [JsonProperty("posts")]
        public int Posts { get; set; }

        [JsonProperty("random_name")]
        public bool RandomName { get; set; }

        [JsonProperty("reply_conditions")]
        public string ReplyConditions { get; set; }

        [JsonProperty("seconds_since_last_activity")]
        public int SecondsSinceLastActivity { get; set; }

        [JsonProperty("share_following")]
        public bool ShareFollowing { get; set; }

        [JsonProperty("share_likes")]
        public bool ShareLikes { get; set; }

        [JsonProperty("show_author_avatar")]
        public bool ShowAuthorAvatar { get; set; }

        [JsonProperty("show_top_posts")]
        public bool ShowTopPosts { get; set; }

        [JsonProperty("submission_page_title")]
        public string SubmissionPageTitle { get; set; }

        [JsonProperty("submission_terms")]
        public SubmissionTerms SubmissionTerms { get; set; }

        [JsonProperty("subscribed")]
        public bool Subscribed { get; set; }

        [JsonProperty("theme_id")]
        public int ThemeId { get; set; }

        [JsonProperty("theme")]
        public Theme Theme { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("total_posts")]
        public int TotalPosts { get; set; }

        [JsonProperty("updated")]
        public int Updated { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }
    }

    public class Meta
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

    public class Response
    {
        [JsonProperty("blog")]
        public Blog Blog { get; set; }
    }

    public class SubmissionTerms
    {
        [JsonProperty("accepted_types")]
        public List<string> AcceptedTypes { get; } = new List<string>();

        [JsonProperty("tags")]
        public List<string> Tags { get; } = new List<string>();

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("guidelines")]
        public string Guidelines { get; set; }
    }

    public class Theme
    {
        [JsonProperty("header_full_width")]
        public int HeaderFullWidth { get; set; }

        [JsonProperty("header_full_height")]
        public int HeaderFullHeight { get; set; }

        [JsonProperty("header_focus_width")]
        public int HeaderFocusWidth { get; set; }

        [JsonProperty("header_focus_height")]
        public int HeaderFocusHeight { get; set; }

        [JsonProperty("avatar_shape")]
        public string AvatarShape { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("body_font")]
        public string BodyFont { get; set; }

        [JsonProperty("header_bounds")]
        public string HeaderBounds { get; set; }

        [JsonProperty("header_image")]
        public string HeaderImage { get; set; }

        [JsonProperty("header_image_focused")]
        public string HeaderImageFocused { get; set; }

        [JsonProperty("header_image_poster")]
        public string HeaderImagePoster { get; set; }

        [JsonProperty("header_image_scaled")]
        public string HeaderImageScaled { get; set; }

        [JsonProperty("header_stretch")]
        public bool HeaderStretch { get; set; }

        [JsonProperty("link_color")]
        public string LinkColor { get; set; }

        [JsonProperty("show_avatar")]
        public bool ShowAvatar { get; set; }

        [JsonProperty("show_description")]
        public bool ShowDescription { get; set; }

        [JsonProperty("show_header_image")]
        public bool ShowHeaderImage { get; set; }

        [JsonProperty("show_title")]
        public bool ShowTitle { get; set; }

        [JsonProperty("title_color")]
        public string TitleColor { get; set; }

        [JsonProperty("title_font")]
        public string TitleFont { get; set; }

        [JsonProperty("title_font_weight")]
        public string TitleFontWeight { get; set; }
    }
}
