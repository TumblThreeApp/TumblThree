using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TumblThree.Applications.Converter;

namespace TumblThree.Applications.DataModels.Bluesky
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    public class AspectRatio
    {
        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int Height { get; set; }

        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public int Width { get; set; }
    }

    //public class Associated
    //{
    //    [JsonProperty("chat", NullValueHandling = NullValueHandling.Ignore)]
    //    public Chat Chat { get; set; }
    //}

    //public class Author
    //{
    //    [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
    //    public string Did { get; set; }

    //    [JsonProperty("handle", NullValueHandling = NullValueHandling.Ignore)]
    //    public string Handle { get; set; }

    //    [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
    //    public string DisplayName { get; set; }

    //    [JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
    //    public string Avatar { get; set; }

    //    [JsonProperty("associated", NullValueHandling = NullValueHandling.Ignore)]
    //    public Associated Associated { get; set; }

    //    [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
    //    public List<object> Labels { get; set; }

    //    [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
    //    public DateTime CreatedAt { get; set; }
    //}

    public class By
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

    //public class Chat
    //{
    //    [JsonProperty("allowIncoming", NullValueHandling = NullValueHandling.Ignore)]
    //    public string AllowIncoming { get; set; }
    //}

    public class EmbededMedia
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("images", NullValueHandling = NullValueHandling.Ignore)]
        public List<ImageItem> Images { get; } = new List<ImageItem>();
    }

    public class Embed
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("media", NullValueHandling = NullValueHandling.Ignore)]
        public EmbededMedia Media { get; set; }

        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
        public Record Record { get; set; }

        [JsonProperty("images", NullValueHandling = NullValueHandling.Ignore)]
        public List<ImageItem> Images { get; } = new List<ImageItem>();

        [JsonProperty("aspectRatio", NullValueHandling = NullValueHandling.Ignore)]
        public AspectRatio AspectRatio { get; set; }

        [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
        public Video Video { get; set; }

        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("playlist", NullValueHandling = NullValueHandling.Ignore)]
        public string Playlist { get; set; }

        [JsonProperty("thumbnail", NullValueHandling = NullValueHandling.Ignore)]
        public string Thumbnail { get; set; }

        [JsonProperty("external", NullValueHandling = NullValueHandling.Ignore)]
        public External External { get; set; }
    }

    public class External
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("thumb", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ObjectOrStringConverter<Thumb>))]
        public object Thumb { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }
    }

    public class Facet
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("features", NullValueHandling = NullValueHandling.Ignore)]
        public List<Feature> Features { get; } = new List<Feature>();

        [JsonProperty("index", NullValueHandling = NullValueHandling.Ignore)]
        public Index Index { get; set; }
    }

    public class Feature
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
        public string Did { get; set; }

        [JsonProperty("tag", NullValueHandling = NullValueHandling.Ignore)]
        public string Tag { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }
    }

    public class FeedEntry
    {
        [JsonProperty("post", NullValueHandling = NullValueHandling.Ignore)]
        public Post Post { get; set; }

        [JsonProperty("reply", NullValueHandling = NullValueHandling.Ignore)]
        public Reply Reply { get; set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public Reason Reason { get; set; }
    }

    public class GrandparentAuthor
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

    public class ImageItem
    {
        [JsonProperty("alt", NullValueHandling = NullValueHandling.Ignore)]
        public string Alt { get; set; }

        [JsonProperty("aspectRatio", NullValueHandling = NullValueHandling.Ignore)]
        public AspectRatio AspectRatio { get; set; }

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public Image11 Image { get; set; }

        [JsonProperty("thumb", NullValueHandling = NullValueHandling.Ignore)]
        public string Thumb { get; set; }

        [JsonProperty("fullsize", NullValueHandling = NullValueHandling.Ignore)]
        public string Fullsize { get; set; }
    }

    public class Image11
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("ref", NullValueHandling = NullValueHandling.Ignore)]
        public Ref Ref { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int Size { get; set; }
    }

    public class Index
    {
        [JsonProperty("byteEnd", NullValueHandling = NullValueHandling.Ignore)]
        public int ByteEnd { get; set; }

        [JsonProperty("byteStart", NullValueHandling = NullValueHandling.Ignore)]
        public int ByteStart { get; set; }
    }

    public class Parent
    {
        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public Author Author { get; set; }

        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
        public Record Record { get; set; }

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public Embed Embed { get; set; }

        [JsonProperty("replyCount", NullValueHandling = NullValueHandling.Ignore)]
        public int ReplyCount { get; set; }

        [JsonProperty("repostCount", NullValueHandling = NullValueHandling.Ignore)]
        public int RepostCount { get; set; }

        [JsonProperty("likeCount", NullValueHandling = NullValueHandling.Ignore)]
        public int LikeCount { get; set; }

        [JsonProperty("quoteCount", NullValueHandling = NullValueHandling.Ignore)]
        public int QuoteCount { get; set; }

        [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IndexedAt { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<Label> Labels { get; } = new List<Label>();

        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }

    public class Post
    {
        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public Author Author { get; set; }

        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
        public Record Record { get; set; }

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public Embed Embed { get; set; }

        [JsonProperty("replyCount", NullValueHandling = NullValueHandling.Ignore)]
        public int ReplyCount { get; set; }

        [JsonProperty("repostCount", NullValueHandling = NullValueHandling.Ignore)]
        public int RepostCount { get; set; }

        [JsonProperty("likeCount", NullValueHandling = NullValueHandling.Ignore)]
        public int LikeCount { get; set; }

        [JsonProperty("quoteCount", NullValueHandling = NullValueHandling.Ignore)]
        public int QuoteCount { get; set; }

        [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IndexedAt { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<Label> Labels { get; } = new List<Label>();
    }

    public class Label
    {
        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("cts", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Cts { get; set; }

        [JsonProperty("src", NullValueHandling = NullValueHandling.Ignore)]
        public string Src { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("val", NullValueHandling = NullValueHandling.Ignore)]
        public string Val { get; set; }

        [JsonProperty("ver", NullValueHandling = NullValueHandling.Ignore)]
        public int Ver { get; set; }

        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("values", NullValueHandling = NullValueHandling.Ignore)]
        public List<Value2> Values { get; } = new List<Value2>();
    }

    public class Labels
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("values", NullValueHandling = NullValueHandling.Ignore)]
        public List<Value2> Values { get; } = new List<Value2>();
    }

    public class Reason
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("by", NullValueHandling = NullValueHandling.Ignore)]
        public By By { get; set; }

        [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IndexedAt { get; set; }
    }

    public class AuthorRecord
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public Embed Embed { get; set; }

        [JsonProperty("langs", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Langs { get; } = new List<string>();

        [JsonProperty("reply", NullValueHandling = NullValueHandling.Ignore)]
        public Reply Reply { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("facets", NullValueHandling = NullValueHandling.Ignore)]
        public List<Facet> Facets { get; } = new List<Facet>();

        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public Author Author { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public Value Value { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public Labels Labels { get; set; }

        [JsonProperty("likeCount", NullValueHandling = NullValueHandling.Ignore)]
        public int LikeCount { get; set; }

        [JsonProperty("replyCount", NullValueHandling = NullValueHandling.Ignore)]
        public int ReplyCount { get; set; }

        [JsonProperty("repostCount", NullValueHandling = NullValueHandling.Ignore)]
        public int RepostCount { get; set; }

        [JsonProperty("quoteCount", NullValueHandling = NullValueHandling.Ignore)]
        public int QuoteCount { get; set; }

        [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IndexedAt { get; set; }

        [JsonProperty("embeds", NullValueHandling = NullValueHandling.Ignore)]
        public List<Embed> Embeds { get; } = new List<Embed>();
    }

    public class Ref
    {
        [JsonProperty("$link", NullValueHandling = NullValueHandling.Ignore)]
        public string Link { get; set; }
    }

    public class Reply
    {
        [JsonProperty("parent", NullValueHandling = NullValueHandling.Ignore)]
        public Parent Parent { get; set; }

        [JsonProperty("root", NullValueHandling = NullValueHandling.Ignore)]
        public Root2 Root { get; set; }

        [JsonProperty("grandparentAuthor", NullValueHandling = NullValueHandling.Ignore)]
        public GrandparentAuthor GrandparentAuthor { get; set; }
    }

    public class AuthorFeed
    {
        [JsonProperty("feed", NullValueHandling = NullValueHandling.Ignore)]
        public List<FeedEntry> FeedEntries { get; } = new List<FeedEntry>();

        [JsonProperty("cursor", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Cursor { get; set; }
    }

    public class Root2
    {
        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("author", NullValueHandling = NullValueHandling.Ignore)]
        public Author Author { get; set; }

        [JsonProperty("record", NullValueHandling = NullValueHandling.Ignore)]
        public AuthorRecord Record { get; set; }

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public Embed Embed { get; set; }

        [JsonProperty("replyCount", NullValueHandling = NullValueHandling.Ignore)]
        public int ReplyCount { get; set; }

        [JsonProperty("repostCount", NullValueHandling = NullValueHandling.Ignore)]
        public int RepostCount { get; set; }

        [JsonProperty("likeCount", NullValueHandling = NullValueHandling.Ignore)]
        public int LikeCount { get; set; }

        [JsonProperty("quoteCount", NullValueHandling = NullValueHandling.Ignore)]
        public int QuoteCount { get; set; }

        [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IndexedAt { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<Label> Labels { get; } = new List<Label>();

        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }

    public class Thumb
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("ref", NullValueHandling = NullValueHandling.Ignore)]
        public Ref Ref { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int Size { get; set; }
    }

    public class Value
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public Embed Embed { get; set; }

        [JsonProperty("facets", NullValueHandling = NullValueHandling.Ignore)]
        public List<Facet> Facets { get; } = new List<Facet>();

        [JsonProperty("langs", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Langs { get; } = new List<string>();

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }

    public class Value2
    {
        [JsonProperty("val")]
        public string Val { get; set; }
    }

    public class Video
    {
        [JsonProperty("$type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("ref", NullValueHandling = NullValueHandling.Ignore)]
        public Ref Ref { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public int Size { get; set; }
    }
}
