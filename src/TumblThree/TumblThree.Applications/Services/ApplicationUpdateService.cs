using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Web;
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

        public async Task<string> GetLatestReleaseFromServer(bool x64Only = false)
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

                if (x64Only || Environment.Is64BitProcess)
                {
                    downloadLink = root.Descendants("browser_download_url").Where(s => s.Value.Contains("x64") && !s.Value.Contains("x64-Tra")).FirstOrDefault()?.Value;
                }
                else
                {
                    downloadLink = root.Descendants("browser_download_url").Where(s => s.Value.Contains("x86") && !s.Value.Contains("x86-Tra")).FirstOrDefault()?.Value;
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

        public async Task<bool> SendFeedback(string name, string email, string message)
        {
            try
            {
                HttpWebRequest request = webRequestFactory.CreatePostRequest("https://9332a1f6dcab0d2f3fdafd51eaed07ca.m.pipedream.net");
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                var p = new Dictionary<string, string>();
                p.Add("name", name);
                p.Add("email", email);
                p.Add("title", "App Feedback");
                p.Add("message", message);
                p.Add("url", "");
                var fields = string.Join("&", p.Select(kvp => string.Format("{0}={1}", kvp.Key, HttpUtility.UrlEncode(kvp.Value))));
                var version = ApplicationInfo.Version;
                p = new Dictionary<string, string>() { { "form", fields }, { "other", version } };
                await webRequestFactory.PerformPostRequestAsync(request, p);
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new ApplicationException(string.Format("endpoint returned: {0} {1}", response.StatusCode, response.StatusDescription));
                }
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error(exception.ToString());
                throw;
            }
        }
    }
}
