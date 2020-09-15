using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using CefSharp;
using CefSharp.Wpf;
using TumblThree.Presentation.AjaxInterception;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for SettingsView.xaml.
    /// </summary>
    [Export(typeof(IAuthenticateView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuthenticateView : IAuthenticateView
    {
        private class CookieCollector : ICookieVisitor
        {
            private readonly TaskCompletionSource<List<CefSharp.Cookie>> _source = new TaskCompletionSource<List<CefSharp.Cookie>>();

            public bool Visit(CefSharp.Cookie cookie, int count, int total, ref bool deleteCookie)
            {
                _cookies.Add(cookie);

                if (count == (total - 1))
                {
                    _source.SetResult(_cookies);
                    return false;
                }
                return true;
            }

            // https://github.com/amaitland/CefSharp.MinimalExample/blob/ce6e579ad77dc92be94c0129b4a101f85e2fd75b/CefSharp.MinimalExample.WinForms/ListCookieVisitor.cs
            // CefSharp.MinimalExample.WinForms ListCookieVisitor 

            public Task<List<CefSharp.Cookie>> Task => _source.Task;

            public static string GetCookieHeader(List<CefSharp.Cookie> cookies)
            {

                StringBuilder cookieString = new StringBuilder();
                string delimiter = string.Empty;

                foreach (var cookie in cookies)
                {
                    cookieString.Append(delimiter);
                    cookieString.Append(cookie.Name);
                    cookieString.Append('=');
                    cookieString.Append(cookie.Value);
                    delimiter = "; ";
                }

                return cookieString.ToString();
            }

            private readonly List<CefSharp.Cookie> _cookies = new List<CefSharp.Cookie>();
            public void Dispose()
            {
            }
        }

        private readonly Lazy<AuthenticateViewModel> viewModel;
        private String _url;

        public AuthenticateView()
        {
            InitializeComponent();
            viewModel = new Lazy<AuthenticateViewModel>(() => ViewHelper.GetViewModel<AuthenticateViewModel>(this));
            browser.Loaded += Browser_Navigated;

            //https://blog.dotnetframework.org/2018/10/26/intercepting-ajax-requests-in-cefsharp-chrome-for-c/
            browser.RenderProcessMessageHandler = new RenderHandler();
            InteractionHandler.browser = browser;
            var jsInterface = new jsMapInterface
            {
                onHookEvent = InteractionHandler.HookEventHandler
            };
            CefSharpSettings.LegacyJavascriptBindingEnabled = true;
            CefSharpSettings.WcfEnabled = true;
            browser.JavascriptObjectRepository.Register("jsInterface", jsInterface, isAsync: false, options: BindingOptions.DefaultBinder);
        }

        private AuthenticateViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public void ShowDialog(object owner)
        {
            browser.IsBrowserInitializedChanged += OnLoad;
            Owner = owner as Window;
            ShowDialog();
        }

        private void OnLoad(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (browser.IsBrowserInitialized)
                browser.Load(_url);
        }

        public void AddUrl(string url)
        {
            _url = url;
        }

        public string GetUrl()
        {
            return browser.Address;
        }

        public String GetCookies(String url)
        {
            var cookieManager = Cef.GetGlobalCookieManager();
            var visitor = new CookieCollector();

            cookieManager.VisitUrlCookies(url, true, visitor);

            var cookies = visitor.Task.GetAwaiter().GetResult();
            var cookieHeader = CookieCollector.GetCookieHeader(cookies);
            return cookieHeader;
        }

        private void Browser_Navigated(object sender, RoutedEventArgs e)
        {
            SetSilent(browser, true); // make it silent, no js error popus.

            try
            {
                var cwb = (ChromiumWebBrowser)sender;
                if (cwb.Address.Equals(ViewModel.OAuthCallbackUrl))
                {
                    Close();
                }
            }
            catch
            {
            }
        }

        public static void SetSilent(ChromiumWebBrowser browser, bool silent)
        {
            //if (browser == null)
            //{
            //    throw new ArgumentNullException(nameof(browser));
            //}

            //// get an IWebBrowser2 from the document
            //var sp = browser.Document as IOleServiceProvider;

            //if (sp == null) return;

            //var iidIWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
            //var iidIWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

            //sp.QueryService(ref iidIWebBrowserApp, ref iidIWebBrowser2, out var webBrowser);

            //webBrowser?.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, new object[] { silent });
        }

        [ComImport]
        [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
    }
}
