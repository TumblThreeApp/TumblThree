using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrSearchBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new TumblrSearchBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.tumblrsearch,
                OriginalBlogType = BlogTypes.tumblrsearch,
                Location = location,
                Online = true,
                Version = "4",
                DateAdded = DateTime.Now,
                PageSize = 20,
                FilenameTemplate = filenameTemplate
            };

            Directory.CreateDirectory(location);

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrSearchBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        protected static new string ExtractName(string url) => url.Split('/')[4].Replace("-", "+");

        protected static new string ExtractUrl(string url)
        {
            if (url.StartsWith("http://"))
            {
                url = url.Insert(4, "s");
            }

            // don't remove "/recent", so that posts can be filtered by date
            var parts = url.Split('/');
            int blogNameLength = parts[4].Length;
            var urlLength = (parts.Length == 6 && string.Compare(parts[5], "recent", StringComparison.InvariantCulture) == 0) ? 37 : 30;
            return url.Substring(0, blogNameLength + urlLength);
        }
    }
}
