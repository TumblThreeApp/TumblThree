﻿using System;
using System.IO;
using System.Runtime.Serialization;

using TumblThree.Domain.Models.Files;

namespace TumblThree.Domain.Models.Blogs
{
    [DataContract]
    public class TumblrHiddenBlog : Blog
    {
        public static Blog Create(string url, string location, string filenameTemplate)
        {
            var blog = new TumblrHiddenBlog()
            {
                Url = ExtractUrl(url),
                Name = ExtractName(url),
                BlogType = Models.BlogTypes.tmblrpriv,
                OriginalBlogType = Models.BlogTypes.tmblrpriv,
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
                IFiles files = new TumblrHiddenBlogFiles(blog.Name, blog.Location);
                files.Save();
            }

            return blog;
        }

        protected static new string ExtractName(string url) => url.Split('/')[5];

        protected static new string ExtractUrl(string url) => "https://" + ExtractName(url) + ".tumblr.com/";
    }
}
