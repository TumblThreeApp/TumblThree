using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using TumblThree.Applications.Extensions;
using TumblThree.Applications.Properties;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IWebRequestFactory))]
    public class WebRequestFactory : IWebRequestFactory
    {
        private readonly IShellService shellService;
        private readonly ISharedCookieService cookieService;
        private readonly AppSettings settings;

        [ImportingConstructor]
        public WebRequestFactory(IShellService shellService, ISharedCookieService cookieService, AppSettings settings)
        {
            this.shellService = shellService;
            this.cookieService = cookieService;
            this.settings = settings;
        }

        private HttpWebRequest CreateStubRequest(string url, string referer = "", Dictionary<string, string> headers = null, bool allowAutoRedirect = true)
        {
            var request = (HttpWebRequest)WebRequest.Create(HttpUtility.UrlDecode(url));
            request.ProtocolVersion = HttpVersion.Version11;
            request.UserAgent = settings.UserAgent;
            request.AllowAutoRedirect = allowAutoRedirect;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            //request.KeepAlive = true;
            //request.Pipelined = true;

            // Timeouts don't work with GetResponseAsync() as it internally uses BeginGetResponse.
            // See docs: https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.timeout(v=vs.110).aspx
            // Quote: The Timeout property has no effect on asynchronous requests made with the BeginGetResponse or BeginGetRequestStream method.
            // TODO: Use HttpClient instead?

            request.ReadWriteTimeout = settings.TimeOut * 1000;
            request.Timeout = settings.TimeOut * 1000;
            request.CookieContainer = new CookieContainer
            {
                PerDomainCapacity = 100
            };
            ServicePointManager.DefaultConnectionLimit = 400;
            request = SetWebRequestProxy(request, settings);
            request.Referer = referer;
            headers = headers ?? new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers[header.Key] = header.Value;
            }

            return request;
        }

        public HttpWebRequest CreateGetRequest(string url, string referer = "", Dictionary<string, string> headers = null, bool allowAutoRedirect = true)
        {
            HttpWebRequest request = CreateStubRequest(url, referer, headers, allowAutoRedirect);
            request.Method = "GET";
            return request;
        }

        public HttpWebRequest CreateGetXhrRequest(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            HttpWebRequest request = CreateStubRequest(url, referer, headers);
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Headers["X-Requested-With"] = "XMLHttpRequest";
            return request;
        }

        public HttpWebRequest CreatePostRequest(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            HttpWebRequest request = CreateStubRequest(url, referer, headers);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            return request;
        }

        public HttpWebRequest CreatePostXhrRequest(string url, string referer = "", Dictionary<string, string> headers = null)
        {
            HttpWebRequest request = CreatePostRequest(url, referer, headers);
            request.Accept = "application/json, text/javascript, */*; q=0.01";
            request.Headers["X-Requested-With"] = "XMLHttpRequest";
            return request;
        }

        public async Task PerformPostRequestAsync(HttpWebRequest request, Dictionary<string, string> parameters)
        {
            string requestBody = UrlEncode(parameters);
            using (Stream postStream = await request.GetRequestStreamAsync().TimeoutAfter(shellService.Settings.TimeOut))
            {
                byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                await postStream.FlushAsync();
            }
        }

        public async Task PerformPostXHRRequestAsync(HttpWebRequest request, string requestBody)
        {
            using (Stream postStream = await request.GetRequestStreamAsync())
            {
                byte[] postBytes = Encoding.ASCII.GetBytes(requestBody);
                await postStream.WriteAsync(postBytes, 0, postBytes.Length);
                await postStream.FlushAsync();
            }
        }

        public async Task<bool> RemotePageIsValidAsync(string url)
        {
            HttpWebRequest request = CreateStubRequest(url);
            request.Method = "HEAD";
            request.AllowAutoRedirect = false;
            var response = await request.GetResponseAsync() as HttpWebResponse;
            response.Close();
            return (response.StatusCode == HttpStatusCode.OK);
        }

        public async Task<string> ReadRequestToEndAsync(HttpWebRequest request)
        {
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut) as HttpWebResponse)
            {
                using (Stream stream = GetStreamForApiRequest(response.GetResponseStream()))
                {
                    using (var buffer = new BufferedStream(stream))
                    {
                        using (var reader = new StreamReader(buffer))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        public async Task<ResponseDetails> ReadRequestToEnd2Async(HttpWebRequest request)
        {
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut) as HttpWebResponse)
            {
                if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.Moved)
                {
                    response.Close();
                    if (response.Headers.AllKeys.Contains("Set-Cookie"))
                        cookieService.SetUriCookie(CookieParser.GetAllCookiesFromHeader(response.Headers["Set-Cookie"], "www.tumblr.com"));

                    return new ResponseDetails() { HttpStatusCode = response.StatusCode, RedirectUrl = response.Headers["Location"] };
                }
                using (Stream stream = GetStreamForApiRequest(response.GetResponseStream()))
                {
                    using (var buffer = new BufferedStream(stream))
                    {
                        using (var reader = new StreamReader(buffer))
                        {
                            string content = reader.ReadToEnd();
                            return new ResponseDetails() { HttpStatusCode = response.StatusCode, Response = content };
                        }
                    }
                }
            }
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

        private static HttpWebRequest SetWebRequestProxy(HttpWebRequest request, AppSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.ProxyHost) && !string.IsNullOrEmpty(settings.ProxyPort))
            {
                request.Proxy = new WebProxy(settings.ProxyHost, int.Parse(settings.ProxyPort));
            }

            if (!string.IsNullOrEmpty(settings.ProxyUsername) && !string.IsNullOrEmpty(settings.ProxyPassword))
            {
                request.Proxy.Credentials = new NetworkCredential(settings.ProxyUsername, settings.ProxyPassword);
            }

            return request;
        }
    }
}
