using System;
using System.Threading;
using System.Threading.Tasks;

namespace TumblThree.Applications.Downloader
{
    public interface IDownloader : IDisposable
    {
        string AppendTemplate { get; set; }

        Task<bool> DownloadBlogAsync();

        void UpdateProgressQueueInformation(string format, params object[] args);

        Task<string> DownloadPageAsync(string url);

        bool CheckIfFileExistsInDB(string filenameUrl);

        bool CheckIfPostedUrlIsDownloaded(string url);

        void ChangeCancellationToken(CancellationToken ct);
    }
}
