using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Waf.Applications.Services;
using System.Waf.Foundation;
using System.Windows;
using System.Windows.Data;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    [Export]
    [Export(typeof(IManagerService))]
    internal class ManagerService : Model, IManagerService
    {
        private readonly IList<IFiles> databases;
        private readonly object databasesLock = new object();
        private readonly ISet<string> archivedLinks;
        private readonly object archiveLock = new object();
        private readonly ICollectionView blogFilesView;
        private static object blogFilesLock = new object();
        private readonly IShellService shellService;
        private readonly IMessageService messageService;

        [ImportingConstructor]
        public ManagerService(IShellService shellService, ICrawlerService crawlerService, IMessageService messageService)
        {
            BlogFiles = new ObservableCollection<IBlog>();
            Application.Current.Dispatcher.Invoke(new Action(() => BindingOperations.EnableCollectionSynchronization(BlogFiles, blogFilesLock)));
            blogFilesView = CollectionViewSource.GetDefaultView(BlogFiles);
            blogFilesView.Filter = BlogFilesViewSource_Filter;
            databases = new List<IFiles>();
            archivedLinks = new HashSet<string>();
            this.shellService = shellService;
            this.messageService = messageService;
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

        public ICollectionView BlogFilesView => blogFilesView;

        public IEnumerable<IFiles> Databases => databases;

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

        public bool CheckIfFileExistsInDB(string filename, bool checkArchive)
        {
            lock (databasesLock)
            {
                foreach (IFiles db in databases)
                {
                    if (db.CheckIfFileExistsInDB(filename)) return true;
                }
            }
            if (checkArchive)
            {
                lock (archiveLock)
                {
                    if (archivedLinks.Contains(filename)) return true;
                }
            }
            return false;
        }

        public void RemoveDatabase(IFiles database)
        {
            lock (databasesLock)
            {
                databases.Remove(database);
            }
        }

        public void AddDatabase(IFiles database)
        {
            lock (databasesLock)
            {
                databases.Add(database);
            }
        }

        public void ClearDatabases()
        {
            lock (databasesLock)
            {
                databases.Clear();
            }
        }

        public void AddArchive(IFiles archiveDB)
        {
            lock (archiveLock)
            {
                foreach (var entry in archiveDB.Entries)
                {
                    archivedLinks.Add(entry.Link);
                }
            }
        }

        public void ClearArchive()
        {
            lock (archiveLock)
            {
                archivedLinks.Clear();
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
            foreach (FileInfo fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.ToString(), fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir.FullName, nextTargetSubDir.FullName);
            }
        }
    }
}
