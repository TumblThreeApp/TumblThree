using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TumblThree.Applications.Parser
{
    public interface ITumblrParser
    {
        Regex GetTumblrPhotoUrlRegex();

        Regex GetGenericPhotoUrlRegex();

        Regex GetTumblrVVideoUrlRegex();

        Regex GetTumblrThumbnailUrlRegex();

        Regex GetTumblrInlineVideoUrlRegex();

        Regex GetGenericVideoUrlRegex();

        IEnumerable<string> SearchForTumblrPhotoUrl(string searchableText);

        IEnumerable<string> SearchForTumblrVideoUrl(string searchableText);

        IEnumerable<string> SearchForTumblrVideoThumbnailUrl(string searchableText);

        IEnumerable<string> SearchForTumblrInlineVideoUrl(string searchableText);

        IEnumerable<string> SearchForGenericPhotoUrl(string searchableText);

        IEnumerable<string> SearchForGenericVideoUrl(string searchableText);

        bool IsTumblrUrl(string url);
    }
}
