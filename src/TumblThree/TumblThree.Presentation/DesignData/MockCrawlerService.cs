﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Foundation;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.DesignData
{
    public class MockCrawlerService : Model, ICrawlerService
    {
        private readonly ObservableCollection<QueueListItem> activeItems;
        private readonly ReadOnlyObservableList<QueueListItem> readonlyActiveItems;

        public MockCrawlerService()
        {
            activeItems = new ObservableCollection<QueueListItem>();
            readonlyActiveItems = new ReadOnlyObservableList<QueueListItem>(activeItems);
        }

        public event EventHandler ActiveCollectionIdChanged;

        public ICommand AddBlogToQueueCommand { get; set; }

        public IReadOnlyObservableList<QueueListItem> ActiveItems => readonlyActiveItems;

        public void AddActiveItems(QueueListItem itemToAdd)
        {
        }

        public void RemoveActiveItem(QueueListItem itemToRemove)
        {
        }

        public ICollectionView Collections { get; }

        public void UpdateCollectionsList(bool isInit)
        {
        }

        public ICommand RemoveBlogCommand { get; set; }

        public ICommand AddBlogCommand { get; set; }

        public ICommand ImportBlogsCommand { get; set; }

        public ICommand RemoveBlogFromQueueCommand { get; set; }

        public ICommand ShowFilesCommand { get; set; }

        public ICommand EnqueueSelectedCommand { get; set; }

        public ICommand LoadLibraryCommand { get; set; }

        public ICommand LoadAllDatabasesCommand { get; set; }

        public ICommand LoadArchiveCommand { get; set; }

        public ICommand CheckIfDatabasesCompleteCommand { get; set; }

        public ICommand ListenClipboardCommand { get; set; }

        public ICommand CrawlCommand { get; set; }

        public ICommand PauseCommand { get; set; }

        public ICommand ResumeCommand { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand AutoDownloadCommand { get; set; }

        public bool IsCrawl { get; set; }

        public bool IsPaused { get; set; }

        public bool IsTimerSet { get; set; }

        public string IsTextVis { get; set; }

        public bool IsToolTipActive { get; set; }

        public string NewBlogUrl { get; set; }

        public int ActiveCollectionId { get; set; }

        public Guava.RateLimiter.RateLimiter TimeconstraintApi { get; set; }

        public Guava.RateLimiter.RateLimiter TimeconstraintSearchApi { get; set; }

        public Guava.RateLimiter.RateLimiter TimeconstraintSvc { get; set; }

        public Guava.RateLimiter.RateLimiter TimeconstraintTwitterApi { get; set; }

        public Timer Timer { get; set; }

        public TaskCompletionSource<bool> LibraryLoaded { get; set; }

        public TaskCompletionSource<bool> DatabasesLoaded { get; set; }

        public TaskCompletionSource<bool> ArchiveLoaded { get; set; }

        public void SetActiveBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            activeItems.Clear();
            blogFilesToAdd.ToList().ForEach(x => activeItems.Add(new QueueListItem(x)));
        }
    }
}
