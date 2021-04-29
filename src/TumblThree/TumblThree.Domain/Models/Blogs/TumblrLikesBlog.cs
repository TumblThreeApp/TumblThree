using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrLikesBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new TumblrLikesBlog()
            {
                Url = ExtractUrl(url),
                Name = "Likes",
                BlogType = BlogTypes.tl,
                OriginalBlogType = BlogTypes.tl,
                Location = location,
                Online = true,
                Version = "4",
                DateAdded = DateTime.Now,
                FilenameTemplate = filenameTemplate
            };

            Directory.CreateDirectory(location);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(location).FullName, blog.Name));

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new TumblrLikesBlogFiles(blog.Name, blog.Location);
                files.Save();
                files = null;
            }

            return blog;
        }


        protected static new string ExtractUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            if (url.StartsWith("http://", true, null))
            {
                url = url.Insert(4, "s");
            }

            return url;
        }
    }
}
