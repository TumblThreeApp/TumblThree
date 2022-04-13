using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

using TumblThree.Applications.DataModels.TumblrApiJson;

namespace TumblThree.Applications.Parser
{
    public class TumblrApiJsonToJsonParser<T> : ITumblrToTextParser<T> where T : Post
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

        private static MetaDataApi FillMetaDataObject(Post post)
        {
            var md = new MetaDataApi
            {
                Date = post.Date,
                DateGmt = post.DateGmt,
                Format = post.Format,
                Id = post.Id,
                Slug = post.Slug,
                Tags = post.Tags,
                Type = post.Type,
                UnixTimestamp = post.UnixTimestamp,
                Url = post.Url,
                UrlWithSlug = post.UrlWithSlug,

                ReblogKey = post.ReblogKey
            };

            if (!string.IsNullOrEmpty(post.RebloggedFromName)) md.RebloggedFromName = post.RebloggedFromName;
            if (!string.IsNullOrEmpty(post.RebloggedFromTitle)) md.RebloggedFromTitle = post.RebloggedFromTitle;
            if (!string.IsNullOrEmpty(post.RebloggedFromUrl)) md.RebloggedFromUrl = post.RebloggedFromUrl;
            if (!string.IsNullOrEmpty(post.RebloggedRootName)) md.RebloggedRootName = post.RebloggedRootName;
            if (!string.IsNullOrEmpty(post.RebloggedRootTitle)) md.RebloggedRootTitle = post.RebloggedRootTitle;
            if (!string.IsNullOrEmpty(post.RebloggedRootUrl)) md.RebloggedRootUrl = post.RebloggedRootUrl;

            if (post.Type == "regular")
            {
                md.RegularBody = post.RegularBody;
                md.RegularTitle = post.RegularTitle;

                md.DownloadedMediaFiles = (post.DownloadedFilenames?.Count > 0) ? post.DownloadedFilenames : null;
            }
            else if (post.Type == "audio")
            {
                md.AudioCaption = post.AudioCaption;
                md.AudioEmbed = post.AudioEmbed;
                md.AudioPlayer = post.AudioPlayer;
                md.AudioPlays = post.AudioPlays;

                md.Id3Album = post.Id3Album;
                md.Id3Artist = post.Id3Artist;
                md.Id3Title = post.Id3Title;
                md.Id3Track = post.Id3Track;
                md.Id3Year = post.Id3Year;

                md.DownloadedMediaFiles = post.DownloadedFilenames;
            }
            else if (post.Type == "photo")
            {
                md.PhotoCaption = post.PhotoCaption;
                md.PhotoLinkUrl = post.PhotoLinkUrl;
                md.PhotoUrl100 = post.PhotoUrl100;
                md.PhotoUrl1280 = post.PhotoUrl1280;
                md.PhotoUrl250 = post.PhotoUrl250;
                md.PhotoUrl400 = post.PhotoUrl400;
                md.PhotoUrl500 = post.PhotoUrl500;
                md.PhotoUrl75 = post.PhotoUrl75;
                md.Photos = post.Photos;

                md.DownloadedMediaFiles = post.DownloadedFilenames;
            }
            else if (post.Type == "video")
            {
                md.VideoCaption = post.VideoCaption;
                md.VideoPlayer = post.VideoPlayer;
                md.VideoPlayer250 = post.VideoPlayer250;
                md.VideoPlayer500 = post.VideoPlayer500;
                md.VideoSource = post.VideoSource;

                md.DownloadedMediaFiles = post.DownloadedFilenames;
            }
            else if (post.Type == "answer")
            {
                md.Answer = post.Answer;
                md.Question = post.Question;
            }
            else if (post.Type == "quote")
            {
                md.QuoteText = post.QuoteText;
                md.QuoteSource = post.QuoteSource;
            }
            else if (post.Type == "conversation")
            {
                md.Conversation = post.Conversation;
                md.ConversationText = post.ConversationText;
                md.ConversationTitle = post.ConversationTitle;
            }
            else if (post.Type == "link")
            {
                md.LinkDescription = post.LinkDescription;
                md.LinkText = post.LinkText;
                md.LinkUrl = post.LinkUrl;
            }

            return md;
        }
    }
}
