using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrCommunityBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new TumblrCommunityBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = Models.BlogTypes.tumblrcommunity,
                OriginalBlogType = Models.BlogTypes.tumblrcommunity,
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
                IFiles files = new TumblrCommunityBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        public static new string ExtractName(string url) => url.Split('/')[4];

        protected static new string ExtractUrl(string url) => "https://www.tumblr.com/communities/" + ExtractName(url);
    }
}
