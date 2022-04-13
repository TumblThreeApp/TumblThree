using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using TumblThree.Applications.DataModels.TumblrSvcJson;

namespace TumblThree.Applications.Parser
{
    public class TumblrSvcJsonToJsonParser<T> : ITumblrToTextParser<T> where T : Post
    {
        public string ParseText(T post) => GetPostAsString(post);

        public string ParseQuote(T post) => GetPostAsString(post);

        public string ParseLink(T post) => GetPostAsString(post);

        public string ParseConversation(T post) => GetPostAsString(post);

        public string ParseAnswer(T post) => GetPostAsString(post);

        public string ParsePhotoMeta(T post) => GetPostAsString(post);

        public string ParseVideoMeta(T post) => GetPostAsString(post);

        public string ParseAudioMeta(T post) => GetPostAsString(post);

        private static string GetPostAsString(T post)
        {
            var postCopy = (Post)post.Clone();

            var meta = FillMetaDataObject(postCopy);

            var serializer = new DataContractJsonSerializer(meta.GetType());

            using (var ms = new MemoryStream())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(
                    ms, Encoding.UTF8, false, true, "  "))
                {
                    serializer.WriteObject(writer, meta);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static MetaDataSvc FillMetaDataObject(Post post)
        {
            var md = new MetaDataSvc
            {
                Date = post.Date,
                Format = post.Format,
                Id = post.Id,
                Slug = post.Slug,
                Tags = post.Tags,
                Type = post.Type,
                Timestamp = post.Timestamp,
                PostHtml = post.PostHtml,
                PostUrl = post.PostUrl,
                PostedOnTooltip = post.PostedOnTooltip,
                Summary = post.Summary,

                ReblogKey = post.ReblogKey
            };

            if (!string.IsNullOrEmpty(post.RebloggedFromName)) md.RebloggedFromName = post.RebloggedFromName;
            if (!string.IsNullOrEmpty(post.RebloggedFromTitle)) md.RebloggedFromTitle = post.RebloggedFromTitle;
            if (!string.IsNullOrEmpty(post.RebloggedFromUrl)) md.RebloggedFromUrl = post.RebloggedFromUrl;
            if (!string.IsNullOrEmpty(post.RebloggedRootName)) md.RebloggedRootName = post.RebloggedRootName;
            if (!string.IsNullOrEmpty(post.RebloggedRootTitle)) md.RebloggedRootTitle = post.RebloggedRootTitle;
            if (!string.IsNullOrEmpty(post.RebloggedRootUrl)) md.RebloggedRootUrl = post.RebloggedRootUrl;

            if (post.Type == "text")
            {
                md.Body = post.Body;
                md.Title = post.Title;

                md.DownloadedMediaFiles = (post.DownloadedFilenames?.Count > 0) ? post.DownloadedFilenames : null;
            }
            if (post.Type == "audio")
            {
                md.Caption = post.Caption;
                md.AudioSourceUrl = post.AudioSourceUrl;
                md.AudioType = post.AudioType;
                md.AudioUrl = post.AudioUrl;

                md.Artist = post.Artist;
                md.Track = post.Track;
                md.TrackName = post.TrackName;
                md.Album = post.Album;
                md.AlbumArt = post.AlbumArt;
                md.Year = post.Year;

                md.DownloadedMediaFiles = post.DownloadedFilenames;
            }
            else if (post.Type == "photo")
            {
                md.Caption = post.Caption;
                md.PhotosetLayout = post.PhotosetLayout;
                md.PhotosetPhotos = post.PhotosetPhotos;
                md.Photos = post.Photos;

                md.DownloadedMediaFiles = post.DownloadedFilenames;
            }
            else if (post.Type == "video")
            {
                md.Video = post.Video;
                md.VideoType = post.VideoType;
                md.VideoUrl = post.VideoUrl;
                md.Caption = post.Caption;

                md.DownloadedMediaFiles = post.DownloadedFilenames;
            }
            else if (post.Type == "answer")
            {
                md.Answer = post.Answer;
                md.Question = post.Question;
                md.AskingName = post.AskingName;
                md.AskingUrl = post.AskingUrl;
            }
            else if (post.Type == "quote")
            {
                md.Text = post.Text;
                md.Source = post.Source;
                md.SourceTitle = post.SourceTitle;
                md.SourceUrl = post.SourceUrl;
            }
            else if (post.Type == "chat")
            {
                md.Title = post.Title;
                md.Body = post.Body;
                md.dialogue = post.dialogue;
            }
            else if (post.Type == "link")
            {
                md.LinkAuthor = post.LinkAuthor;
                md.LinkImage = post.LinkImage;
                md.LinkUrl = post.Url;

                md.Title = post.Title;
                md.Description = post.Description;
                md.Excerpt = post.Excerpt;
            }

            return md;
        }
    }
}
