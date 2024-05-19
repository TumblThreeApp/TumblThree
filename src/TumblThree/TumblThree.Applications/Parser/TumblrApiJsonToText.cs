using System;
using System.Globalization;
using System.Linq;

using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Parser
{
    public class TumblrApiJsonToTextParser<T> : ITumblrToTextParser<T> where T : Post
    {
        public string ParseText(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.RegularTitle) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Body, post.RegularBody) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine +
                   ((post.DownloadedFilenames?.Count > 0) ? string.Format(CultureInfo.CurrentCulture, Resources.DownloadedFiles,
                       "\"" + string.Join("\",\n \"", post.DownloadedFilenames.ToArray()) + "\"") + Environment.NewLine : "");
        }

        public string ParseQuote(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Quote, post.QuoteText) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.QuoteSource, post.QuoteSource) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Body, post.RegularBody) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseLink(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Link, post.LinkDescription) +
                   (string.IsNullOrEmpty(post.LinkText) ? "" : Environment.NewLine + post.LinkText) +
                   (string.IsNullOrEmpty(post.LinkUrl) ? "" : Environment.NewLine + post.LinkUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Body, post.RegularBody) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseConversation(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Title, post.ConversationTitle) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Body, post.ConversationText.Replace("\n", Environment.NewLine + new string(' ', Resources.Body.Replace("{0}", "").Length))) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParseAnswer(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Question, post.Question) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Answer, post.Answer) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine;
        }

        public string ParsePhotoMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoUrl, post.PhotoUrl1280) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoSetUrl,
                       string.Join(" ", post.Photos.Select(photo => photo.PhotoUrl1280))) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PhotoCaption, post.PhotoCaption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.DownloadedFiles,
                       (post.DownloadedFilenames?.Count > 0 ? "\"" + string.Join("\",\n \"", post.DownloadedFilenames.ToArray()) + "\"" : "")) +
                   Environment.NewLine;
        }

        public string ParseVideoMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoCaption, post.VideoCaption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoSource, post.VideoSource) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.VideoPlayer, post.VideoPlayer) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.DownloadedFiles,
                       (post.DownloadedFilenames?.Count > 0 ? "\"" + string.Join("\",\n \"", post.DownloadedFilenames.ToArray()) + "\"" : "")) +
                   Environment.NewLine;
        }

        public string ParseAudioMeta(T post)
        {
            return string.Format(CultureInfo.CurrentCulture, Resources.PostId, post.Id) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Date, post.DateGmt) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.PostUrl, post.UrlWithSlug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Slug, post.Slug) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogKey, post.ReblogKey) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogUrl, post.RebloggedFromUrl) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.ReblogName, post.RebloggedFromName) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.AudioCaption, post.AudioCaption) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Artist, post.Id3Artist) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Title, post.Id3Title) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Track, post.Id3Track) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Album, post.Id3Album) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Id3Year, post.Id3Year) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.Tags,
                       string.Join(", ", post.Tags.ToArray())) +
                   Environment.NewLine +
                   string.Format(CultureInfo.CurrentCulture, Resources.DownloadedFiles,
                       (post.DownloadedFilenames?.Count > 0 ? "\"" + string.Join("\",\n \"", post.DownloadedFilenames.ToArray()) + "\"" : "")) +
                   Environment.NewLine;
        }
    }
}
