using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;

using TumblThree.Applications.Crawler;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
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
        private readonly IManagerService _managerService;
        private readonly IShellService _shellService;

        private readonly AsyncDelegateCommand _crawlCommand;
        private readonly DelegateCommand _pauseCommand;
        private readonly DelegateCommand _resumeCommand;
        private readonly DelegateCommand _stopCommand;

        private readonly object _lockObject;
        private readonly List<Task> _runningTasks;
        private CancellationTokenSource _crawlerCancellationTokenSource;
        private PauseTokenSource _crawlerPauseTokenSource;

        [ImportingConstructor]
        public CrawlerController(IShellService shellService, IManagerService managerService, ICrawlerService crawlerService,
            ICrawlerFactory crawlerFactory, Lazy<CrawlerViewModel> crawlerViewModel)
        {
            _shellService = shellService;
            _managerService = managerService;
            _crawlerService = crawlerService;
            _crawlerViewModel = crawlerViewModel;
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

            foreach (IBlog blog in _managerService.BlogFiles)
            {
                if (blog.Dirty)
                {
                    blog.Save();
                }
            }
        }

        private bool CanStop() => _crawlerService.IsCrawl;

        private void Stop()
        {
            if (_resumeCommand.CanExecute(null))
            {
                _resumeCommand.Execute(null);
            }

            _crawlerCancellationTokenSource.Cancel();
            _crawlerService.IsCrawl = false;
            _crawlCommand.RaiseCanExecuteChanged();
            _pauseCommand.RaiseCanExecuteChanged();
            _resumeCommand.RaiseCanExecuteChanged();
            _stopCommand.RaiseCanExecuteChanged();
        }

        private bool CanPause() => _crawlerService.IsCrawl && !_crawlerService.IsPaused;

        private void Pause()
        {
            _crawlerPauseTokenSource.PauseWithResponseAsync().Wait();
            _crawlerService.IsPaused = true;
            _pauseCommand.RaiseCanExecuteChanged();
            _resumeCommand.RaiseCanExecuteChanged();
        }

        private bool CanResume() => _crawlerService.IsCrawl && _crawlerService.IsPaused;

        private void Resume()
        {
            _crawlerPauseTokenSource.Resume();
            _crawlerService.IsPaused = false;
            _pauseCommand.RaiseCanExecuteChanged();
            _resumeCommand.RaiseCanExecuteChanged();
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

            await Task.WhenAll(_crawlerService.LibraryLoaded.Task, _crawlerService.DatabasesLoaded.Task);

            for (var i = 0; i < _shellService.Settings.ConcurrentBlogs; i++)
            {
                _runningTasks.Add(Task.Run(() =>
                    RunCrawlerTasksAsync(_crawlerPauseTokenSource.Token, _crawlerCancellationTokenSource.Token)));
            }

            await CrawlAsync();
        }

        private async Task CrawlAsync()
        {
            try
            {
                await Task.WhenAll(_runningTasks.ToArray());
            }
            catch
            {
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
                    pt.WaitWhilePausedWithResponseAsyc().Wait();
                }

                Monitor.Enter(_lockObject);
                if (_crawlerService.ActiveItems.Count < QueueManager.Items.Count)
                {
                    IEnumerable<QueueListItem> queueList = QueueManager.Items.Except(_crawlerService.ActiveItems);
                    QueueListItem nextQueueItem = queueList.First();
                    IBlog blog = nextQueueItem.Blog;

                    ICrawler crawler = _crawlerFactory.GetCrawler(blog, new Progress<DownloadProgress>(), pt, ct);
                    crawler.IsBlogOnlineAsync().Wait(4000);
                    crawler.Dispose();

                    if (_crawlerService.ActiveItems.Any(item =>
                        item.Blog.Name.Equals(nextQueueItem.Blog.Name) &&
                        item.Blog.BlogType.Equals(nextQueueItem.Blog.BlogType)))
                    {
                        QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(nextQueueItem));
                        Monitor.Exit(_lockObject);
                        continue;
                    }

                    if (!nextQueueItem.Blog.Online)
                    {
                        QueueOnDispatcher.CheckBeginInvokeOnUI(() => QueueManager.RemoveItem(nextQueueItem));
                        Monitor.Exit(_lockObject);
                        continue;
                    }

                    _crawlerService.AddActiveItems(nextQueueItem);
                    Monitor.Exit(_lockObject);
                    await StartSiteSpecificDownloaderAsync(nextQueueItem, pt, ct);
                }
                else
                {
                    Monitor.Exit(_lockObject);
                    await Task.Delay(4000, ct);
                }
            }
        }

        private async Task StartSiteSpecificDownloaderAsync(QueueListItem queueListItem, PauseToken pt, CancellationToken ct)
        {
            IBlog blog = queueListItem.Blog;
            blog.Dirty = true;
            ProgressThrottler<DownloadProgress> progress = SetupThrottledQueueListProgress(queueListItem);

            ICrawler crawler = _crawlerFactory.GetCrawler(blog, progress, pt, ct);
            await crawler.CrawlAsync();
            crawler.Dispose();

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
            var progressHandler = new Progress<DownloadProgress>(value => { queueListItem.Progress = value.Progress; });
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
    }
}
