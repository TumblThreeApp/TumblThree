using System.Threading.Tasks;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    internal interface IGlobalDatabaseService
    {
        Task PrepareGlobalDatabaseAsync();
        Task AddFileToDb(string blogName, BlogTypes blogType, string fileNameUrl, string fileNameOriginalUrl, string fileName);
        Task<string> AddFileToDb(string blogName, BlogTypes blogType, string fileNameUrl, string fileNameOriginalUrl, string fileName, string appendTemplate);
        Task AddFileEntriesAsync(IFiles files, bool isArchive);
        Task<bool> CheckIfFileExistsAsync(string filenameUrl, bool checkOriginalLinkFirst, bool checkArchive);
        Task RemoveFileEntriesAsync(string name, int blogType, bool isArchive);
        Task ClearFileEntries(bool isArchive);
        Task UpdateOriginalLink(string blogName, int blogType, string filenameUrl, string filenameOriginalUrl);
    }
}
