﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using TumblThree.Applications.Crawler;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class CrawlerController : IDisposable
    {
        private readonly ICrawlerFactory _crawlerFactory;
        private readonly ICrawlerService _crawlerService;
        private readonly Lazy<CrawlerViewModel> _crawlerViewModel;
        private readonly ITumblrBlogDetector _tumblrBlogDetector;
        private readonly IManagerService _managerService;
        private readonly IShellService _shellService;
        private readonly IMessageService _messageService;
        private readonly AsyncDelegateCommand _crawlCommand;
        private readonly DelegateCommand _pauseCommand;
        private readonly DelegateCommand _resumeCommand;
        private readonly DelegateCommand _stopCommand;

        private readonly object _lockObject;
        private readonly List<Task> _runningTasks;
        private CancellationTokenSource _crawlerCancellationTokenSource;
        private PauseTokenSource _crawlerPauseTokenSource;

        [ImportingConstructor]
        public CrawlerController(IShellService shellService, IManagerService managerService, IMessageService messageService, ICrawlerService crawlerService,
            ICrawlerFactory crawlerFactory, Lazy<CrawlerViewModel> crawlerViewModel, ITumblrBlogDetector tumblrBlogDetector)
        {
            _shellService = shellService;
            _managerService = managerService;
            _messageService = messageService;
            _crawlerService = crawlerService;
            _crawlerViewModel = crawlerViewModel;
            _tumblrBlogDetector = tumblrBlogDetector;
            _crawlerFactory = crawlerFactory;
            _crawlCommand = new AsyncDelegateCommand(SetupCrawlAsync, CanCrawl);
            _pauseCommand = new DelegateCommand(Pause, CanPause);
            _resumeCommand = new DelegateCommand(Resume, CanResume);
            _stopCommand = new DelegateCommand(Stop, CanStop);
            _runningTasks = new List<Task>();
            _lockObject = new object();
        }

        private CrawlerViewModel CrawlerViewModel => _crawlerViewModel.Value;

        public QueueManager QueueManager { get; set; }

        public void Initialize()
        {
            _crawlerService.CrawlCommand = _crawlCommand;
            _crawlerService.PauseCommand = _pauseCommand;
            _crawlerService.ResumeCommand = _resumeCommand;
            _crawlerService.StopCommand = _stopCommand;
            _shellService.CrawlerView = CrawlerViewModel.View;
            CheckTextVis();
        }

        /// <summary>
        /// Ask the controller if a shutdown can be executed.
        /// </summary>
        /// <returns>
        /// true  - can be executed,
        /// false - shall be postponed
        /// </returns>
        public bool QueryShutdown()
        {
            if (!_managerService.UpdateCollectionOnlineStatuses(true))
            {
                return false;
            }
            foreach (IBlog blog in _managerService.BlogFiles)
            {
                if (blog.Dirty)
                {
                    var collection = _shellService.Settings.GetCollection(blog.CollectionId);
                    if (!collection.IsOnline.Value)
                    {
                        if (_messageService.ShowYesNoQuestion(string.Format(Resources.AskModifiedBlogContinueShutdownAnyway, collection.Name))) { break; }
                        return false;
                    }
                }
            }
            return true;
        }

        public void Shutdown()
        {
            try
            {
                if (_stopCommand.CanExecute(null))
                {
                    _stopCommand.Execute(null);
                }

                Task.WaitAll(_runningTasks.ToArray());
            }
            catch (AggregateException)
            {
            }

            using (_shellService.SetApplicationBusy())
            {
                foreach (IBlog blog in _managerService.BlogFiles)
                {
                    if (blog.Dirty)
                    {
                        try
                        {
                            var collection = _shellService.Settings.GetCollection(blog.CollectionId);
                            if (collection.IsOnline.Value)
                            {
                                blog.Save();
                            }
                            else
                            {
                                Logger.Warning(Resources.CannotSaveChangedBlogFile, blog.Name, collection.DownloadLocation);
                                _shellService.ShowError(null, Resources.CannotSaveChangedBlogFile, blog.Name, collection.DownloadLocation);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("CrawlerController.Shutdown: {0}", ex);
                            _shellService.ShowError(ex, Resources.SavingBlogFileFailed, blog.Name, ex.Message);
                        }
                    }
                }

                _managerService.CacheLibraries();
            }
        }

        private bool CanStop() => _crawlerService.IsCrawl;

        private void Stop()
        {
            if (_resumeCommand.CanExecute(null))
            {
                _resumeCommand.Execute(null);
            }

            try
            {
                _crawlerCancellationTokenSource.Cancel();
            }
            catch
            {
                // sometimes it fails to cancel the crawlers, because they are already cancelled/disposed
            }
            _crawlerService.IsCrawl = false;
            _crawlCommand.RaiseCanExecuteChanged();
            _pauseCommand.RaiseCanExecuteChanged();
            _resumeCommand.RaiseCanExecuteChanged();
            _stopCommand.RaiseCanExecuteChanged();
            _crawlerService.StopFreeDiskSpaceMonitor();
        }

        private bool CanPause() => _crawlerService.IsCrawl && !_crawlerService.IsPaused;

        private void Pause()
        {
            _crawlerPauseTokenSource.PauseWithResponseAsync().Wait();
            _crawlerService.IsPaused = true;
            _pauseCommand.RaiseCanExecuteChanged();
            _resumeCommand.RaiseCanExecuteChanged();
            _crawlerService.StopFreeDiskSpaceMonitor();
        }

        private bool CanResume() => _crawlerService.IsCrawl && _crawlerService.IsPaused;

        private void Resume()
        {
            _crawlerPauseTokenSource.Resume();
            _crawlerService.IsPaused = false;
            _pauseCommand.RaiseCanExecuteChanged();
            _resumeCommand.RaiseCanExecuteChanged();
            _crawlerService.StartFreeDiskSpaceMonitor();
        }

        private bool CanCrawl() => !_crawlerService.IsCrawl;

        private async Task SetupCrawlAsync()
        {
            _crawlerCancellationTokenSource = new CancellationTokenSource();
            _crawlerPauseTokenSource = new PauseTokenSource();

            _crawlerService.IsCrawl = true;

            _crawlCommand.RaiseCanExecuteChanged();
            _pauseCommand.RaiseCanExecuteChanged();
            _stopCommand.RaiseCanExecuteChanged();

            await Task.WhenAll(_crawlerService.LibraryLoaded.Task, _crawlerService.DatabasesLoaded.Task, _crawlerService.ArchiveLoaded.Task);

            _crawlerService.StartFreeDiskSpaceMonitor();

            for (var i = 0; i < _shellService.Settings.ConcurrentBlogs; i++)
            {
                _runningTasks.Add(Task.Run(() =>
                    RunCrawlerTasksAsync(_crawlerPauseTokenSource.Token, _crawlerCancellationTokenSource.Token)));
            }

            await CrawlAsync();

            _crawlerService.StopFreeDiskSpaceMonitor();
        }

        private async Task CrawlAsync()
        {
            try
            {
                await Task.WhenAll(_runningTasks.ToArray());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            finally
            {
                _crawlerCancellationTokenSource.Dispose();
                _runningTasks.Clear();
            }
        }

        private async Task RunCrawlerTasksAsync(PauseToken pt, CancellationToken ct)
        {
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if (pt.IsPaused)
                {
                    pt.WaitWhilePausedWithResponseAsync().Wait();
                }

                bool lockTaken = false;
                Monitor.Enter(_lockObject, ref lockTaken);
                try
                {
                    if (_crawlerService.ActiveItems.Count < QueueManager.Items.Count)
                    {
                        QueueListItem nextQueueItem;
                        try
                        {
                            nextQueueItem = QueueManager.Items.Except(_crawlerService.ActiveItems).First();
                        }
                        catch (InvalidOperationException)
                        {
                            Monitor.Exit(_lockObject);
                            continue;
                        }
                        IBlog blog = nextQueueItem.Blog;
                        bool isHiddenTumblrBlog = false;
                        if (blog.BlogType == BlogTypes.tumblr)
                            isHiddenTumblrBlog = _tumblrBlogDetector.IsHiddenTumblrBlogAsync(blog.Url).GetAwaiter().GetResult();
                        if (isHiddenTumblrBlog)
                            blog.BlogType = BlogTypes.tmblrpriv;
                        var privacyConsentNeeded = false;
                        ICrawler crawler = _crawlerFactory.GetCrawler(blog, new Progress<DownloadProgress>(), pt, ct);
                        try
                        {
                            crawler.IsBlogOnlineAsync().Wait(4000, ct);
                        }
                        catch (AggregateException ex)
                        {
                            if (ex.InnerExceptions.Any(x => x.Message == "Acceptance of privacy consent needed!"))
                                privacyConsentNeeded = true;
                        }
                        finally
                        {
                            crawler.Dispose();
                        }

                        if (privacyConsentNeeded
                            || (_crawlerService.ActiveItems.Any(item =>
                                item.Blog.Name.Equals(nextQueueItem.Blog.Name) &&
                                item.Blog.BlogType.Equals(nextQueueItem.Blog.BlogType)))
                            || (!nextQueueItem.Blog.Online))
                        {
                            QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(nextQueueItem));
                            Monitor.Exit(_lockObject);
                            continue;
                        }

                        _crawlerService.AddActiveItems(nextQueueItem);
                        Monitor.Exit(_lockObject);
                        lockTaken = false;
                        await StartSiteSpecificDownloaderAsync(nextQueueItem, pt, ct);
                    }
                    else
                    {
                        Monitor.Exit(_lockObject);
                        lockTaken = false;
                        await Task.Delay(4000, ct);
                    }
                }
                catch (Exception e)
                {
                    if (!ct.IsCancellationRequested) Logger.Error("CrawlerController.RunCrawlerTasksAsync: {0}", e);
                    if (lockTaken) Monitor.Exit(_lockObject);
                }
            }
        }

        private async Task StartSiteSpecificDownloaderAsync(QueueListItem queueListItem, PauseToken pt, CancellationToken ct)
        {
            IBlog blog = queueListItem.Blog;
            blog.Dirty = true;
            ProgressThrottler<DownloadProgress> progress = SetupThrottledQueueListProgress(queueListItem);

            ICrawler crawler = null;
            try
            {
                crawler = _crawlerFactory.GetCrawler(blog, progress, pt, ct);
                queueListItem.InterruptionRequested += crawler.InterruptionRequestedEventHandler;
                await crawler.CrawlAsync();
                blog.UpdateProgress(false);
            }
            catch (Exception e)
            {
                if (!ct.IsCancellationRequested)
                {
                    Logger.Error("CrawlerController.StartSiteSpecificDownloaderAsync: {0}", e);
                }
            }
            finally
            {
                if (crawler != null)
                {
                    queueListItem.InterruptionRequested -= crawler.InterruptionRequestedEventHandler;
                }
                crawler?.Dispose();
            }

            Monitor.Enter(_lockObject);
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => _crawlerService.RemoveActiveItem(queueListItem));
            Monitor.Exit(_lockObject);

            if (!ct.IsCancellationRequested)
            {
                Monitor.Enter(_lockObject);
                QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(queueListItem));
                Monitor.Exit(_lockObject);
            }
        }

        private ProgressThrottler<DownloadProgress> SetupThrottledQueueListProgress(QueueListItem queueListItem)
        {
            var progressHandler = new Progress<DownloadProgress>(value => queueListItem.Progress = value.Progress);
            return new ProgressThrottler<DownloadProgress>(progressHandler, _shellService.Settings.ProgressUpdateInterval);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _crawlerCancellationTokenSource?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CheckTextVis()
        {
            if (_shellService.Settings.HideToolBarButtonsText)
            {
                _crawlerService.IsTextVis = "Collapsed";
                _crawlerService.IsToolTipActive = true;
            }
            else
            {
                _crawlerService.IsTextVis = "Visible";
                _crawlerService.IsToolTipActive = false;
            }
        }
    }
}
