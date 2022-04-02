using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface IApplicationUpdateService
    {
        Task<string> GetLatestReleaseFromServer(bool x64Only = false);

        bool IsNewVersionAvailable();

        string GetNewAvailableVersion();

        Uri GetDownloadUri();

        Task<bool> SendFeedback(string name, string email, string message);
    }
}
