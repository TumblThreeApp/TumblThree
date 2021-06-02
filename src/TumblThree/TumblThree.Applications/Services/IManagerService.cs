using System.Collections.Generic;
using System.Collections.ObjectModel;

using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    public interface IManagerService
    {
        ObservableCollection<IBlog> BlogFiles { get; }

        IEnumerable<IFiles> Databases { get; }

        bool CheckIfFileExistsInDB(string filename, bool checkArchive);

        void RemoveDatabase(IFiles database);

        void AddDatabase(IFiles database);

        void ClearDatabases();

        void AddArchive(IFiles archiveDB);

        void ClearArchive();
    }
}
