using System;
using System.Net;
using System.Threading;

namespace WpfImageViewer
{
    internal class MediaFileServer
    {
        private readonly HttpListener _httpListener = new HttpListener();
        private readonly Func<HttpListenerRequest, byte[]> _requestHandlerMethod;
        private string _prefix;

        public string Prefix { get => _prefix; }

        public MediaFileServer(string prefix, Func<HttpListenerRequest, byte[]> requestHandlerMethod)
        {
            _ = prefix ?? throw new ArgumentNullException(nameof(prefix));
            _ = requestHandlerMethod ?? throw new ArgumentNullException(nameof(requestHandlerMethod));

            _prefix = prefix;
            _requestHandlerMethod = requestHandlerMethod;
            _httpListener.Prefixes.Add(prefix);
            _httpListener.Start();
        }

        public void Run()
        {
            _ = ThreadPool.QueueUserWorkItem((o) =>
              {
                  try
                  {
                      while (_httpListener.IsListening)
                      {
                          _ = ThreadPool.QueueUserWorkItem((c) =>
                            {
                                var ctx = c as HttpListenerContext;
                                try
                                {
                                    byte[] buffer = _requestHandlerMethod(ctx.Request);
                                    ctx.Response.ContentLength64 = buffer.Length;
                                    ctx.Response.ContentType = "application/octet-stream";
                                    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                                }
                                catch { }
                                finally
                                {
                                    ctx.Response.OutputStream.Close();
                                }
                            }, _httpListener.GetContext());
                      }
                  }
                  catch { }
              });
        }

        public void Stop()
        {
            _httpListener.Stop();
            _httpListener.Close();
        }
    }
}
