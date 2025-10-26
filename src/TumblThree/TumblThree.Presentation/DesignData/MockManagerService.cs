using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Presentation.DesignData
{
    public class MockManagerService : IManagerService
    {
        private readonly ObservableCollection<IBlog> blogFiles;
        private readonly ObservableCollection<IBlog> innerBlogFiles;

        public MockManagerService()
        {
            innerBlogFiles = new ObservableCollection<IBlog>();
            blogFiles = new ObservableCollection<IBlog>(innerBlogFiles);
        }

        public ObservableCollection<IBlog> BlogFiles => blogFiles;

        public ICollectionView BlogFilesView { get; }

        public IEnumerable<IFiles> Databases { get; }

        public bool IsDragOperationActive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool GlobalDbExisted => throw new NotImplementedException();

        public void SetBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            innerBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => innerBlogFiles.Add(x));
        }

        public void EnsureUniqueFolder(IBlog blog) => throw new NotImplementedException();

        public bool CheckIfFileExistsInDB(string filename, bool checkOriginalLink, bool checkArchive) => false;

        public void AddDatabase(IFiles database) => throw new NotImplementedException();

        public void RemoveDatabase(string name, int blogType) => throw new NotImplementedException();

        public void ClearDatabases() => throw new NotImplementedException();

        public void AddArchive(IFiles archiveDB) => throw new NotImplementedException();

        public void ClearArchives() => throw new NotImplementedException();

        public bool IsCollectionIdUsed(int id) => throw new NotImplementedException();

        public void CacheLibraries() => throw new NotImplementedException();

        bool IManagerService.UpdateCollectionOnlineStatuses(bool askFirstTime = false) => throw new NotImplementedException();

        public IFiles GetDatabase(string blogName, BlogTypes originalBlogType) => throw new NotImplementedException();

        public Task BeforeDatabaseAdding() => throw new NotImplementedException();

        public Task AfterDatabaseAdding() => throw new NotImplementedException();
    }
}
