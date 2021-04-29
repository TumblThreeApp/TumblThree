﻿using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IUrlValidator))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class UrlValidator : IUrlValidator
    {
        private readonly Regex tumbexRegex = new Regex("(http[A-Za-z0-9_/:.]*www.tumbex.com[A-Za-z0-9_/:.-]*tumblr/)");
        private readonly Regex urlRegex = new Regex("(^https?://[A-Za-z0-9_.]*[/]?$)");

        public bool IsValidTumblrUrl(string url)
        {
            return url != null &&
                url.Length > 18 &&
                url.Contains(".tumblr.com") &&
                (!url.Contains("//www.tumblr.com") || url.EndsWith("www.tumblr.com/likes", true, null)) &&
                !url.Any(char.IsWhiteSpace) &&
                !url.Contains(".media.tumblr.com") &&
                (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsTumbexUrl(string url)
        {
            return tumbexRegex.IsMatch(url);
        }

        public bool IsValidTumblrHiddenUrl(string url)
        {
            return url != null && url.Length > 38 && url.Contains("www.tumblr.com/dashboard/blog/") &&
                   !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrLikedByUrl(string url)
        {
            return url != null && url.Length > 31 && url.Contains("www.tumblr.com/liked/by/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrSearchUrl(string url)
        {
            return url != null && url.Length > 29 && url.Contains("www.tumblr.com/search/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidTumblrTagSearchUrl(string url)
        {
            return url != null && url.Length > 29 && url.Contains("www.tumblr.com/tagged/") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }

        public bool IsValidUrl(string url)
        {
            return url != null && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null)) &&
                   urlRegex.IsMatch(url);
        }

        public string AddHttpsProtocol(string url)
        {
            if (url == null)
            {
                return string.Empty;
            }

            if (!url.Contains("http"))
            {
                return "https://" + url;
            }

            return url;
        }

        public bool IsValidTumblrLikesUrl(string url)
        {
            return url != null && url.Contains("www.tumblr.com/likes") && !url.Any(char.IsWhiteSpace) &&
                   (url.StartsWith("http://", true, null) || url.StartsWith("https://", true, null));
        }
    }
}
