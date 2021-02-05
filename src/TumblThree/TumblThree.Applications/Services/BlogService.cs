using System.IO;
using System.Linq;
using System.Threading;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    public class BlogService : IBlogService
    {
        private readonly IBlog _blog;
        private readonly IFiles _files;
        private readonly object _lockObjectProgress = new object();
        private readonly object _lockObjectPostCount = new object();
        //private readonly object _lockObjectDb = new object();
        private readonly object _lockObjectDirectory = new object();

        public BlogService(IBlog blog, IFiles files)
        {
            _blog = blog;
            _files = files;
        }

        public void UpdateBlogProgress()
        {
            lock (_lockObjectProgress)
            {
                _blog.DownloadedImages++;
                _blog.Progress = (int)(_blog.DownloadedImages / (double)_blog.TotalCount * 100);
            }
        }

        public void UpdateBlogPostCount(string propertyName)
        {
            lock (_lockObjectPostCount)
            {
                var property = typeof(IBlog).GetProperty(propertyName);
                var postCounter = (int)property.GetValue(_blog);
                postCounter++;
                property.SetValue(_blog, postCounter, null);
            }
        }

        //public void UpdateBlogDB(string fileName)
        //{
        //    lock (_lockObjectDb)
        //    {
        //        _files.Links.Add(fileName);
        //    }
        //}

        public bool CreateDataFolder()
        {
            if (string.IsNullOrEmpty(_blog.Name))
            {
                return false;
            }

            var blogPath = _blog.DownloadLocation();

            if (!Directory.Exists(blogPath))
            {
                Directory.CreateDirectory(blogPath);
            }

            return true;
        }

        //public bool CheckIfFileExistsInDB(string url)
        //{
        //    var fileName = url.Split('/').Last();
        //    Monitor.Enter(_lockObjectDb);
        //    try
        //    {
        //        return _files.Links.Contains(fileName);
        //    }
        //    finally
        //    {
        //        Monitor.Exit(_lockObjectDb);
        //    }
        //}

        public bool CheckIfBlogShouldCheckDirectory(string url)
        {
            return _blog.CheckDirectoryForFiles && CheckIfFileExistsInDirectory(url);
        }

        public bool CheckIfFileExistsInDirectory(string url)
        {
            var fileName = url.Split('/').Last();
            Monitor.Enter(_lockObjectDirectory);
            try
            {
                var blogPath = _blog.DownloadLocation();
                return File.Exists(Path.Combine(blogPath, fileName));
            }
            finally
            {
                Monitor.Exit(_lockObjectDirectory);
            }
        }

        public void SaveFiles()
        {
            _files.Save();
        }
    }
}
