using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications.Services;
using System.Waf.Foundation;
using System.Windows;
using System.Windows.Data;
using TumblThree.Applications.Properties;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    [Export]
    [Export(typeof(IManagerService))]
    internal class ManagerService : Model, IManagerService
    {
        private readonly IList<IFiles> databases;
        private readonly ReaderWriterLockSlim databasesLock = new ReaderWriterLockSlim();
        private readonly IList<IFiles> archiveDatabases;
        private readonly ReaderWriterLockSlim archivesLock = new ReaderWriterLockSlim();
        private readonly ICollectionView blogFilesView;
        private static object blogFilesLock = new object();
        private readonly IShellService shellService;
        private readonly IMessageService messageService;
        private readonly IGlobalDatabaseService globalDatabaseService;
        private readonly object isDragOperationActiveLock = new object();
        private bool isDragOperationActive;

        [ImportingConstructor]
        public ManagerService(IShellService shellService, ICrawlerService crawlerService, IMessageService messageService, IGlobalDatabaseService globalDatabaseService)
        {
            BlogFiles = new ObservableCollection<IBlog>();
            Application.Current.Dispatcher.Invoke(new Action(() => BindingOperations.EnableCollectionSynchronization(BlogFiles, blogFilesLock)));
            blogFilesView = CollectionViewSource.GetDefaultView(BlogFiles);
            blogFilesView.Filter = BlogFilesViewSource_Filter;
            databases = new List<IFiles>();
            archiveDatabases = new List<IFiles>();
            this.shellService = shellService;
            this.messageService = messageService;
            this.globalDatabaseService = globalDatabaseService;
            crawlerService.ActiveCollectionIdChanged += CrawlerService_ActiveCollectionIdChanged;
        }

        private void CrawlerService_ActiveCollectionIdChanged(object sender, System.EventArgs e)
        {
            try
            {
                var ecv = (IEditableCollectionView)blogFilesView;
                if (ecv.IsAddingNew) ecv.CancelNew();
                blogFilesView.Refresh();
            }
            catch (Exception ex)
            {
                throw new Exception("Exception 2", ex);
            }
        }

        private bool BlogFilesViewSource_Filter(object obj)
        {
            if (shellService.Settings.ActiveCollectionId == 0) return true;

            IBlog item = obj as IBlog;
            return item.CollectionId == shellService.Settings.ActiveCollectionId;
        }

        public ObservableCollection<IBlog> BlogFiles { get; }

        public bool GlobalDbExisted
        {
            get
            {
                return globalDatabaseService.DbExisted;
            }
        }

        public bool IsDragOperationActive
        {
            get
            {
                lock (isDragOperationActiveLock)
                {
                    return isDragOperationActive;
                }
            }
            set
            {
                lock (isDragOperationActiveLock)
                {
                    isDragOperationActive = value;
                }
            }
        }

        public ICollectionView BlogFilesView => blogFilesView;

        public IFiles GetDatabase(string blogName, BlogTypes originalBlogType)
        {
            return databases.FirstOrDefault(file => file.Name.Equals(blogName) && file.BlogType.Equals(originalBlogType));
        }

        public void EnsureUniqueFolder(IBlog blog)
        {
            int number = 1;
            string appendix = "";
            while (BlogFiles.Any(b => b.DownloadLocation() == blog.DownloadLocation() + appendix) || Directory.Exists(blog.DownloadLocation() + appendix))
            {
                number++;
                appendix = $"_{number}";
            }
            if (number != 1)
            {
                blog.FileDownloadLocation = blog.DownloadLocation() + appendix;
            }
            Directory.CreateDirectory(blog.DownloadLocation());
        }

        private bool CheckIfFileExistsInDBInternal(string filename, bool checkOriginalLinkFirst, bool checkArchive)
        {
            databasesLock.EnterReadLock();
            try
            {
                foreach (IFiles db in databases)
                {
                    if (db.CheckIfFileExistsInDB(filename, checkOriginalLinkFirst)) return true;
                }
            }
            finally
            {
                databasesLock.ExitReadLock();
            }
            if (checkArchive)
            {
                archivesLock.EnterReadLock();
                try
                {
                    foreach (IFiles db in archiveDatabases)
                    {
                        if (db.CheckIfFileExistsInDB(filename, checkOriginalLinkFirst)) return true;
                    }
                }
                finally
                {
                    archivesLock.ExitReadLock();
                }
            }
            return false;
        }

        public bool CheckIfFileExistsInDB(string filename, bool checkOriginalLinkFirst, bool checkArchive)
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                return globalDatabaseService.CheckIfFileExistsAsync(filename, checkOriginalLinkFirst, checkArchive).GetAwaiter().GetResult();
            }
            else
            {
                return CheckIfFileExistsInDBInternal(filename, checkOriginalLinkFirst, checkArchive);
            }
        }

        private void RemoveDatabaseInternal(IFiles database)
        {
            databasesLock.EnterWriteLock();
            try
            {
                databases.Remove(database);
            }
            finally
            {
                databasesLock.ExitWriteLock();
            }
        }

        public void RemoveDatabase(string name, int blogType)
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.RemoveFileEntriesAsync(name, blogType, false);
            }
            else
            {
                var database = databases.FirstOrDefault(db => db.Name.Equals(name) && db.BlogType.Equals(blogType));
                RemoveDatabaseInternal(database);
            }
        }

        private void AddDatabaseInternal(IFiles database)
        {
            databasesLock.EnterWriteLock();
            try
            {
                databases.Add(database);
            }
            finally
            {
                databasesLock.ExitWriteLock();
            }
        }

        public async Task BeforeDatabaseAdding()
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                await globalDatabaseService.Init();
            }
        }

        public async Task AfterDatabaseAdding()
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                await globalDatabaseService.PostInit();
            }
        }

        public void AddDatabase(IFiles database)
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.AddFileEntriesAsync(database, false);
            }
            else
            {
                AddDatabaseInternal(database);
            }
        }

        private void ClearDatabasesInternal()
        {
            databasesLock.EnterWriteLock();
            try
            {
                databases.Clear();
            }
            finally
            {
                databasesLock.ExitWriteLock();
            }
        }

        public void ClearDatabases()
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.ClearFileEntries(false);
            }
            else
            {
                ClearDatabasesInternal();
            }
        }

        private void AddArchiveInternal(IFiles archiveDB)
        {
            archivesLock.EnterWriteLock();
            try
            {
                archiveDatabases.Add(archiveDB);
            }
            finally
            {
                archivesLock.ExitWriteLock();
            }
        }

        public void AddArchive(IFiles archiveDB)
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.AddFileEntriesAsync(archiveDB, true);
            }
            else
            {
                AddArchiveInternal(archiveDB);
            }
        }

        private void ClearArchivesInternal()
        {
            archivesLock.EnterWriteLock();
            try
            {
                archiveDatabases.Clear();
            }
            finally
            {
                archivesLock.ExitWriteLock();
            }
        }

        public void ClearArchives()
        {
            if (shellService.Settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.ClearFileEntries(true);
            }
            else
            {
                ClearArchivesInternal();
            }
        }

        public bool IsCollectionIdUsed(int id)
        {
            foreach (var blog in BlogFiles)
            {
                if (blog.CollectionId == id) return true;
            }
            return false;
        }

        public bool UpdateCollectionOnlineStatuses(bool askFirstTime = false)
        {
            try
            {
                bool? ok = null;
                foreach (var item in shellService.Settings.Collections)
                {
                    var stillOnline = Directory.Exists(item.DownloadLocation) || Directory.Exists(Directory.GetParent(item.DownloadLocation).FullName);
                    if (item.IsOnline.Value && !stillOnline)
                    {
                        if (askFirstTime && ok == null)
                            ok = messageService.ShowYesNoQuestion(string.Format(Resources.AskCachedCollectionOfflineContinueShutdownAnyway, item.Name));
                        if (ok == false) return false;
                        item.IsOnline = stillOnline;
                    }
                }
                return ok ?? true;
            }
            catch (Exception e)
            {
                Logger.Error("ManagerService.UpdateCollectionOnlineStates: {0}", e);
                messageService.ShowError(e.Message);
                return false;
            }
        }

        public void CacheLibraries()
        {
            try
            {
                // remember all cache folders
                var cachePath = Path.Combine(shellService.Settings.DownloadLocation, "Index", "Archive");
                if (!Directory.Exists(cachePath)) return;

                var folderList = new List<string>();
                foreach (var folder in Directory.EnumerateDirectories(cachePath, "[cache]*", SearchOption.TopDirectoryOnly))
                {
                    folderList.Add(folder.ToLower());
                }

                foreach (var collection in shellService.Settings.Collections)
                {
                    if (collection.Id == 0) continue;
                    var collIndexPath = Path.Combine(collection.DownloadLocation, "Index");
                    var currPath = Path.Combine(cachePath, "[cache]" + collection.Id.ToString());
                    folderList.Remove(currPath.ToLower());
                    if (!collection.IsOnline.Value) continue;

                    if (collection.OfflineDuplicateCheck)
                    {
                        if (!Directory.Exists(currPath)) { Directory.CreateDirectory(currPath); }
                        EmptyDirectory(currPath);
                        CopyAll(collIndexPath, currPath);
                    }
                    else
                    {
                        if (Directory.Exists(currPath))
                        {
                            Directory.Delete(currPath, true);
                        }
                    }
                }

                // remove the rest of the old cache folders
                foreach (var folder in folderList)
                {
                    Directory.Delete(folder, true);
                }
            }
            catch (Exception e)
            {
                Logger.Error("ManagerService.CacheLibraries: {0}", e);
                messageService.ShowError(e.Message);
            }
        }

        private static void EmptyDirectory(string folder)
        {
            DirectoryInfo di = new DirectoryInfo(folder);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        public static void CopyAll(string sourcePath, string targetPath)
        {
            var source = new DirectoryInfo(sourcePath);
            var target = new DirectoryInfo(targetPath);

            if (string.Equals(source.FullName, target.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Check if the target directory exists, if not, create it.
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into it's new directory.
            foreach (FileInfo fi in source.EnumerateFiles())
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.EnumerateDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir.FullName, nextTargetSubDir.FullName);
            }
        }
    }
}
