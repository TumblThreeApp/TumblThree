using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Xml;
using System.Xml.Linq;
using TumblThree.Domain;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IApplicationUpdateService))]
    public class ApplicationUpdateService : IApplicationUpdateService
    {
        private readonly IShellService shellService;
        private readonly IWebRequestFactory webRequestFactory;
        private string downloadLink;
        private string version;

        [ImportingConstructor]
        public ApplicationUpdateService(IShellService shellService, IWebRequestFactory webRequestFactory)
        {
            this.shellService = shellService;
            this.webRequestFactory = webRequestFactory;
        }

        public async Task<string> GetLatestReleaseFromServer()
        {
            version = null;
            downloadLink = null;
            try
            {
                HttpWebRequest request = webRequestFactory.CreateGetRequest("https://api.github.com/repos/tumblthreeapp/tumblthree/releases/latest");
                string result = await webRequestFactory.ReadRequestToEndAsync(request);
                XmlDictionaryReader jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(result), new XmlDictionaryReaderQuotas());
                XElement root = XElement.Load(jsonReader);
                version = root.Element("tag_name").Value;
                if (version.StartsWith("v", StringComparison.InvariantCultureIgnoreCase)) version = version.Substring(1);

                if (Environment.Is64BitProcess)
                {
                    downloadLink = root.Descendants("browser_download_url").Where(s => s.Value.Contains("x64-App")).FirstOrDefault()?.Value;
                }
                else
                {
                    downloadLink = root.Descendants("browser_download_url").Where(s => s.Value.Contains("x86-App")).FirstOrDefault()?.Value;
                }

                if (string.IsNullOrEmpty(downloadLink)) downloadLink = root.Element("assets").Element("item").Element("browser_download_url").Value;

            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
                return exception.Message;
            }

            return null;
        }

        public bool IsNewVersionAvailable()
        {
            try
            {
                var newVersion = new Version(version);

                if (newVersion > new Version(ApplicationInfo.Version))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
            }

            return false;
        }

        public string GetNewAvailableVersion()
        {
            return version;
        }

        public Uri GetDownloadUri()
        {
            if (downloadLink == null)
            {
                return null;
            }

            return new Uri(downloadLink);
        }
    }
}
