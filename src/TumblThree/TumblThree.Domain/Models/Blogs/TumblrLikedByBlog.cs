using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrLikedByBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new TumblrLikedByBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tlb,
                OriginalBlogType = BlogTypes.tlb,
                Location = location,
                Online = true,
                Version = "4",
                DateAdded = DateTime.Now,
                FilenameTemplate = filenameTemplate
            };

            Directory.CreateDirectory(location);

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrLikedByBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        protected static new string ExtractName(string url) => IsLikesUrl(url) ? "Likes" : url.Split('/')[5];

        protected static new string ExtractUrl(string url)
        {
            if (url.StartsWith("http://"))
            {
                url = url.Insert(4, "s");
            }

            if (IsLikesUrl(url)) return url;

            int blogNameLength = url.Split('/')[5].Length;
            var urlLength = 32;
            return url.Substring(0, blogNameLength + urlLength);
        }

        public static bool IsLikesUrl(string url) => url.Contains("www.tumblr.com/likes");
    }
}
