using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    // DataPages myDeserializedClass = JsonConvert.DeserializeObject<DataPages>(myJsonResponse);

    public class AuthorBlog
    {
        [JsonProperty("avatar")]
        public List<Avatar> Avatar { get; private set; } = new List<Avatar>();

        [JsonProperty("blogViewUrl")]
        public string BlogViewUrl { get; set; }

        [JsonProperty("canBeFollowed")]
        public bool CanBeFollowed { get; set; }

        [JsonProperty("canShowBadges")]
        public bool CanShowBadges { get; set; }

        [JsonProperty("canSubscribe")]
        public bool CanSubscribe { get; set; }

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

        [JsonProperty("subscribed")]
        public bool Subscribed { get; set; }

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

        [JsonProperty("isBlockedFromPrimary")]
        public bool IsBlockedFromPrimary { get; set; }

        [JsonProperty("isPasswordProtected")]
        public bool IsPasswordProtected { get; set; }

        [JsonProperty("shareFollowing")]
        public bool ShareFollowing { get; set; }

        [JsonProperty("shareLikes")]
        public bool ShareLikes { get; set; }

        [JsonProperty("shareReplies")]
        public bool ShareReplies { get; set; }

        [JsonProperty("shouldBlur")]
        public bool ShouldBlur { get; set; }

        [JsonProperty("isAdultLastReporter")]
        public object IsAdultLastReporter { get; set; }

        [JsonProperty("isAdmin")]
        public bool IsAdmin { get; set; }
    }

    public class DataPages
    {
        [JsonProperty("pages")]
        public List<Page> Pages { get; private set; } = new List<Page>();

        [JsonProperty("pageParams")]
        public List<object> PageParams { get; private set; } = new List<object>();
    }

    public class Emoji
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("grapheme")]
        public string Grapheme { get; set; }

        [JsonProperty("baseGrapheme")]
        public string BaseGrapheme { get; set; }

        [JsonProperty("baseSlug")]
        public string BaseSlug { get; set; }

        [JsonProperty("variants")]
        public List<object> Variants { get; private set; } = new List<object>();
    }

    public class Facepile
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Page
    {
        [JsonProperty("items")]
        public List<Post> Items { get; private set; } = new List<Post>();

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("nextLink")]
        public string NextLink { get; set; }
    }

    public class PostData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }

        [JsonProperty("isCommercial")]
        public bool IsCommercial { get; set; }
    }

    public class PostAuthorAvatar
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

    public class Reaction
    {
        [JsonProperty("emoji")]
        public Emoji Emoji { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("preview")]
        public List<string> Preview { get; private set; } = new List<string>();

        [JsonProperty("reacted")]
        public bool Reacted { get; set; }
    }

    public class TrailC
    {
        [JsonProperty("content")]
        public List<Content> Content { get; private set; } = new List<Content>();

        [JsonProperty("layout")]
        public List<object> Layout { get; private set; } = new List<object>();

        [JsonProperty("brokenBlog", NullValueHandling = NullValueHandling.Ignore)]
        public Blog BrokenBlog { get; set; }

        [JsonProperty("post")]
        public PostData Post { get; set; }

        [JsonProperty("blog", NullValueHandling = NullValueHandling.Ignore)]
        public Blog Blog { get; set; }
    }
}
