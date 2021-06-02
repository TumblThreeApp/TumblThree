using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Waf.Foundation;

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

        [ImportingConstructor]
        public ManagerService()
        {
            BlogFiles = new ObservableCollection<IBlog>();
            databases = new List<IFiles>();
            archivedLinks = new HashSet<string>();
        }

        public ObservableCollection<IBlog> BlogFiles { get; }

        public IEnumerable<IFiles> Databases => databases;

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
    }
}
