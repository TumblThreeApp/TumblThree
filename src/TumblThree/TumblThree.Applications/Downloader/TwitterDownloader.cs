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
    public class TwitterDownloader : AbstractDownloader
    {
        public TwitterDownloader(IShellService shellService, IManagerService managerService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress,
            IPostQueue<AbstractPost> postQueue, FileDownloader fileDownloader, ICrawlerService crawlerService = null, IBlog blog = null, IFiles files = null)
            : base(shellService, managerService, ct, pt, progress, postQueue, fileDownloader, crawlerService, blog, files)
        {
        }

        protected override string FileName(TumblrPost downloadItem)
        {
            var url = downloadItem.Url.Split('/').Last();
            if (url.Contains("?format=") && url.Contains("&name="))
            {
                var ext = url.Substring(url.IndexOf('?') + 1).Replace("format=", "");
                ext = ext.Substring(0, ext.IndexOf('&'));
                url = url.Substring(0, url.IndexOf('?')) + "." + ext;
            }
            return url;
        }

        protected override string FileNameUrl(TumblrPost downloadItem)
        {
            return FileName(downloadItem);
        }
    }
}
