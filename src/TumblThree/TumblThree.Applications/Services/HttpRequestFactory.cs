using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TumblThree.Applications.Properties;
using TumblThree.Domain;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IHttpRequestFactory))]

    public class HttpRequestFactory : IHttpRequestFactory
    {
        public WinHttpHandler HttpHandler { get; set; }
        public HttpClient HttpClient { get; set; }

        private ISharedCookieService cookieService;
        //private CookieContainer cookieContainer = new CookieContainer();
        private readonly AppSettings settings;

        [ImportingConstructor]
        public HttpRequestFactory(IShellService shellService, ISharedCookieService cookieService, AppSettings settings)
        {
            //this.shellService = shellService;
            this.cookieService = cookieService;
            this.settings = settings;
            ServicePointManager.DefaultConnectionLimit = 400;

            initHttpHandler();
            initHttpClient(HttpHandler);
        }
        public void initHttpHandler()
        {
            HttpHandler = new WinHttpHandler();
            HttpHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; // def: None;
            HttpHandler.AutomaticRedirection = true;
            //HttpHandler.CookieUsePolicy = CookieUsePolicy.UseInternalCookieStoreOnly;
            HttpHandler.CookieUsePolicy = CookieUsePolicy.UseSpecifiedCookieContainer;
            HttpHandler.CookieContainer = cookieService.CookieContainer;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            if (!string.IsNullOrEmpty(settings.ProxyHost) && !string.IsNullOrEmpty(settings.ProxyPort)) // TODO: new options
            {
                IWebProxy proxy = new WebProxy(settings.ProxyHost, int.Parse(settings.ProxyPort));
                if (!string.IsNullOrEmpty(settings.ProxyUsername) && !string.IsNullOrEmpty(settings.ProxyPassword))
                    proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword);
                HttpHandler.Proxy = proxy;
                HttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;
            }
            else
            {
                //IWebProxy proxy = new WebProxy();
                //proxy = new WebProxy("127.0.0.1", 10809);
                //HttpHandler.Proxy = proxy;
                //HttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseCustomProxy;

                HttpHandler.WindowsProxyUsePolicy = WindowsProxyUsePolicy.UseWinInetProxy;
            }
        }
        public void initHttpClient(WinHttpHandler handler = null) // due to only once allowed
        {
            HttpClient = new HttpClient(handler ?? HttpHandler);

            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
            HttpClient.Timeout = TimeSpan.FromSeconds(settings.TimeOut * 1000);
            HttpClient.BaseAddress = new Uri("https://www.tumblr.com/");
        }

        public WinHttpHandler TakeHttpHandler => HttpHandler;
        public HttpClient TakeHttpClient => this.HttpClient;
        public CookieContainer CookieContainer => this.TakeHttpHandler.CookieContainer;

        private HttpRequestMessage NewStubRequest(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            var message = new HttpRequestMessage()
            {
                RequestUri = new Uri(url)
                , Version = new Version("2.0") // see also https://github.com/dotnet/runtime/issues/15877
            };
            if(!string.IsNullOrEmpty(referer))
                message.Headers.Referrer = new Uri(referer);
            //message.Headers.Add("User-Agent", settings.UserAgent);

            headers = headers ?? new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> header in headers)
            {
                message.Headers.Add(header.Key, header.Value);
            }

            return message;
        }

        public async Task<HttpRequestMessage> GetReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            var request = NewStubRequest(url, referer, headers);
            request.Method = HttpMethod.Get;
            return request;
        }

        public async Task<HttpResponseMessage> GetReqeust(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            var request = NewStubRequest(url, referer, headers);
            request.Method = HttpMethod.Get;
            return await HttpClient.SendAsync(request); // TODO: try catch
        }

        public HttpRequestMessage GetXhrReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            var request = NewStubRequest(url, referer, headers);
            request.Method = HttpMethod.Get;
            request.Content = new FormUrlEncodedContent(headers);
            //request.Content = new StringContent(JsonConvert.SerializeObject(headers), System.Text.Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            return request;
        }

        public HttpRequestMessage PostReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            var request = NewStubRequest(url, referer, headers);
            request.Method = HttpMethod.Post;
            return request;
        }

        public HttpRequestMessage PostXhrReqeustMessage(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            var request = PostReqeustMessage(url, referer, headers);
            request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            return request;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage requestMessage)//, HttpClient httpClient)
        {
            // TODO: try catch
            return HttpClient.SendAsync(requestMessage);
        }

        public async Task<HttpResponseMessage> PostReqeustAsync(HttpRequestMessage request, Dictionary<string, string> parameters) //
        {
            request.Content = new FormUrlEncodedContent(parameters);
            return await TakeHttpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostXHRReqeustAsync(HttpRequestMessage request, string requestBody)
        {
            //request.Content = new StringContent(requestBody);
            return await TakeHttpClient.SendAsync(request);
        }

        public async Task<bool> RemotePageIsValidAsync(string url)
        {
            var httpHandler = TakeHttpHandler;
            httpHandler.AutomaticRedirection = false;
            var httpClient = TakeHttpClient;

            var request = NewStubRequest(url);
            request.Method = HttpMethod.Head;
            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }

        public Stream GetStreamForApiRequest(Stream stream)
        {
            return !settings.LimitScanBandwidth || settings.Bandwidth == 0
                ? stream
                : new ThrottledStream(stream, (settings.Bandwidth / settings.ConcurrentConnections) * 1024);
        }

        public string UrlEncode(IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> val in parameters)
            {
                sb.AppendFormat("{0}={1}&", val.Key, HttpUtility.UrlEncode(val.Value));
            }

            sb.Remove(sb.Length - 1, 1); // remove last '&'
            return sb.ToString();
        }
    }
}
