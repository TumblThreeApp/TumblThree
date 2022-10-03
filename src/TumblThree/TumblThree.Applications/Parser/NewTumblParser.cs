using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TumblThree.Applications.Parser
{
    public class NewTumblParser : INewTumblParser
    {
        public Regex GetPhotoUrlRegex() => new Regex("\"(http[A-Za-z0-9_/:.]*newtumbl\\.com[A-Za-z0-9_/:.-]*(?<!_150)\\.(jpg|jpeg|png|gif))\"");

        public Regex GetGenericPhotoUrlRegex() => new Regex("\"(https?://(?:[a-z0-9\\-]+\\.)+[a-z]{2,6}(?:/[^/#?]+)+\\.(?:jpg|jpeg|tiff|tif|heif|heic|png|gif|webp))\"");

        public IEnumerable<string> SearchForPhotoUrl(string searchableText)
        {
            Regex regex = GetPhotoUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string imageUrl = match.Groups[1].Value;
                yield return imageUrl;
            }
        }

        public IEnumerable<string> SearchForGenericPhotoUrl(string searchableText)
        {
            Regex regex = GetGenericPhotoUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                string imageUrl = match.Groups[1].Value;
                yield return imageUrl;
            }
        }

        public bool IsNewTumblUrl(string url)
        {
            var regex = new Regex("/nT_[\\w]*");
            return regex.IsMatch(url);
        }
    }
}
