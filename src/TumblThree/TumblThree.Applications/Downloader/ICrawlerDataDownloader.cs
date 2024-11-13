using System;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Applications.DataModels;

namespace TumblThree.Applications.Downloader
{
    public interface ICrawlerDataDownloader
    {
        Task DownloadCrawlerDataAsync();

        Task GetAlreadyExistingCrawlerDataFilesAsync(IProgress<DownloadProgress> progress);

        bool ExistingCrawlerDataContainsOrAdd(string filename);

        void ChangeCancellationToken(CancellationToken ct);
    }
}
