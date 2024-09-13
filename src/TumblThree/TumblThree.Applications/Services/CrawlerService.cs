using Guava.RateLimiter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Foundation;
using System.Windows.Data;
using System.Windows.Input;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ICrawlerService))]
    [Export]
    public class CrawlerService : Model, ICrawlerService
    {
        private const int MILLISECONDS_PER_MINUTE = 60 * 1000;
        private const int BYTES_PER_MB = 1024 * 1024;

        private readonly ObservableCollection<Collection> _collections;
        private readonly ObservableCollection<QueueListItem> _activeItems;
        private readonly ReadOnlyObservableList<QueueListItem> _readonlyActiveItems;
        private readonly IShellService _shellService;
        private ICommand _addBlogCommand;
        private ICommand _importBlogsCommand;
        private ICommand _autoDownloadCommand;
        private ICommand _crawlCommand;
        private ICommand _enqueueSelectedCommand;
        private ICommand _dequeueSelectedCommand;
        private ICommand _loadLibraryCommand;
        private ICommand _loadAllDatabasesCommand;
        private ICommand _loadArchiveCommand;
        private ICommand _checkIfDatabasesCompleteCommand;
        private bool _isCrawl;
        private bool _isPaused;
        private bool _isTimerSet;
        private TaskCompletionSource<bool> _libraryLoaded;
        private TaskCompletionSource<bool> _databasesLoaded;
        private TaskCompletionSource<bool> _archiveLoaded;
        private ICommand _listenClipboardCommand;
        private string _newBlogUrl;
        private ICommand _pauseCommand;
        private ICommand _removeBlogCommand;
        private ICommand _removeBlogFromQueueCommand;
        private ICommand _removeBlogSelectionFromQueueCommand;
        private ICommand _resumeCommand;
        private ICommand _showFilesCommand;
        private ICommand _stopCommand;
        private RateLimiter _timeconstraintApi;
        private RateLimiter _timeconstraintSearchApi;
        private RateLimiter _timeconstraintSvc;
        private RateLimiter _timeconstraintTwitterApi;
        private Timer _timer;
        private string _isTextVis;
        private bool _isToolTipActive;
        private IBlog _lastDeselectedPreview;
        private Timer _diskSpaceTimer;

        [ImportingConstructor]
        public CrawlerService(IShellService shellService)
        {
            _shellService = shellService;
            _timeconstraintApi =
                RateLimiter.Create(_shellService.Settings.MaxConnectionsApi /
                                   (double)_shellService.Settings.ConnectionTimeIntervalApi);

            _timeconstraintSearchApi =
                RateLimiter.Create(_shellService.Settings.MaxConnectionsSearchApi /
                                   (double)_shellService.Settings.ConnectionTimeIntervalSearchApi);

            _timeconstraintSvc =
                RateLimiter.Create(_shellService.Settings.MaxConnectionsSvc /
                       (double)_shellService.Settings.ConnectionTimeIntervalSvc);

            _timeconstraintTwitterApi =
                RateLimiter.Create(_shellService.Settings.MaxConnectionsTwitterApi /
                       (double)_shellService.Settings.ConnectionTimeIntervalTwitterApi);

            _activeItems = new ObservableCollection<QueueListItem>();
            _readonlyActiveItems = new ReadOnlyObservableList<QueueListItem>(_activeItems);
            _libraryLoaded = new TaskCompletionSource<bool>();
            _databasesLoaded = new TaskCompletionSource<bool>();
            _archiveLoaded = new TaskCompletionSource<bool>();
            _activeItems.CollectionChanged += ActiveItemsCollectionChanged;

            _collections = new ObservableCollection<Collection>();
            Collections = CollectionViewSource.GetDefaultView(_collections);
            Collections.CurrentChanged += Collections_CurrentChanged;
        }

        private void Collections_CurrentChanged(object sender, EventArgs e)
        {
            if (Collections.CurrentItem == null) return;

            _shellService.Settings.ActiveCollectionId = (Collections.CurrentItem as Collection)?.Id ?? 0;
            ActiveCollectionIdChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ActiveCollectionIdChanged;

        public string IsTextVis
        {
            get => _isTextVis;
            set => SetProperty(ref _isTextVis, value);
        }

        public bool IsToolTipActive
        {
            get => _isToolTipActive;
            set => SetProperty(ref _isToolTipActive, value);
        }

        public bool IsTimerSet
        {
            get => _isTimerSet;
            set => SetProperty(ref _isTimerSet, value);
        }

        public TaskCompletionSource<bool> LibraryLoaded
        {
            get => _libraryLoaded;
            set => SetProperty(ref _libraryLoaded, value);
        }

        public TaskCompletionSource<bool> DatabasesLoaded
        {
            get => _databasesLoaded;
            set => SetProperty(ref _databasesLoaded, value);
        }

        public TaskCompletionSource<bool> ArchiveLoaded
        {
            get => _archiveLoaded;
            set => SetProperty(ref _archiveLoaded, value);
        }

        public Timer Timer
        {
            get => _timer;
            set => SetProperty(ref _timer, value);
        }

        public IReadOnlyObservableList<QueueListItem> ActiveItems => _readonlyActiveItems;

        public ICommand ImportBlogsCommand
        {
            get => _importBlogsCommand;
            set => SetProperty(ref _importBlogsCommand, value);
        }

        public ICommand AddBlogCommand
        {
            get => _addBlogCommand;
            set => SetProperty(ref _addBlogCommand, value);
        }

        public ICommand RemoveBlogCommand
        {
            get => _removeBlogCommand;
            set => SetProperty(ref _removeBlogCommand, value);
        }

        public ICommand ShowFilesCommand
        {
            get => _showFilesCommand;
            set => SetProperty(ref _showFilesCommand, value);
        }

        public ICommand EnqueueSelectedCommand
        {
            get => _enqueueSelectedCommand;
            set => SetProperty(ref _enqueueSelectedCommand, value);
        }

        public ICommand DequeueSelectedCommand
        {
            get => _dequeueSelectedCommand;
            set => SetProperty(ref _dequeueSelectedCommand, value);
        }

        public ICommand LoadLibraryCommand
        {
            get => _loadLibraryCommand;
            set => SetProperty(ref _loadLibraryCommand, value);
        }

        public ICommand LoadAllDatabasesCommand
        {
            get => _loadAllDatabasesCommand;
            set => SetProperty(ref _loadAllDatabasesCommand, value);
        }

        public ICommand LoadArchiveCommand
        {
            get => _loadArchiveCommand;
            set => SetProperty(ref _loadArchiveCommand, value);
        }

        public ICommand CheckIfDatabasesCompleteCommand
        {
            get => _checkIfDatabasesCompleteCommand;
            set => SetProperty(ref _checkIfDatabasesCompleteCommand, value);
        }

        public ICommand RemoveBlogFromQueueCommand
        {
            get => _removeBlogFromQueueCommand;
            set => SetProperty(ref _removeBlogFromQueueCommand, value);
        }

        public ICommand RemoveBlogSelectionFromQueueCommand
        {
            get => _removeBlogSelectionFromQueueCommand;
            set => SetProperty(ref _removeBlogSelectionFromQueueCommand, value);
        }

        public ICommand ListenClipboardCommand
        {
            get => _listenClipboardCommand;
            set => SetProperty(ref _listenClipboardCommand, value);
        }

        public ICommand CrawlCommand
        {
            get => _crawlCommand;
            set => SetProperty(ref _crawlCommand, value);
        }

        public ICommand PauseCommand
        {
            get => _pauseCommand;
            set => SetProperty(ref _pauseCommand, value);
        }

        public ICommand ResumeCommand
        {
            get => _resumeCommand;
            set => SetProperty(ref _resumeCommand, value);
        }

        public ICommand StopCommand
        {
            get => _stopCommand;
            set => SetProperty(ref _stopCommand, value);
        }

        public ICommand AutoDownloadCommand
        {
            get => _autoDownloadCommand;
            set => SetProperty(ref _autoDownloadCommand, value);
        }

        public bool DequeueSelectedCommandVisible
        {
            get => _shellService.Settings.DequeueSelectedCommandVisible;
        }

        public bool IsCrawl
        {
            get => _isCrawl;
            set => SetProperty(ref _isCrawl, value);
        }

        public bool IsPaused
        {
            get => _isPaused;
            set => SetProperty(ref _isPaused, value);
        }

        public string NewBlogUrl
        {
            get => _newBlogUrl;
            set => SetProperty(ref _newBlogUrl, value);
        }

        public IBlog LastDeselectedPreview
        {
            get => _lastDeselectedPreview;
            set => SetProperty(ref _lastDeselectedPreview, value);
        }

        public int ActiveCollectionId
        {
            get
            {
                return _shellService.Settings.ActiveCollectionId;
            }
            set
            {
                _shellService.Settings.ActiveCollectionId = value;
                ActiveCollectionIdChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public RateLimiter TimeconstraintApi
        {
            get => _timeconstraintApi;
            set => SetProperty(ref _timeconstraintApi, value);
        }

        public RateLimiter TimeconstraintSearchApi
        {
            get => _timeconstraintSearchApi;
            set => SetProperty(ref _timeconstraintSearchApi, value);
        }

        public RateLimiter TimeconstraintSvc
        {
            get => _timeconstraintSvc;
            set => SetProperty(ref _timeconstraintSvc, value);
        }

        public RateLimiter TimeconstraintTwitterApi
        {
            get => _timeconstraintTwitterApi;
            set => SetProperty(ref _timeconstraintTwitterApi, value);
        }

        public void AddActiveItems(QueueListItem itemToAdd) => _activeItems.Add(itemToAdd);

        public void RemoveActiveItem(QueueListItem itemToRemove) => _activeItems.Remove(itemToRemove);

        public void ClearItems() => _activeItems.Clear();

        public ICollectionView Collections { get; }

        public void UpdateCollectionsList(bool isInit)
        {
            _collections.Clear();

            foreach (var item in _shellService.Settings.Collections.Where(x => isInit || x.IsOnline.Value).OrderBy(x => x.Name))
            {
                _collections.Add(item);
            }

            //var ecv = (IEditableCollectionView)Collections;
            //if (ecv.IsAddingNew) ecv.CancelNew();
            //Collections.Refresh();
            Collection current = _collections.FirstOrDefault(x => x.Id == _shellService.Settings.ActiveCollectionId) ?? _collections.FirstOrDefault(x => x.Id == 0);
            Collections.MoveCurrentTo(current);
        }

        public void StartFreeDiskSpaceMonitor()
        {
            if (!_shellService.Settings.FreeDiskSpaceMonitorEnabled) return;

            _diskSpaceTimer = new Timer(x => { OnTimedEvent(); }, null, 2 * 1000, _shellService.Settings.FreeDiskSpaceMonitorInterval * MILLISECONDS_PER_MINUTE);
        }

        public void StopFreeDiskSpaceMonitor()
        {
            if (_diskSpaceTimer != null)
            {
                _diskSpaceTimer.Dispose();
                _diskSpaceTimer = null;
            }
        }

        private void OnTimedEvent()
        {
            try
            {
                List<string> checkedLocations = new List<string>();
                foreach (var item in ActiveItems.ToArray())
                {
                    if (!ActiveItems.Contains(item)) continue;
                    var location = _shellService.Settings.GetCollection(item.Blog.CollectionId).DownloadLocation;

                    if (checkedLocations.Contains(location)) continue;
                    checkedLocations.Add(location);

                    ulong freeBytesAvailable = 0, totalNumberOfBytes = 0, totalNumberOfFreeBytes = 0;
                    var success = NativeMethods.GetDiskFreeSpaceEx(location, out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
                    if (success && (long)freeBytesAvailable <= _shellService.Settings.FreeDiskSpaceMonitorLevel * BYTES_PER_MB)
                    {
                        StopFreeDiskSpaceMonitor();
                        _shellService.ShowError(new Exception(string.Format(Resources.LowDiskSpaceError, location)), Resources.LowDiskSpaceMsg, location);
                        QueueOnDispatcher.CheckBeginInvokeOnUI(() => { if (_pauseCommand.CanExecute(null)) _pauseCommand.Execute(null); });
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("CrawlerService.OnTimedEvent: {0}", ex);
            }
        }

        private void ActiveItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove)
            {
                RaisePropertyChanged("ActiveItems");
            }
        }
    }
}
