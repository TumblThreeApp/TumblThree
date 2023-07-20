using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for SettingsView.xaml.
    /// </summary>
    [Export(typeof(IAuthenticateView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuthenticateView : IAuthenticateView, IDisposable
    {
        private readonly Lazy<AuthenticateViewModel> viewModel;
        private readonly string _appSettingsPath;
        private string _url;
        private string _domain;
        private static IntPtr HWND_MESSAGE = new IntPtr(-3);
        private readonly CountdownEvent _pageLoad = new CountdownEvent(1);

        [ImportingConstructor]
        public AuthenticateView(IEnvironmentService environmentService)
        {
            InitializeComponent();
            _appSettingsPath = Path.GetFullPath(Path.Combine(environmentService.AppSettingsPath, ".."));
            viewModel = new Lazy<AuthenticateViewModel>(() => ViewHelper.GetViewModel<AuthenticateViewModel>(this));
            Configure();
            InitializeAsync();
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            switch (e.ProcessFailedKind)
            {
                case CoreWebView2ProcessFailedKind.BrowserProcessExited:
                case CoreWebView2ProcessFailedKind.RenderProcessUnresponsive:
                    RecreateWebView2();
                    break;
                case CoreWebView2ProcessFailedKind.RenderProcessExited:
                case CoreWebView2ProcessFailedKind.FrameRenderProcessExited:
                    browser.Reload();
                    break;
            }
        }

        private void Configure()
        {
            browser.CoreWebView2InitializationCompleted += (_, e) =>
            {
                _ = Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (e.IsSuccess && browser.CoreWebView2 != null)
                    {
                        browser.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
                    }
                    if (_url != null)
                    {
                        browser.Source = new Uri(_url);
                    }
                }));
            };
        }

        private void RecreateWebView2()
        {
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    webViewHost.Children.Clear();
                }
                catch { }

                try
                {
                    browser.Dispose();
                }
                catch { }

                browser = new WebView2();
                _ = webViewHost.Children.Add(browser);

                Configure();
                InitializeAsync();
            }));
        }

        private async void InitializeAsync()
        {
            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, _appSettingsPath);
            await browser.EnsureCoreWebView2Async(env);
        }

        private AuthenticateViewModel ViewModel => viewModel.Value;

        public void ShowDialog(object owner, string url, string cookieDomain)
        {
            Owner = owner as Window;
            _url = url;
            _domain = cookieDomain;
            ShowDialog();
        }

        public string GetUrl()
        {
            return browser.Source.ToString();
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _pageLoad.Signal();
            Thread.Sleep(1);
            _pageLoad.Reset();
        }

        private string WaitForPageLoad()
        {
            return _pageLoad.Wait(20000) ? "Success" : "Timeout";
        }

        public async Task<string> GetDocument()
        {
            CoreWebView2 webview = await GetWebviewAsync();
            webview.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            webview.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webview.Navigate("https://twitter.com/settings");
            await Task.Run(() => { WaitForPageLoad(); });
            string document = await webview.ExecuteScriptAsync("document.documentElement.outerHTML");
            document = System.Text.RegularExpressions.Regex.Unescape(document.Substring(1, document.Length - 2));
            return document;
        }

        public async Task<CookieCollection> GetCookies(string url)
        {
            var cookieManager = browser.CoreWebView2.CookieManager;
            var cookies = await cookieManager.GetCookiesAsync(url);

            CookieCollection cookieCollection;
            if (url.Contains("tumblr.com"))
            {
                // don't ask why, but one cookieCollection works and the other not
                var cookieHeader = GetCookieHeader(cookies);
                CookieContainer cookieCon = new CookieContainer();
                cookieCon.SetCookies(new Uri("https://" + _domain + "/"), cookieHeader);
                cookieCollection = FixCookieDates(cookieCon.GetCookies(new Uri("https://" + _domain + "/")));
            }
            else
            {
                cookieCollection = AuthenticateView.GetCookies(cookies);
            }
      
            return cookieCollection;
        }

        public async Task DeleteCookies(string url)
        {
            CoreWebView2 webview = await GetWebviewAsync();
            var cookieManager = webview.CookieManager;
            var cookies = await cookieManager.GetCookiesAsync(url);
            foreach (var cookie in cookies)
            {
                cookieManager.DeleteCookie(cookie);
            }
        }

        private async Task<CoreWebView2> GetWebviewAsync()
        {
            CoreWebView2 webview = browser.CoreWebView2;
            if (webview is null)
            {
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, _appSettingsPath);
                var browserController = await env.CreateCoreWebView2ControllerAsync(HWND_MESSAGE);
                webview = browserController.CoreWebView2;
            }
            return webview;
        }

        private static CookieCollection GetCookies(List<CoreWebView2Cookie> cookies)
        {
            CookieCollection cookieCollection = new CookieCollection();
            foreach (var cookie in cookies)
            {
                var transferCookie = new System.Net.Cookie(cookie.Name, WebUtility.UrlEncode(cookie.Value), cookie.Path, cookie.Domain);
                transferCookie.Expires = cookie.Expires;
                transferCookie.HttpOnly = cookie.IsHttpOnly;
                transferCookie.Secure = cookie.IsSecure;
                cookieCollection.Add(transferCookie);
            }
            return cookieCollection;
        }

        private static string GetCookieHeader(List<CoreWebView2Cookie> cookies)
        {
            StringBuilder cookieString = new StringBuilder();
            string delimiter = string.Empty;

            foreach (var cookie in cookies)
            {
                cookieString.Append(delimiter)
                    .Append(cookie.Name)
                    .Append('=')
                    .Append(WebUtility.UrlEncode(cookie.Value));
                delimiter = ",";
            }

            return cookieString.ToString();
        }

        private static CookieCollection FixCookieDates(CookieCollection cookieCol)
        {
            foreach (System.Net.Cookie cookie in cookieCol)
            {
                if (cookie.Expires.Equals(DateTime.MinValue) && cookie.Expires.Kind == DateTimeKind.Unspecified)
                    cookie.Expires = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            }
            return cookieCol;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pageLoad.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
