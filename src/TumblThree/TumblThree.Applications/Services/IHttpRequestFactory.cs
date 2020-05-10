using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface IHttpRequestFactory
    {
        WinHttpHandler TakeHttpHandler { get; }
        HttpClient TakeHttpClient { get; }
        void initHttpHandler();
        void initHttpClient(WinHttpHandler HttpHandler);
        CookieContainer CookieContainer { get; }

        Task<HttpRequestMessage> GetReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null);
        Task<HttpResponseMessage> GetReqeust(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpRequestMessage GetXhrReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpRequestMessage PostReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null);

        HttpRequestMessage PostXhrReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null);
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage);

        Task<HttpResponseMessage> PostReqeustAsync(HttpRequestMessage request, Dictionary<string, string> parameters);

        Task<HttpResponseMessage> PostXHRReqeustAsync(HttpRequestMessage request, string requestBody);

        Task<bool> RemotePageIsValidAsync(string url);


        Stream GetStreamForApiRequest(Stream stream);

        string UrlEncode(IDictionary<string, string> parameters);
    }
}
