using System;
using System.IO;
using System.Runtime.Serialization;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TwitterBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new TwitterBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.twitter,
                OriginalBlogType = BlogTypes.twitter,
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
                IFiles files = new TwitterBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        protected static new string ExtractName(string url) => url.Split('/')[3];

        protected static new string ExtractUrl(string url) => "https://twitter.com/" + ExtractName(url);
    }
}
