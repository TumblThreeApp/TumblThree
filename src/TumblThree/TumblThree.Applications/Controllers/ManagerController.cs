using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Windows.Forms;
using TumblThree.Applications.Crawler;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;
using TumblThree.Domain.Queue;

using Clipboard = System.Windows.Clipboard;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class ManagerController : IDisposable
    {
        private readonly IBlogFactory _blogFactory;
        private readonly ICrawlerService _crawlerService;
        private readonly IClipboardService _clipboardService;
        private readonly ICrawlerFactory _crawlerFactory;
        private readonly IManagerService _managerService;
        private readonly Lazy<ManagerViewModel> _managerViewModel;
        private readonly IMessageService _messageService;
        private readonly ISelectionService _selectionService;
        private readonly IShellService _shellService;
        private readonly ISettingsService _settingsService;
        private readonly ITumblrBlogDetector _tumblrBlogDetector;

        private readonly AsyncDelegateCommand _checkStatusCommand;
        private readonly DelegateCommand _copyUrlCommand;
        private readonly DelegateCommand _checkIfDatabasesCompleteCommand;
        private readonly AsyncDelegateCommand _importBlogsCommand;
        private readonly AsyncDelegateCommand _addBlogCommand;
        private readonly DelegateCommand _autoDownloadCommand;
        private readonly DelegateCommand _enqueueSelectedCommand;
        private readonly DelegateCommand _listenClipboardCommand;
        private readonly AsyncDelegateCommand _loadLibraryCommand;
        private readonly AsyncDelegateCommand _loadAllDatabasesCommand;
        private readonly DelegateCommand _removeBlogCommand;
        private readonly DelegateCommand _showDetailsCommand;
        private readonly DelegateCommand _showFilesCommand;
        private readonly DelegateCommand _visitBlogCommand;
        private readonly DelegateCommand _visitBlogOnTumbexCommand;

        private readonly SemaphoreSlim _addBlogSemaphoreSlim = new SemaphoreSlim(1);
        private readonly object _lockObject = new object();

        public delegate void BlogManagerFinishedLoadingLibraryHandler(object sender, EventArgs e);

        public delegate void BlogManagerFinishedLoadingDatabasesHandler(object sender, EventArgs e);

        [ImportingConstructor]
        public ManagerController(IShellService shellService, ISelectionService selectionService, ICrawlerService crawlerService,
            ISettingsService settingsService, IClipboardService clipboardService, IManagerService managerService,
            ICrawlerFactory crawlerFactory, IBlogFactory blogFactory, ITumblrBlogDetector tumblrBlogDetector,
            IMessageService messageService, Lazy<ManagerViewModel> managerViewModel)
        {
            _shellService = shellService;
            _selectionService = selectionService;
            _clipboardService = clipboardService;
            _crawlerService = crawlerService;
            _managerService = managerService;
            _managerViewModel = managerViewModel;
            _settingsService = settingsService;
            _messageService = messageService;
            _crawlerFactory = crawlerFactory;
            _blogFactory = blogFactory;
            _tumblrBlogDetector = tumblrBlogDetector;
            _importBlogsCommand = new AsyncDelegateCommand(ImportBlogs);
            _addBlogCommand = new AsyncDelegateCommand(AddBlog, CanAddBlog);
            _removeBlogCommand = new DelegateCommand(RemoveBlog, CanRemoveBlog);
            _showFilesCommand = new DelegateCommand(ShowFiles, CanShowFiles);
            _visitBlogCommand = new DelegateCommand(VisitBlog, CanVisitBlog);
            _visitBlogOnTumbexCommand = new DelegateCommand(VisitBlogOnTumbex, CanVisitBlog);
            _enqueueSelectedCommand = new DelegateCommand(EnqueueSelected, CanEnqueueSelected);
            _loadLibraryCommand = new AsyncDelegateCommand(LoadLibraryAsync, CanLoadLibrary);
            _loadAllDatabasesCommand = new AsyncDelegateCommand(LoadAllDatabasesAsync, CanLoadAllDatbases);
            _checkIfDatabasesCompleteCommand = new DelegateCommand(CheckIfDatabasesComplete, CanCheckIfDatabasesComplete);
            _listenClipboardCommand = new DelegateCommand(ListenClipboard);
            _autoDownloadCommand = new DelegateCommand(EnqueueAutoDownload, CanEnqueueAutoDownload);
            _showDetailsCommand = new DelegateCommand(ShowDetailsCommand);
            _copyUrlCommand = new DelegateCommand(CopyUrl, CanCopyUrl);
            _checkStatusCommand = new AsyncDelegateCommand(CheckStatusAsync, CanCheckStatus);
        }

        private ManagerViewModel ManagerViewModel => _managerViewModel.Value;

        public ManagerSettings ManagerSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        public event BlogManagerFinishedLoadingLibraryHandler BlogManagerFinishedLoadingLibrary;

        public event BlogManagerFinishedLoadingDatabasesHandler BlogManagerFinishedLoadingDatabases;

        public async Task InitializeAsync()
        {
            _crawlerService.ImportBlogsCommand = _importBlogsCommand;
            _crawlerService.AddBlogCommand = _addBlogCommand;
            _crawlerService.RemoveBlogCommand = _removeBlogCommand;
            _crawlerService.ShowFilesCommand = _showFilesCommand;
            _crawlerService.EnqueueSelectedCommand = _enqueueSelectedCommand;
            _crawlerService.LoadLibraryCommand = _loadLibraryCommand;
            _crawlerService.LoadAllDatabasesCommand = _loadAllDatabasesCommand;
            _crawlerService.CheckIfDatabasesCompleteCommand = _checkIfDatabasesCompleteCommand;
            _crawlerService.AutoDownloadCommand = _autoDownloadCommand;
            _crawlerService.ListenClipboardCommand = _listenClipboardCommand;
            _crawlerService.PropertyChanged += CrawlerServicePropertyChanged;

            ManagerViewModel.ShowFilesCommand = _showFilesCommand;
            ManagerViewModel.VisitBlogCommand = _visitBlogCommand;
            ManagerViewModel.VisitBlogOnTumbexCommand = _visitBlogOnTumbexCommand;
            ManagerViewModel.ShowDetailsCommand = _showDetailsCommand;
            ManagerViewModel.CopyUrlCommand = _copyUrlCommand;
            ManagerViewModel.CheckStatusCommand = _checkStatusCommand;

            ManagerViewModel.PropertyChanged += ManagerViewModelPropertyChanged;

            ManagerViewModel.QueueItems = QueueManager.Items;
            QueueManager.Items.CollectionChanged += QueueItemsCollectionChanged;
            ManagerViewModel.QueueItems.CollectionChanged += ManagerViewModel.QueueItemsCollectionChanged;
            BlogManagerFinishedLoadingLibrary += OnBlogManagerFinishedLoadingLibrary;
            BlogManagerFinishedLoadingDatabases += OnBlogManagerFinishedLoadingDatabases;

            _shellService.ContentView = ManagerViewModel.View;

            // Refresh command availability on selection change.
            ManagerViewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName != nameof(ManagerViewModel.SelectedBlogFile))
                {
                    return;
                }

                _showFilesCommand.RaiseCanExecuteChanged();
                _visitBlogCommand.RaiseCanExecuteChanged();
                _visitBlogOnTumbexCommand.RaiseCanExecuteChanged();
                _showDetailsCommand.RaiseCanExecuteChanged();
                _copyUrlCommand.RaiseCanExecuteChanged();
                _checkStatusCommand.RaiseCanExecuteChanged();
            };

            if (_shellService.Settings.CheckClipboard)
            {
                _shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
            }

            await LoadDataBasesAsync();
        }

        public void Shutdown()
        {
        }

        private void OnBlogManagerFinishedLoadingLibrary(object sender, EventArgs e) =>
            _crawlerService.LibraryLoaded.SetResult(true);

        private void OnBlogManagerFinishedLoadingDatabases(object sender, EventArgs e) =>
            _crawlerService.DatabasesLoaded.SetResult(true);

        private void QueueItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add | e.Action == NotifyCollectionChangedAction.Remove)
            {
                ManagerViewModel.QueueItems = QueueManager.Items;
            }
        }

        private async Task LoadDataBasesAsync()
        {
            // TODO: Methods have side effects!
            // They remove blogs from the blog manager.
            await LoadLibraryAsync();
            await LoadAllDatabasesAsync();
            CheckIfDatabasesComplete();
            await CheckBlogsOnlineStatusAsync();
        }

        private async Task LoadLibraryAsync()
        {
            Logger.Verbose("ManagerController.LoadLibrary:Start");
            _managerService.BlogFiles.Clear();
            string path = Path.Combine(_shellService.Settings.DownloadLocation, "Index");

            if (Directory.Exists(path))
            {
                IReadOnlyList<IBlog> files = await GetIBlogsAsync(path);
                foreach (IBlog file in files)
                {
                    _managerService.BlogFiles.Add(file);
                }
            }

            BlogManagerFinishedLoadingLibrary?.Invoke(this, EventArgs.Empty);
            Logger.Verbose("ManagerController.LoadLibrary:End");
        }

        //TODO: Refactor and extract blog loading.
        private Task<IReadOnlyList<IBlog>> GetIBlogsAsync(string directory) => Task.Run(() => GetIBlogsCore(directory));

        private IReadOnlyList<IBlog> GetIBlogsCore(string directory)
        {
            Logger.Verbose("ManagerController:GetIBlogsCore Start");

            var blogs = new List<IBlog>();
            var failedToLoadBlogs = new List<string>();

            string[] supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (string filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) &&
                            !fileName.Contains("_files")))
            {
                //TODO: Refactor
                try
                {
                    if (filename.EndsWith(BlogTypes.tumblr.ToString()))
                    {
                        blogs.Add(new TumblrBlog().Load(filename));
                    }

                    if (filename.EndsWith(BlogTypes.tmblrpriv.ToString()))
                    {
                        blogs.Add(new TumblrHiddenBlog().Load(filename));
                    }

                    if (filename.EndsWith(BlogTypes.tlb.ToString()))
                    {
                        blogs.Add(new TumblrLikedByBlog().Load(filename));
                    }

                    if (filename.EndsWith(BlogTypes.tumblrsearch.ToString()))
                    {
                        blogs.Add(new TumblrSearchBlog().Load(filename));
                    }

                    if (filename.EndsWith(BlogTypes.tumblrtagsearch.ToString()))
                    {
                        blogs.Add(new TumblrTagSearchBlog().Load(filename));
                    }
                }
                catch (SerializationException ex)
                {
                    failedToLoadBlogs.Add(ex.Data["Filename"].ToString());
                }
            }

            if (failedToLoadBlogs.Any())
            {
                string failedBlogNames = failedToLoadBlogs.Aggregate((a, b) => a + ", " + b);
                Logger.Verbose("ManagerController:GetIBlogsCore: {0}", failedBlogNames);
                _shellService.ShowError(new SerializationException(), Resources.CouldNotLoadLibrary, failedBlogNames);
            }

            Logger.Verbose("ManagerController.GetIBlogsCore End");

            return blogs;
        }

        private async Task LoadAllDatabasesAsync()
        {
            Logger.Verbose("ManagerController.LoadAllDatabasesAsync:Start");
            _managerService.ClearDatabases();
            string path = Path.Combine(_shellService.Settings.DownloadLocation, "Index");

            if (Directory.Exists(path))
            {
                IReadOnlyList<IFiles> databases = await GetIFilesAsync(path);
                foreach (IFiles database in databases)
                {
                    _managerService.AddDatabase(database);
                }
            }

            BlogManagerFinishedLoadingDatabases?.Invoke(this, EventArgs.Empty);
            Logger.Verbose("ManagerController.LoadAllDatabasesAsync:End");
        }

        private Task<IReadOnlyList<IFiles>> GetIFilesAsync(string directory) => Task.Run(() => GetIFilesCore(directory));

        private IReadOnlyList<IFiles> GetIFilesCore(string directory)
        {
            Logger.Verbose("ManagerController:GetFilesCore Start");

            var databases = new List<IFiles>();
            var failedToLoadDatabases = new List<string>();

            string[] supportedFileTypes = Enum.GetNames(typeof(BlogTypes)).ToArray();

            foreach (string filename in Directory.GetFiles(directory, "*").Where(
                fileName => supportedFileTypes.Any(fileName.Contains) &&
                            fileName.Contains("_files")))
            {
                //TODO: Refactor
                try
                {
                    IFiles database = new Files().Load(filename);
                    if (_shellService.Settings.LoadAllDatabases)
                    {
                        databases.Add(database);
                    }
                }
                catch (SerializationException ex)
                {
                    failedToLoadDatabases.Add(ex.Data["Filename"].ToString());
                }
            }

            if (failedToLoadDatabases.Any())
            {
                IEnumerable<IBlog> blogs = _managerService.BlogFiles;
                IEnumerable<IBlog> failedToLoadBlogs = blogs.Where(blog => failedToLoadDatabases.Contains(blog.ChildId));

                string failedBlogNames = failedToLoadDatabases.Aggregate((a, b) => a + ", " + b);
                Logger.Verbose("ManagerController:GetIFilesCore: {0}", failedBlogNames);
                _shellService.ShowError(new SerializationException(), Resources.CouldNotLoadLibrary, failedBlogNames);

                foreach (IBlog failedToLoadBlog in failedToLoadBlogs)
                {
                    _managerService.BlogFiles.Remove(failedToLoadBlog);
                }
            }

            Logger.Verbose("ManagerController.GetFilesCore End");

            return databases;
        }

        private void CheckIfDatabasesComplete()
        {
            IEnumerable<IBlog> blogs = _managerService.BlogFiles;
            List<IBlog> incompleteBlogs = blogs.Where(blog => !File.Exists(blog.ChildId)).ToList();

            if (!incompleteBlogs.Any())
            {
                return;
            }

            string incompleteBlogNames = incompleteBlogs.Select(blog => blog.ChildId).Aggregate((a, b) => a + ", " + b);
            Logger.Verbose("ManagerController:CheckIfDatabasesComplete: {0}", incompleteBlogNames);
            _shellService.ShowError(new SerializationException(), Resources.CouldNotLoadLibrary, incompleteBlogNames);

            foreach (IBlog incompleteBlog in incompleteBlogs)
            {
                _managerService.BlogFiles.Remove(incompleteBlog);
            }
        }

        private async Task CheckBlogsOnlineStatusAsync()
        {
            if (_shellService.Settings.CheckOnlineStatusOnStartup)
            {
                IEnumerable<IBlog> blogs = _managerService.BlogFiles;
                await Task.Run(() => ThrottledCheckStatusOfBlogsAsync(blogs));
            }
        }

        private async Task CheckStatusAsync()
        {
            IEnumerable<IBlog> blogs = _selectionService.SelectedBlogFiles.ToArray();
            await Task.Run(() => ThrottledCheckStatusOfBlogsAsync(blogs));
        }

        private async Task ThrottledCheckStatusOfBlogsAsync(IEnumerable<IBlog> blogs)
        {
            var semaphoreSlim = new SemaphoreSlim(25);
            IEnumerable<Task> tasks = blogs.Select(async blog => await CheckStatusOfBlogsAsync(semaphoreSlim, blog));
            await Task.WhenAll(tasks);
        }

        private async Task CheckStatusOfBlogsAsync(SemaphoreSlim semaphoreSlim, IBlog blog)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                ICrawler crawler = _crawlerFactory.GetCrawler(blog, new Progress<DownloadProgress>(), new PauseToken(),
                    new CancellationToken());
                await crawler.IsBlogOnlineAsync();
                crawler.Dispose();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private bool CanLoadLibrary() => !_crawlerService.IsCrawl;

        private bool CanLoadAllDatbases() => !_crawlerService.IsCrawl;

        private bool CanCheckIfDatabasesComplete() => _crawlerService.DatabasesLoaded.Task.GetAwaiter().IsCompleted &&
                                                      _crawlerService.LibraryLoaded.Task.GetAwaiter().IsCompleted;

        private bool CanEnqueueSelected() => ManagerViewModel.SelectedBlogFile != null && ManagerViewModel.SelectedBlogFile.Online;

        private void EnqueueSelected() => Enqueue(_selectionService.SelectedBlogFiles.Where(blog => blog.Online).ToArray());

        private void Enqueue(IEnumerable<IBlog> blogFiles) => QueueManager.AddItems(blogFiles.Select(x => new QueueListItem(x)));

        private bool CanEnqueueAutoDownload() => _managerService.BlogFiles.Any();

        private void EnqueueAutoDownload()
        {
            if (_shellService.Settings.BlogType == _shellService.Settings.BlogTypes.ElementAtOrDefault(0))
            {
            }

            if (_shellService.Settings.BlogType == _shellService.Settings.BlogTypes.ElementAtOrDefault(1))
            {
                Enqueue(_managerService.BlogFiles.Where(blog => blog.Online).ToArray());
            }

            if (_shellService.Settings.BlogType == _shellService.Settings.BlogTypes.ElementAtOrDefault(2))
            {
                Enqueue(
                    _managerService
                        .BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl != new DateTime(0L, DateTimeKind.Utc))
                        .ToArray());
            }

            if (_shellService.Settings.BlogType == _shellService.Settings.BlogTypes.ElementAtOrDefault(3))
            {
                Enqueue(
                    _managerService
                        .BlogFiles.Where(blog => blog.Online && blog.LastCompleteCrawl == new DateTime(0L, DateTimeKind.Utc))
                        .ToArray());
            }

            if (_crawlerService.IsCrawl && _crawlerService.IsPaused)
            {
                _crawlerService.ResumeCommand.CanExecute(null);
                _crawlerService.ResumeCommand.Execute(null);
            }
            else if (!_crawlerService.IsCrawl)
            {
                _crawlerService.CrawlCommand.CanExecute(null);
                _crawlerService.CrawlCommand.Execute(null);
            }
        }

        private bool CanAddBlog() => _blogFactory.IsValidTumblrBlogUrl(_crawlerService.NewBlogUrl);

        private async Task AddBlog()
        {
            try
            {
                await AddBlogAsync(null);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        private async Task ImportBlogs()
        {
            try
            {
                var fileBrowser = new OpenFileDialog()
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
                };

                if (fileBrowser.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                var path = fileBrowser.FileName;

                if (!File.Exists(path))
                {
                    Logger.Warning("ManagerController:ImportBlogs: An attempt was made to import blogs from a file which doesn't exist.");
                    return;
                }

                string fileContent;

                using (var streamReader = new StreamReader(path))
                {
                    fileContent = await streamReader.ReadToEndAsync();
                }

                var blogUris = fileContent.Split().Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x));

                await Task.Run(() => AddBlogBatchedAsync(blogUris));
            }
            catch (Exception ex)
            {
                Logger.Error($"ManagerController:ImportBlogs: {ex}");
            }
        }

        private bool CanRemoveBlog() => ManagerViewModel.SelectedBlogFile != null;

        private void RemoveBlog()
        {
            IBlog[] blogs = _selectionService.SelectedBlogFiles.ToArray();

            if (_shellService.Settings.DisplayConfirmationDialog)
            {
                var blogNames = string.Join(", ", blogs.Select(blog => blog.Name));
                var message = string.Format(
                    _shellService.Settings.DeleteOnlyIndex ? Resources.DeleteBlogsDialog : Resources.DeleteBlogsAndFilesDialog,
                    blogNames);

                if (!_messageService.ShowYesNoQuestion(this, message))
                {
                    return;
                }
            }

            RemoveBlog(blogs);
        }

        private void RemoveBlog(IEnumerable<IBlog> blogs)
        {
            foreach (IBlog blog in blogs)
            {
                if (!_shellService.Settings.DeleteOnlyIndex)
                {
                    try
                    {
                        string blogPath = blog.DownloadLocation();
                        Directory.Delete(blogPath, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                        _shellService.ShowError(ex, Resources.CouldNotRemoveBlog, blog.Name);
                        return;
                    }
                }

                string indexFile = Path.Combine(blog.Location, blog.Name) + "." + blog.OriginalBlogType;
                try
                {
                    File.Delete(indexFile);
                    File.Delete(blog.ChildId);
                }
                catch (Exception ex)
                {
                    Logger.Error("ManagerController:RemoveBlog: {0}", ex);
                    _shellService.ShowError(ex, Resources.CouldNotRemoveBlogIndex, blog.Name);
                    return;
                }

                _managerService.BlogFiles.Remove(blog);
                if (_shellService.Settings.LoadAllDatabases)
                {
                    _managerService.RemoveDatabase(_managerService.Databases
                                                                .FirstOrDefault(db =>
                                                                    db.Name.Equals(blog.Name) &&
                                                                    db.BlogType.Equals(blog.BlogType)));
                }

                QueueManager.RemoveItems(QueueManager.Items.Where(item => item.Blog.Equals(blog)));
            }
        }

        private bool CanShowFiles() => ManagerViewModel.SelectedBlogFile != null;

        private void ShowFiles()
        {
            foreach (IBlog blog in _selectionService.SelectedBlogFiles.ToArray())
            {
                Process.Start("explorer.exe", blog.DownloadLocation());
            }
        }

        private bool CanVisitBlog() => ManagerViewModel.SelectedBlogFile != null;

        private void VisitBlog()
        {
            foreach (IBlog blog in _selectionService.SelectedBlogFiles.ToArray())
            {
                Process.Start(blog.Url);
            }
        }

        private void VisitBlogOnTumbex()
        {
            foreach (IBlog blog in _selectionService.SelectedBlogFiles.ToArray())
            {
                string tumbexUrl = $"https://www.tumbex.com/{blog.Name}.tumblr/";
                Process.Start(tumbexUrl);
            }
        }

        private void ShowDetailsCommand() => _shellService.ShowDetailsView();

        private void CopyUrl()
        {
            List<string> urls = _selectionService.SelectedBlogFiles.Select(blog => blog.Url).ToList();
            urls.Sort();
            _clipboardService.SetText(string.Join(Environment.NewLine, urls));
        }

        private bool CanCopyUrl() => ManagerViewModel.SelectedBlogFile != null;

        private bool CanCheckStatus() => ManagerViewModel.SelectedBlogFile != null;

        private async Task AddBlogAsync(string blogUrl)
        {
            if (string.IsNullOrEmpty(blogUrl))
            {
                blogUrl = _crawlerService.NewBlogUrl;
            }

            IBlog blog = CheckIfCrawlableBlog(blogUrl);

            blog = await CheckIfBlogIsHiddenTumblrBlogAsync(blog);

            lock (_lockObject)
            {
                if (CheckIfBlogAlreadyExists(blog))
                {
                    return;
                }
                SetDefaultTumblrBlogCrawler(blog);
                SaveBlog(blog);
            }

            blog = _settingsService.TransferGlobalSettingsToBlog(blog);
            await UpdateMetaInformationAsync(blog);
        }

        private void SetDefaultTumblrBlogCrawler(IBlog blog)
        {
            if (_shellService.Settings.OverrideTumblrBlogCrawler)
                if (blog.BlogType == BlogTypes.tumblr || blog.BlogType == BlogTypes.tmblrpriv)
                    blog.BlogType = _shellService.Settings.TumblrBlogCrawlerType.MapToBlogType();
        }

        private void SaveBlog(IBlog blog)
        {
            if (blog.Save())
            {
                AddToManager(blog);
            }
        }

        private bool CheckIfBlogAlreadyExists(IBlog blog)
        {
            if (_managerService.BlogFiles.Any(blogs => blogs.Name.Equals(blog.Name) && (blogs.BlogType.Equals(blog.BlogType) || CheckifBlogsAreTumblrBlogs(blogs, blog))))
            {
                _shellService.ShowError(null, Resources.BlogAlreadyExist, blog.Name);
                return true;
            }

            return false;
        }

        private bool CheckifBlogsAreTumblrBlogs(IBlog blogs, IBlog toMatch) {
            if (blogs.BlogType == BlogTypes.tumblr || blogs.BlogType == BlogTypes.tmblrpriv)
                return toMatch.BlogType == BlogTypes.tumblr || toMatch.BlogType == BlogTypes.tmblrpriv;
            return false;
        }

        private async Task UpdateMetaInformationAsync(IBlog blog)
        {
            ICrawler crawler = _crawlerFactory.GetCrawler(blog, new Progress<DownloadProgress>(), new PauseToken(),
                new CancellationToken());

            await crawler.UpdateMetaInformationAsync();
            crawler.Dispose();
        }

        private IBlog CheckIfCrawlableBlog(string blogUrl)
        {
            return _blogFactory.GetBlog(blogUrl, Path.Combine(_shellService.Settings.DownloadLocation, "Index"));
        }

        private void AddToManager(IBlog blog)
        {
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => _managerService.BlogFiles.Add(blog));
            if (_shellService.Settings.LoadAllDatabases)
            {
                _managerService.AddDatabase(new Files().Load(blog.ChildId));
            }
        }

        private async Task<IBlog> CheckIfBlogIsHiddenTumblrBlogAsync(IBlog blog)
        {
            if (blog.GetType() == typeof(TumblrBlog) && await _tumblrBlogDetector.IsHiddenTumblrBlogAsync(blog.Url))
            {
                RemoveBlog(new[] { blog });
                blog = TumblrHiddenBlog.Create("https://www.tumblr.com/dashboard/blog/" + blog.Name, Path.Combine(_shellService.Settings.DownloadLocation, "Index"));
            }

            return blog;
        }

        private void OnClipboardContentChanged(object sender, EventArgs e)
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    return;
                }

                // Count each whitespace as new url
                string content = Clipboard.GetText();
                if (content == null) return;
                string[] urls = content.Split();

                Task.Run(() => AddBlogBatchedAsync(urls));
            }
            catch (Exception ex)
            {
                Logger.Error($"ManagerController:OnClipboardContentChanged: {ex}");
                _shellService.ShowError(ex, "error getting clipboard content");
            }
        }

        private async Task AddBlogBatchedAsync(IEnumerable<string> urls)
        {
            var semaphoreSlim = new SemaphoreSlim(25);

            await _addBlogSemaphoreSlim.WaitAsync();
            try
            {
                IEnumerable<Task> tasks = urls.Select(async url => await AddBlogsAsync(semaphoreSlim, url));
                await Task.WhenAll(tasks);
            }
            finally
            {
                _addBlogSemaphoreSlim.Release();
            }
        }

        private async Task AddBlogsAsync(SemaphoreSlim semaphoreSlim, string url)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                await AddBlogAsync(url);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private void ListenClipboard()
        {
            if (_shellService.Settings.CheckClipboard)
            {
                _shellService.ClipboardMonitor.OnClipboardContentChanged += OnClipboardContentChanged;
            }
            else
            {
                _shellService.ClipboardMonitor.OnClipboardContentChanged -= OnClipboardContentChanged;
            }
        }

        private void CrawlerServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_crawlerService.NewBlogUrl))
            {
                _addBlogCommand.RaiseCanExecuteChanged();
            }
        }

        private void ManagerViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ManagerViewModel.SelectedBlogFile))
            {
                UpdateCommands();
            }
        }

        private void UpdateCommands()
        {
            _enqueueSelectedCommand.RaiseCanExecuteChanged();
            _removeBlogCommand.RaiseCanExecuteChanged();
            _showFilesCommand.RaiseCanExecuteChanged();
        }

        public void RestoreColumn() => ManagerViewModel.DataGridColumnRestore();

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _addBlogSemaphoreSlim.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
