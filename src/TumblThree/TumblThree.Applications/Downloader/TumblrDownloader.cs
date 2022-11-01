using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Downloader
{
    public class TumblrDownloader : AbstractDownloader
    {
        private int numberOfPagesCrawled = 0;

        public TumblrDownloader(IShellService shellService, IManagerService managerService, PauseToken pt,
            IProgress<DownloadProgress> progress, IPostQueue<AbstractPost> postQueue, FileDownloader fileDownloader,
            ICrawlerService crawlerService, IBlog blog, IFiles files, CancellationToken ct)
            : base(shellService, managerService, ct, pt, progress, postQueue, fileDownloader, crawlerService, blog, files)
        {
        }

        protected string ImageSize()
        {
            return (shellService.Settings.ImageSize == "raw" || shellService.Settings.ImageSize == "best") ? "1280" : shellService.Settings.ImageSize;
        }

        protected string ResizeTumblrImageUrl(string imageUrl)
        {
            var sb = new StringBuilder(imageUrl);
            return sb
                   .Replace("_raw", "_" + ImageSize())
                   .Replace("_1280", "_" + ImageSize())
                   .Replace("_540", "_" + ImageSize())
                   .Replace("_500", "_" + ImageSize())
                   .Replace("_400", "_" + ImageSize())
                   .Replace("_250", "_" + ImageSize())
                   .Replace("_100", "_" + ImageSize())
                   .Replace("_75sq", "_" + ImageSize())
                   .ToString();
        }

        protected override async Task<bool> DownloadBinaryPostAsync(TumblrPost downloadItem)
        {
            if (!(downloadItem is PhotoPost && blog.ForceSize))
            {
                return await base.DownloadBinaryPostAsync(downloadItem);
            }

            string url = Url(downloadItem);
            url = ResizeTumblrImageUrl(url);

            return await base.DownloadBinaryPostAsync(new PhotoPost(url, downloadItem.PostedUrl, downloadItem.Id, downloadItem.Date, downloadItem.Filename));
        }
    }
}
