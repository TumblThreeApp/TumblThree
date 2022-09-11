using System;
using System.IO;
using System.Runtime.Serialization;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class NewTumblBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new NewTumblBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = BlogTypes.newtumbl,
                OriginalBlogType = BlogTypes.newtumbl,
                Location = location,
                Online = true,
                Version = "4",
                DateAdded = DateTime.Now,
                FilenameTemplate = filenameTemplate
            };

            _ = Directory.CreateDirectory(location);

            blog.ChildId = Path.Combine(location, blog.Name + "_files." + blog.BlogType);
            if (!File.Exists(blog.ChildId))
            {
                IFiles files = new NewTumblBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        protected static new string ExtractUrl(string url)
        {
            return "https://" + ExtractSubDomain(url) + ".newtumbl.com/";
        }
    }
}
