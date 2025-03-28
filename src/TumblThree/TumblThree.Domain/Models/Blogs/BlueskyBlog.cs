using System;
using System.IO;
using System.Runtime.Serialization;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class BlueskyBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new BlueskyBlog()
            {
                Url = url,
                Name = ExtractName(url),
                BlogType = BlogTypes.bluesky,
                OriginalBlogType = BlogTypes.bluesky,
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
                IFiles files = new BlueskyBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        protected static new string ExtractName(string url) => url.Split('/')[4].Replace(".bsky.social", "");

        protected static new string ExtractUrl(string url) => "https://bsky.app/profile/" + ExtractName(url);
    }
}
