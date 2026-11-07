using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    // DataPages myDeserializedClass = JsonConvert.DeserializeObject<DataPages>(myJsonResponse);

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
