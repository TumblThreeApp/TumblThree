using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Waf.Foundation;
using System.Windows;
using System.Windows.Data;
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

        [ImportingConstructor]
        public ManagerService(IShellService shellService, ICrawlerService crawlerService)
        {
            BlogFiles = new ObservableCollection<IBlog>();
            Application.Current.Dispatcher.Invoke(new Action(() => BindingOperations.EnableCollectionSynchronization(BlogFiles, blogFilesLock)));
            blogFilesView = CollectionViewSource.GetDefaultView(BlogFiles);
            blogFilesView.Filter = BlogFilesViewSource_Filter;
            databases = new List<IFiles>();
            archivedLinks = new HashSet<string>();
            this.shellService = shellService;
            crawlerService.ActiveCollectionIdChanged += CrawlerService_ActiveCollectionIdChanged;
        }

        private void CrawlerService_ActiveCollectionIdChanged(object sender, System.EventArgs e)
        {
            QueueOnDispatcher.CheckBeginInvokeOnUI(() => blogFilesView.Refresh());
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
    }
}
