using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.Bluesky
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    public class Associated
    {
        [JsonProperty("chat", NullValueHandling = NullValueHandling.Ignore)]
        public Chat Chat { get; set; }
    }

    public class Author
    {
        [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
        public string Did { get; set; }

        [JsonProperty("handle", NullValueHandling = NullValueHandling.Ignore)]
        public string Handle { get; set; }

        [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        [JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string Avatar { get; set; }

        [JsonProperty("associated", NullValueHandling = NullValueHandling.Ignore)]
        public Associated Associated { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<Label> Labels { get; } = new List<Label>();

        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedAt { get; set; }
    }

    public class Chat
    {
        [JsonProperty("allowIncoming", NullValueHandling = NullValueHandling.Ignore)]
        public string AllowIncoming { get; set; }
    }

    //public class Post
    //{
    //    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    //    public string Uri { get; set; }

    //    [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
    //    public string Cid { get; set; }

    //    [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
    //    public Author Author { get; set; }

    //    [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
    //    public Record Record { get; set; }

    //    [JsonProperty("replyCount", NullValueHandling = NullValueHandling.Ignore)]
    //    public int ReplyCount { get; set; }

    //    [JsonProperty("repostCount", NullValueHandling = NullValueHandling.Ignore)]
    //    public int RepostCount { get; set; }

    //    [JsonProperty("likeCount", NullValueHandling = NullValueHandling.Ignore)]
    //    public int LikeCount { get; set; }

    //    [JsonProperty("quoteCount", NullValueHandling = NullValueHandling.Ignore)]
    //    public int QuoteCount { get; set; }

    //    [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
    //    public DateTime IndexedAt { get; set; }

    //    [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
    //    public List<object> Labels { get; set; }
    //}

    public class Record
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public Embed Embed { get; set; }

        [JsonProperty("embeds", NullValueHandling = NullValueHandling.Ignore)]
        public List<Embed> Embeds { get; } = new List<Embed>();

        [JsonProperty("facets", NullValueHandling = NullValueHandling.Ignore)]
        public List<Facet> Facets { get; } = new List<Facet>();

        [JsonProperty("langs", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Langs { get; } = new List<string>();

        [JsonProperty("reply", NullValueHandling = NullValueHandling.Ignore)]
        public Reply Reply { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }

    public class PostsList
    {
        [JsonProperty("posts", NullValueHandling = NullValueHandling.Ignore)]
        public List<Post> Posts { get; } = new List<Post>();
    }
}
