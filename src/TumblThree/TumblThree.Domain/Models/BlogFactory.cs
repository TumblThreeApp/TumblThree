using System;
using System.ComponentModel.Composition;

using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Models
{
    [Export(typeof(IBlogFactory))]
    public class BlogFactory : IBlogFactory
    {
        private readonly IUrlValidator _urlValidator;

        [ImportingConstructor]
        internal BlogFactory(IUrlValidator urlValidator)
        {
            _urlValidator = urlValidator;
        }

        public bool IsValidTumblrBlogUrl(string blogUrl)
        {
            blogUrl = _urlValidator.AddHttpsProtocol(blogUrl);
            return _urlValidator.IsValidTumblrUrl(blogUrl)
                   || _urlValidator.IsValidTumblrHiddenUrl(blogUrl)
                   || _urlValidator.IsValidTumblrLikedByUrl(blogUrl)
                   || _urlValidator.IsValidTumblrSearchUrl(blogUrl)
                   || _urlValidator.IsValidTumblrTagSearchUrl(blogUrl);
        }

        public IBlog GetBlog(string blogUrl, string path)
        {
            blogUrl = _urlValidator.AddHttpsProtocol(blogUrl);
            if (_urlValidator.IsValidTumblrUrl(blogUrl))
            {
                return TumblrBlog.Create(blogUrl, path);
            }

            if (_urlValidator.IsValidTumblrHiddenUrl(blogUrl))
            {
                return TumblrHiddenBlog.Create(blogUrl, path);
            }

            if (_urlValidator.IsValidTumblrLikedByUrl(blogUrl))
            {
                return TumblrLikedByBlog.Create(blogUrl, path);
            }

            if (_urlValidator.IsValidTumblrSearchUrl(blogUrl))
            {
                return TumblrSearchBlog.Create(blogUrl, path);
            }

            if (_urlValidator.IsValidTumblrTagSearchUrl(blogUrl))
            {
                return TumblrTagSearchBlog.Create(blogUrl, path);
            }

            throw new ArgumentException("Website is not supported!", nameof(blogUrl));
        }
    }
}
