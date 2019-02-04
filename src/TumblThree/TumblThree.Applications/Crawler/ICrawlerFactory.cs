using System;
using System.Threading;

using TumblThree.Applications.DataModels;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Crawler
{
    public interface ICrawlerFactory
    {
        ICrawler GetCrawler(IBlog blog);

        ICrawler GetCrawler(IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct);
    }
}
