using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TumblThree.Applications.DataModels.TumblrSvcJson
{
    [DataContract]
    public class MetaDataSvc
    {
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        public int Timestamp { get; set; }

        [DataMember(Name = "format", EmitDefaultValue = false)]
        public string Format { get; set; }

        [DataMember(Name = "reblog_key", EmitDefaultValue = false)]
        public string ReblogKey { get; set; }

        [DataMember(Name = "caption", EmitDefaultValue = false)]
        public string Caption { get; set; }

        [DataMember(Name = "slug", EmitDefaultValue = false)]
        public string Slug { get; set; }

        [DataMember(Name = "reblogged_from_id", EmitDefaultValue = false)]
        public string RebloggedFromId { get; set; }

        [DataMember(Name = "reblogged_from_url", EmitDefaultValue = false)]
        public string RebloggedFromUrl { get; set; }

        [DataMember(Name = "reblogged_from_name", EmitDefaultValue = false)]
        public string RebloggedFromName { get; set; }

        [DataMember(Name = "reblogged_from_title", EmitDefaultValue = false)]
        public string RebloggedFromTitle { get; set; }

        [DataMember(Name = "reblogged_from_uuid", EmitDefaultValue = false)]
        public string RebloggedFromUuid { get; set; }

        [DataMember(Name = "reblogged_root_id", EmitDefaultValue = false)]
        public string RebloggedRootId { get; set; }

        [DataMember(Name = "reblogged_root_url", EmitDefaultValue = false)]
        public string RebloggedRootUrl { get; set; }

        [DataMember(Name = "reblogged_root_name", EmitDefaultValue = false)]
        public string RebloggedRootName { get; set; }

        [DataMember(Name = "reblogged_root_title", EmitDefaultValue = false)]
        public string RebloggedRootTitle { get; set; }

        [DataMember(Name = "reblogged_root_uuid", EmitDefaultValue = false)]
        public string RebloggedRootUuid { get; set; }

        [DataMember(Name = "tags", EmitDefaultValue = false)]
        public List<string> Tags { get; set; }

        [DataMember(Name = "summary", EmitDefaultValue = false)]
        public string Summary { get; set; }

        [DataMember(Name = "photos", EmitDefaultValue = false)]
        public List<Photo> Photos { get; set; }

        [DataMember(Name = "photoset_layout", EmitDefaultValue = false)]
        public string PhotosetLayout { get; set; }

        [DataMember(Name = "photoset_photos", EmitDefaultValue = false)]
        public List<PhotosetPhoto> PhotosetPhotos { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false)]
        public string Title { get; set; }

        [DataMember(Name = "body", EmitDefaultValue = false)]
        public string Body { get; set; }

        [DataMember(Name = "text", EmitDefaultValue = false)]
        public string Text { get; set; }

        [DataMember(Name = "source", EmitDefaultValue = false)]
        public string Source { get; set; }

        [DataMember(Name = "source_url", EmitDefaultValue = false)]
        public string SourceUrl { get; set; }

        [DataMember(Name = "source_title", EmitDefaultValue = false)]
        public string SourceTitle { get; set; }

        [DataMember(Name = "post_html", EmitDefaultValue = false)]
        public string PostHtml { get; set; }

        [DataMember(Name = "link_url", EmitDefaultValue = false)]
        public string LinkUrl { get; set; }

        [DataMember(Name = "link_image", EmitDefaultValue = false)]
        public string LinkImage { get; set; }

        [DataMember(Name = "link_author", EmitDefaultValue = false)]
        public object LinkAuthor { get; set; }

        [DataMember(Name = "excerpt", EmitDefaultValue = false)]
        public string Excerpt { get; set; }

        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "dialogue", EmitDefaultValue = false)]
        public List<Dialogue> dialogue { get; set; }

        [DataMember(Name = "question", EmitDefaultValue = false)]
        public string Question { get; set; }

        [DataMember(Name = "answer", EmitDefaultValue = false)]
        public string Answer { get; set; }

        [DataMember(Name = "asking_name", EmitDefaultValue = false)]
        public string AskingName { get; set; }

        [DataMember(Name = "asking_url", EmitDefaultValue = false)]
        public string AskingUrl { get; set; }

        [DataMember(Name = "audio_url", EmitDefaultValue = false)]
        public string AudioUrl { get; set; }

        [DataMember(Name = "post_url", EmitDefaultValue = false)]
        public string PostUrl { get; set; }

        [DataMember(Name = "posted_on_tooltip", EmitDefaultValue = false)]
        public string PostedOnTooltip { get; set; }

        [DataMember(Name = "audio_source_url", EmitDefaultValue = false)]
        public string AudioSourceUrl { get; set; }

        [DataMember(Name = "audio_type", EmitDefaultValue = false)]
        public string AudioType { get; set; }

        [DataMember(Name = "album", EmitDefaultValue = false)]
        public string Album { get; set; }

        [DataMember(Name = "artist", EmitDefaultValue = false)]
        public string Artist { get; set; }

        [DataMember(Name = "track_name", EmitDefaultValue = false)]
        public string TrackName { get; set; }

        [DataMember(Name = "album_art", EmitDefaultValue = false)]
        public string AlbumArt { get; set; }

        [DataMember(Name = "year", EmitDefaultValue = false)]
        public int? Year { get; set; }

        [DataMember(Name = "track", EmitDefaultValue = false)]
        public string Track { get; set; }

        [DataMember(Name = "video", EmitDefaultValue = false)]
        public Video Video { get; set; }

        [DataMember(Name = "video_type", EmitDefaultValue = false)]
        public string VideoType { get; set; }

        [DataMember(Name = "video_url", EmitDefaultValue = false)]
        public string VideoUrl { get; set; }

        [DataMember(Name = "downloaded_media_files", EmitDefaultValue = false)]
        public List<string> DownloadedMediaFiles { get; set; }
    }
}
