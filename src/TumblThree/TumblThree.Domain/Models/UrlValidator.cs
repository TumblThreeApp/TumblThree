using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IUrlValidator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
    public class UrlValidator : IUrlValidator
    {
        private static readonly Regex tumbexRegex = new Regex("(http[A-Za-z0-9_/:.]*www.tumbex.com[A-Za-z0-9_/:.-]*tumblr/)");
        private static readonly Regex urlRegex = new Regex(@"^(?:http(s)?:\/\/){1}?[\w.-]+(?:\.[\w\.-]+)+[/]??$");
        private static readonly Regex twitterRegex = new Regex("(^https?://x.com/[A-Za-z0-9_]+$)");
        private static readonly Regex newTumblRegex = new Regex(@"(^(?:https?://)?(?:(?!.+like$)(?:[\w-]+\.{1})|.{0})newtumbl.com[/]??(?:like|.{0})$)");
        private static readonly Regex tumblrUrl = new Regex(@"^(?:http(?:s)?:\/\/)(?!www)[\w-]+.tumblr.com[/]??");
        private static readonly Regex tumblrUrlNew = new Regex(@"^(?:http(?:s)?:\/\/)www.tumblr.com\/((?!dashboard|like(s|d)|search|tagged)[^/]+)");
        private static readonly Regex blueskyRegex = new Regex(@"^(?:http(?:s)?:\/\/)bsky.app/profile/[A-Za-z0-9_.-]+$");

        private static bool CheckNullLengthProtocolAndWhiteSpace(string url, int minLength)
        {
            return url != null && (minLength <= 0 || url.Length > minLength) && !url.Any(char.IsWhiteSpace) &&
                (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public static bool IsValidTumblrUrlInNewFormat(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 0) && tumblrUrlNew.IsMatch(url);
        }

        public static string GetTumblrNewUrlFormatBlogname(string url)
        {
            var match = tumblrUrlNew.Match(url);
            return match.Success ? match.Groups[1].Value : "";
        }

        public static string CorrectTwitterkUrl(string url)
        {
            return ReplaceCI(url, "twitter.com", "x.com");
        }

        public bool IsValidTumblrUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 18) && !url.Contains(".media.tumblr.com") &&
                (tumblrUrl.IsMatch(url) || tumblrUrlNew.IsMatch(url));
        }

        public bool IsTumbexUrl(string url)
        {
            return tumbexRegex.IsMatch(url);
        }

        public bool IsValidTumblrHiddenUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 38) && url.Contains("www.tumblr.com/dashboard/blog/");
        }

        public bool IsValidTumblrLikesUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 0) && url.Contains("www.tumblr.com/likes");
        }

        public bool IsValidTumblrLikedByUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 31) && url.Contains("www.tumblr.com/liked/by/");
        }

        public bool IsValidTumblrSearchUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 29) && url.Contains("www.tumblr.com/search/");
        }

        public bool IsValidTumblrTagSearchUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 29) && url.Contains("www.tumblr.com/tagged/");
        }

        public bool IsValidUrl(string url)
        {
            return CheckNullLengthProtocolAndWhiteSpace(url, 0) && urlRegex.IsMatch(url);
        }

        public bool IsValidTwitterUrl(string url)
        {
            return url != null && twitterRegex.IsMatch(url) && !url.EndsWith("/home");
        }

        public bool IsValidNewTumblUrl(string url)
        {
            return url != null && newTumblRegex.IsMatch(url);
        }

        public bool IsValidBlueskyUrl(string url)
        {
            return url != null && blueskyRegex.IsMatch(url);
        }

        public string AddHttpsProtocol(string url)
        {
            if (url == null)
            {
                return string.Empty;
            }

            if (!url.StartsWith("http"))
            {
                return "https://" + url;
            }

            return url;
        }

        private static string ReplaceCI(string input, string search, string replacement)
        {
            string result = Regex.Replace(
                input,
                Regex.Escape(search),
                replacement.Replace("$", "$$"),
                RegexOptions.IgnoreCase
            );
            return result;
        }
    }
}
