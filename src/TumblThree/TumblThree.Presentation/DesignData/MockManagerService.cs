using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using TumblThree.Applications.Services;
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

        public void SetBlogFiles(IEnumerable<IBlog> blogFilesToAdd)
        {
            innerBlogFiles.Clear();
            blogFilesToAdd.ToList().ForEach(x => innerBlogFiles.Add(x));
        }

        public void EnsureUniqueFolder(IBlog blog) => throw new NotImplementedException();

        public bool CheckIfFileExistsInDB(string filename, bool checkArchive) => false;

        public void AddDatabase(IFiles database) => throw new NotImplementedException();

        public void RemoveDatabase(IFiles database) => throw new NotImplementedException();

        public void ClearDatabases() => throw new NotImplementedException();

        public void AddArchive(IFiles archiveDB) => throw new NotImplementedException();

        public void ClearArchive() => throw new NotImplementedException();

        public bool IsCollectionIdUsed(int id) => throw new NotImplementedException();

        public void CacheLibraries() => throw new NotImplementedException();
    }
}
