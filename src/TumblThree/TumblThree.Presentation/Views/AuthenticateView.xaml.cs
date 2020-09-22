using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using CefSharp;
using CefSharp.Wpf;
using System.Net;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for SettingsView.xaml.
    /// </summary>
    [Export(typeof(IAuthenticateView))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AuthenticateView : IAuthenticateView
    {
        private readonly Lazy<AuthenticateViewModel> viewModel;
        private String _url;

        public AuthenticateView()
        {
            InitializeComponent();
            viewModel = new Lazy<AuthenticateViewModel>(() => ViewHelper.GetViewModel<AuthenticateViewModel>(this));
            browser.Loaded += Browser_Navigated;
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

        public async Task<CookieCollection> GetCookies(String url)
        {
            var cookieManager = Cef.GetGlobalCookieManager();
            var cookies = await cookieManager.VisitUrlCookiesAsync(url, true);

            var cookieCollection = GetCookies(cookies);
            return cookieCollection;
        }

        private static CookieCollection GetCookies(List<CefSharp.Cookie> cookies)
        {
            CookieCollection cookieCollection = new CookieCollection();
            foreach (var cookie in cookies)
            {
                var transferCookie = new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
                transferCookie.Expires = cookie.Expires.Value;
                transferCookie.HttpOnly = cookie.HttpOnly;
                cookieCollection.Add(transferCookie);
            }
            return cookieCollection;
        }

        private void Browser_Navigated(object sender, RoutedEventArgs e)
        {
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

        public static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null)
            {
                throw new ArgumentNullException("browser");
            }

            // get an IWebBrowser2 from the document
            var sp = browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                var IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                var IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                {
                    webBrowser.GetType()
                              .InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty,
                                  null, webBrowser, new object[] { silent });
                }
            }
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid,
                [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
    }
}
