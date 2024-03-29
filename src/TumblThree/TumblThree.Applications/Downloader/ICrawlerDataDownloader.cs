﻿using System.Threading;
using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public interface ICrawlerDataDownloader
    {
        Task DownloadCrawlerDataAsync();

        void ChangeCancellationToken(CancellationToken ct);
    }
}
