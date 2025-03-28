using System;
using System.Linq;
using System.Threading;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Downloader
{
    public class BlueskyDownloader : AbstractDownloader
    {
        public BlueskyDownloader(IShellService shellService, IManagerService managerService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress,
            IPostQueue<AbstractPost> postQueue, FileDownloader fileDownloader, ICrawlerService crawlerService = null, IBlog blog = null, IFiles files = null)
            : base(shellService, managerService, ct, pt, progress, postQueue, fileDownloader, crawlerService, blog, files)
        {
        }
         
        protected override string FileName(TumblrPost downloadItem)
        {
            return (string.IsNullOrEmpty(downloadItem.PostedUrl) ? downloadItem.Url : downloadItem.PostedUrl).Split('/').Last();
        }

        protected override string FileNameUrl(TumblrPost downloadItem)
        {
            return CorrectUrlFileExtension(string.IsNullOrEmpty(downloadItem.PostedUrl) ? downloadItem.Url : downloadItem.PostedUrl).Split('/').Last();
        }

        private static string CorrectUrlFileExtension(string url)
        {
            return url.EndsWith("@jpeg", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".jpg" :
                url.EndsWith("@png", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".png" :
                url.EndsWith("@webp", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".webp" :
                url.EndsWith("@heic", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".heic" :
                url.EndsWith("@heif", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".heif" :
                url.EndsWith("@mp4", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".mp4" :
                url.EndsWith("@mpeg", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".mpeg" :
                url.EndsWith("@webm", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".webm" :
                url.EndsWith("@mov", StringComparison.InvariantCultureIgnoreCase) ? url.Substring(0, url.Length - 5) + ".mov" : url;
        }
    }
}
