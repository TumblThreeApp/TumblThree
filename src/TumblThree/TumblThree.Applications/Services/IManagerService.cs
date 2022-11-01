using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    public interface IManagerService
    {
        ObservableCollection<IBlog> BlogFiles { get; }

        IEnumerable<IFiles> Databases { get; }

        void EnsureUniqueFolder(IBlog blog);

        bool CheckIfFileExistsInDB(string filename, bool checkOriginalLinkFirst, bool checkArchive);

        void RemoveDatabase(IFiles database);

        void AddDatabase(IFiles database);

        void ClearDatabases();

        void AddArchive(IFiles archiveDB);

        void ClearArchive();

        ICollectionView BlogFilesView { get; }

        bool IsCollectionIdUsed(int id);

        void CacheLibraries();

        bool IsDragOperationActive { get; set; }
    }
}
