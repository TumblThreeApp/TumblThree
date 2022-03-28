using System;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface ILogService
    {
        string TumblThreeVersionString { get; }

        string WindowsVersionString { get; }

        string DefaultBrowserString { get; }

        string RegionSettingsString { get; }

        string NetFrameworkVersionString { get; }

        string NetVersionSupportString { get; }

        Task SendErrorDetails(Exception ex, bool terminating);

        Task SendLogData();
    }
}
