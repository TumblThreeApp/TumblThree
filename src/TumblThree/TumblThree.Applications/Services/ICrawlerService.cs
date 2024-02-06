using Guava.RateLimiter;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Foundation;
using System.Windows.Input;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    public interface ICrawlerService : INotifyPropertyChanged
    {
        ICommand ImportBlogsCommand { get; set; }

        ICommand AddBlogCommand { get; set; }

        ICommand RemoveBlogCommand { get; set; }

        ICommand ShowFilesCommand { get; set; }

        ICommand EnqueueSelectedCommand { get; set; }

        ICommand DequeueSelectedCommand { get; set; }

        ICommand LoadLibraryCommand { get; set; }

        ICommand LoadAllDatabasesCommand { get; set; }

        ICommand LoadArchiveCommand { get; set; }

        ICommand CheckIfDatabasesCompleteCommand { get; set; }

        ICommand RemoveBlogFromQueueCommand { get; set; }

        ICommand RemoveBlogSelectionFromQueueCommand { get; set; }

        ICommand ListenClipboardCommand { get; set; }

        ICommand CrawlCommand { get; set; }

        ICommand PauseCommand { get; set; }

        ICommand ResumeCommand { get; set; }

        ICommand StopCommand { get; set; }

        ICommand AutoDownloadCommand { get; set; }

        bool IsCrawl { get; set; }

        bool IsPaused { get; set; }

        bool IsTimerSet { get; set; }

        string IsTextVis { get; set; }
        bool IsToolTipActive { get; set; }

        string NewBlogUrl { get; set; }

        IBlog LastDeselectedPreview { get; set; }

        int ActiveCollectionId { get; set; }

        IReadOnlyObservableList<QueueListItem> ActiveItems { get; }

        RateLimiter TimeconstraintApi { get; set; }

        RateLimiter TimeconstraintSearchApi { get; set; }

        RateLimiter TimeconstraintSvc { get; set; }

        RateLimiter TimeconstraintTwitterApi { get; set; }

        Timer Timer { get; set; }

        TaskCompletionSource<bool> LibraryLoaded { get; set; }

        TaskCompletionSource<bool> DatabasesLoaded { get; set; }

        TaskCompletionSource<bool> ArchiveLoaded { get; set; }

        event EventHandler ActiveCollectionIdChanged;

        void AddActiveItems(QueueListItem itemToAdd);

        void RemoveActiveItem(QueueListItem itemToRemove);

        ICollectionView Collections { get; }

        void UpdateCollectionsList(bool isInit);
    }
}
