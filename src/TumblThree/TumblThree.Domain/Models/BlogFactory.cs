using System;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IBlogFactory))]
    public class BlogFactory : IBlogFactory
    {
        private readonly IUrlValidator _urlValidator;
        private readonly Regex tumbexRegex = new Regex("(http[A-Za-z0-9_/:.]*www.tumbex.com/([A-Za-z0-9_/:.-]*)\\.tumblr/)");

        [ImportingConstructor]
        internal BlogFactory(IUrlValidator urlValidator)
        {
            _urlValidator = urlValidator;
        }

        public bool IsValidBlogUrl(string blogUrl)
        {
            blogUrl = _urlValidator.AddHttpsProtocol(blogUrl);
            blogUrl = UrlValidator.CorrectTwitterkUrl(blogUrl);
            return _urlValidator.IsValidTumblrUrl(blogUrl)
                   || _urlValidator.IsValidTumblrHiddenUrl(blogUrl)
                   || _urlValidator.IsValidTumblrLikedByUrl(blogUrl)
                   || _urlValidator.IsValidTumblrLikesUrl(blogUrl)
                   || _urlValidator.IsValidTumblrSearchUrl(blogUrl)
                   || _urlValidator.IsValidTumblrTagSearchUrl(blogUrl)
                   || _urlValidator.IsTumbexUrl(blogUrl)
                   || _urlValidator.IsValidTwitterUrl(blogUrl)
                   || _urlValidator.IsValidNewTumblUrl(blogUrl)
                   || _urlValidator.IsValidBlueskyUrl(blogUrl);
        }

        public bool IsValidUrl(string url)
        {
            url = _urlValidator.AddHttpsProtocol(url);
            return _urlValidator.IsValidUrl(url);
        }

        public IBlog GetBlog(string blogUrl, string path, string filenameTemplate)
        {
            blogUrl = _urlValidator.AddHttpsProtocol(blogUrl);
            blogUrl = UrlValidator.CorrectTwitterkUrl(blogUrl);

            if (_urlValidator.IsValidTumblrLikesUrl(blogUrl))
            {
                return TumblrLikedByBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidTumblrUrl(blogUrl))
            {
                return TumblrBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsTumbexUrl(blogUrl))
            {
                return TumblrBlog.Create(CreateTumblrUrlFromTumbex(blogUrl), path, filenameTemplate);
            }

            if (_urlValidator.IsValidTumblrHiddenUrl(blogUrl))
            {
                return TumblrHiddenBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidTumblrLikedByUrl(blogUrl))
            {
                return TumblrLikedByBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidTumblrSearchUrl(blogUrl))
            {
                return TumblrSearchBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidTumblrTagSearchUrl(blogUrl))
            {
                return TumblrTagSearchBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidTwitterUrl(blogUrl))
            {
                return TwitterBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidNewTumblUrl(blogUrl))
            {
                return NewTumblBlog.Create(blogUrl, path, filenameTemplate);
            }

            if (_urlValidator.IsValidBlueskyUrl(blogUrl))
            {
                return BlueskyBlog.Create(blogUrl, path, filenameTemplate);
            }

            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }

        //TODO: Refactor out.
        private string CreateTumblrUrlFromTumbex(string blogUrl)
        {
            Match match = tumbexRegex.Match(blogUrl);
            String tumblrBlogName = match.Groups[2].Value;

            return $"https://{tumblrBlogName}.tumblr.com/";
        }
    }
}
