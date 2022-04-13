using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrApiJson
{
    [DataContract]
    public class MetaDataApi
    {
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "url-with-slug", EmitDefaultValue = false)]
        public string UrlWithSlug { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "date-gmt", EmitDefaultValue = false)]
        public string DateGmt { get; set; }

        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        [DataMember(Name = "unix-timestamp", EmitDefaultValue = false)]
        public int UnixTimestamp { get; set; }

        [DataMember(Name = "format", EmitDefaultValue = false)]
        public string Format { get; set; }

        [DataMember(Name = "reblog-key", EmitDefaultValue = false)]
        public string ReblogKey { get; set; }

        [DataMember(Name = "slug", EmitDefaultValue = false)]
        public string Slug { get; set; }

        [DataMember(Name = "reblogged-from-url", EmitDefaultValue = false)]
        public string RebloggedFromUrl { get; set; }

        [DataMember(Name = "reblogged-from-name", EmitDefaultValue = false)]
        public string RebloggedFromName { get; set; }

        [DataMember(Name = "reblogged-from-title", EmitDefaultValue = false)]
        public string RebloggedFromTitle { get; set; }

        [DataMember(Name = "reblogged-root-url", EmitDefaultValue = false)]
        public string RebloggedRootUrl { get; set; }

        [DataMember(Name = "reblogged-root-name", EmitDefaultValue = false)]
        public string RebloggedRootName { get; set; }

        [DataMember(Name = "reblogged-root-title", EmitDefaultValue = false)]
        public string RebloggedRootTitle { get; set; }

        [DataMember(Name = "quote-text", EmitDefaultValue = false)]
        public string QuoteText { get; set; }

        [DataMember(Name = "quote-source", EmitDefaultValue = false)]
        public string QuoteSource { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public List<string> Tags { get; set; }

        [DataMember(Name = "photo-caption", EmitDefaultValue = false)]
        public string PhotoCaption { get; set; }

        [DataMember(Name = "photo-link-url", EmitDefaultValue = false)]
        public string PhotoLinkUrl { get; set; }

        [DataMember(Name = "photo-url-1280", EmitDefaultValue = false)]
        public string PhotoUrl1280 { get; set; }

        [DataMember(Name = "photo-url-500", EmitDefaultValue = false)]
        public string PhotoUrl500 { get; set; }

        [DataMember(Name = "photo-url-400", EmitDefaultValue = false)]
        public string PhotoUrl400 { get; set; }

        [DataMember(Name = "photo-url-250", EmitDefaultValue = false)]
        public string PhotoUrl250 { get; set; }

        [DataMember(Name = "photo-url-100", EmitDefaultValue = false)]
        public string PhotoUrl100 { get; set; }

        [DataMember(Name = "photo-url-75", EmitDefaultValue = false)]
        public string PhotoUrl75 { get; set; }

        [DataMember(Name = "photos", EmitDefaultValue = false)]
        public List<Photo> Photos { get; set; }

        [DataMember(Name = "photo-url", EmitDefaultValue = false)]
        public string PhotoUrl { get; set; }

        [DataMember(Name = "photoset-urls", EmitDefaultValue = false)]
        public List<string> PhotosetUrls { get; set; }

        [DataMember(Name = "id3-artist", EmitDefaultValue = false)]
        public string Id3Artist { get; set; }

        [DataMember(Name = "id3-album", EmitDefaultValue = false)]
        public string Id3Album { get; set; }

        [DataMember(Name = "id3-year", EmitDefaultValue = false)]
        public string Id3Year { get; set; }

        [DataMember(Name = "id3-track", EmitDefaultValue = false)]
        public string Id3Track { get; set; }

        [DataMember(Name = "id3-title", EmitDefaultValue = false)]
        public string Id3Title { get; set; }

        [DataMember(Name = "audio-caption", EmitDefaultValue = false)]
        public string AudioCaption { get; set; }

        [DataMember(Name = "audio-player", EmitDefaultValue = false)]
        public string AudioPlayer { get; set; }

        [DataMember(Name = "audio-embed", EmitDefaultValue = false)]
        public string AudioEmbed { get; set; }

        [DataMember(Name = "audio-plays", EmitDefaultValue = false)]
        public int? AudioPlays { get; set; }

        [DataMember(Name = "regular-title", EmitDefaultValue = false)]
        public string RegularTitle { get; set; }

        [DataMember(Name = "regular-body", EmitDefaultValue = false)]
        public string RegularBody { get; set; }

        [DataMember(Name = "link-text", EmitDefaultValue = false)]
        public string LinkText { get; set; }

        [DataMember(Name = "link-url", EmitDefaultValue = false)]
        public string LinkUrl { get; set; }

        [DataMember(Name = "link-description", EmitDefaultValue = false)]
        public string LinkDescription { get; set; }

        [DataMember(Name = "conversation-title", EmitDefaultValue = false)]
        public string ConversationTitle { get; set; }

        [DataMember(Name = "conversation-text", EmitDefaultValue = false)]
        public string ConversationText { get; set; }

        [DataMember(Name = "video-caption", EmitDefaultValue = false)]
        public string VideoCaption { get; set; }

        [DataMember(Name = "video-source", EmitDefaultValue = false)]
        public string VideoSource { get; set; }

        [DataMember(Name = "video-player", EmitDefaultValue = false)]
        public string VideoPlayer { get; set; }

        [DataMember(Name = "video-player-500", EmitDefaultValue = false)]
        public string VideoPlayer500 { get; set; }

        [DataMember(Name = "video-player-250", EmitDefaultValue = false)]
        public string VideoPlayer250 { get; set; }

        [DataMember(Name = "conversation", EmitDefaultValue = false)]
        public List<Conversation> Conversation { get; set; }

        [DataMember(Name = "question", EmitDefaultValue = false)]
        public string Question { get; set; }

        [DataMember(Name = "answer", EmitDefaultValue = false)]
        public string Answer { get; set; }

        [DataMember(Name = "downloaded-media-files", EmitDefaultValue = false)]
        public List<string> DownloadedMediaFiles { get; set; }
    }
}
