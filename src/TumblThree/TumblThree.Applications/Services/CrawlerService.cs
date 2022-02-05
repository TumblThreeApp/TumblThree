using Guava.RateLimiter;
using System;
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
using TumblThree.Domain.Models;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ICrawlerService))]
    [Export]
    public class CrawlerService : Model, ICrawlerService
    {
        private readonly ObservableCollection<Collection> _collections;
        private readonly ObservableCollection<QueueListItem> _activeItems;
        private readonly ReadOnlyObservableList<QueueListItem> _readonlyActiveItems;
        private readonly IShellService _shellService;
        private ICommand _addBlogCommand;
        private ICommand _importBlogsCommand;
        private ICommand _autoDownloadCommand;
        private ICommand _crawlCommand;
        private ICommand _enqueueSelectedCommand;
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
        private ICommand _resumeCommand;
        private ICommand _showFilesCommand;
        private ICommand _stopCommand;
        private RateLimiter _timeconstraintApi;
        private RateLimiter _timeconstraintSearchApi;
        private RateLimiter _timeconstraintSvc;
        private Timer _timer;

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

        public void AddActiveItems(QueueListItem itemToAdd) => _activeItems.Add(itemToAdd);

        public void RemoveActiveItem(QueueListItem itemToRemove) => _activeItems.Remove(itemToRemove);

        public void ClearItems() => _activeItems.Clear();

        public ICollectionView Collections { get; }

        public void UpdateCollectionsList(bool isInit)
        {
            _collections.Clear();

            foreach (var item in _shellService.Settings.Collections.OrderBy(x => x.Name))
            {
                _collections.Add(item);
            }

            var ecv = (IEditableCollectionView)Collections;
            if (ecv.IsAddingNew) ecv.CancelNew();
            Collections.Refresh();
            Collection current = _collections.FirstOrDefault(x => x.Id == _shellService.Settings.ActiveCollectionId) ?? _collections.FirstOrDefault(x => x.Id == 0);
            Collections.MoveCurrentTo(current);
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
