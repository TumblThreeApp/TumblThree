using System.Collections.ObjectModel;
using System.ComponentModel;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    public interface IManagerService
    {
        ObservableCollection<IBlog> BlogFiles { get; }

        IFiles GetDatabase(string blogName, BlogTypes originalBlogType);

        void EnsureUniqueFolder(IBlog blog);

        bool CheckIfFileExistsInDB(string filename, bool checkOriginalLinkFirst, bool checkArchive);

        void RemoveDatabase(string name, int blogType);

        void AddDatabase(IFiles database);

        void ClearDatabases();

        void AddArchive(IFiles archiveDB);

        void ClearArchives();

        ICollectionView BlogFilesView { get; }

        bool IsCollectionIdUsed(int id);

        bool UpdateCollectionOnlineStatuses(bool askFirstTime = false);

        void CacheLibraries();

        bool IsDragOperationActive { get; set; }
    }
}
