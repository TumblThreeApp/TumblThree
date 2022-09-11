using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface IWebRequestFactory
    {
        HttpWebRequest CreateGetRequest(string url, string referer = "", Dictionary<string, string> headers = null, bool allowAutoRedirect = true);

        HttpWebRequest CreateGetXhrRequest(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpWebRequest CreatePostRequest(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpWebRequest CreatePostXhrRequest(string url, string referer = "", Dictionary<string, string> headers = null);

        Task PerformPostRequestAsync(HttpWebRequest request, Dictionary<string, string> parameters);

        Task PerformPostXHRRequestAsync(HttpWebRequest request, string requestBody, bool useUTF8);

        Task<bool> RemotePageIsValidAsync(string url);

        Task<string> ReadRequestToEndAsync(HttpWebRequest request, bool storeCookies = false);

        Task<ResponseDetails> ReadRequestToEnd2Async(HttpWebRequest request, string cookieDomain);

        Stream GetStreamForApiRequest(Stream stream);

        string UrlEncode(IDictionary<string, string> parameters);
    }
}
