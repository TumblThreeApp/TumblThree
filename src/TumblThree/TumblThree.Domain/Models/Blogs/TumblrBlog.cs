using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate, bool isCustomDomain = false)
        {
            url = isCustomDomain ? url : ExtractUrl(ConvertNewFormatUrl(url));
            var name = isCustomDomain ? ExtractCustomName(url) : ExtractName(url);
            var blog = new TumblrBlog()
            {
                Url = url,
                Name = name,
                BlogType = BlogTypes.tumblr,
                OriginalBlogType = BlogTypes.tumblr,
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
                IFiles files = new TumblrBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        private static string ConvertNewFormatUrl(string url)
        {
            if (UrlValidator.IsValidTumblrUrlInNewFormat(url))
            {
                var name = UrlValidator.GetTumblrNewUrlFormatBlogname(url);
                return $"https://{name}.tumblr.com";
            }
            else
            {
                return Blog.ExtractUrl(url);
            }
        }

        private static string ExtractCustomName(string url)
        {
            url = url.ToLower(CultureInfo.InvariantCulture).Replace("https://", string.Empty).Replace("http://", string.Empty).TrimEnd('/');
            var parts = url.Split('.');
            return parts[parts.Length - 2];
        }
    }
}
