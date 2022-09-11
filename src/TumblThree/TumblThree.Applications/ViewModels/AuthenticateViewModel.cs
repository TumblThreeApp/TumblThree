using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;
using System.Waf.Applications;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class AuthenticateViewModel : ViewModel<IAuthenticateView>
    {
        private string _oauthCallbackUrl;

        [ImportingConstructor]
        public AuthenticateViewModel(IAuthenticateView view, IShellService shellService, ILoginService loginService)
            : base(view)
        {
            view.Closed += ViewClosed;
            ShellService = shellService;
            LoginService = loginService;
            _oauthCallbackUrl = shellService.Settings.OAuthCallbackUrl;
        }

        public IShellService ShellService { get; }
        public ILoginService LoginService { get; }

        public string OAuthCallbackUrl
        {
            get => _oauthCallbackUrl;
            set => SetProperty(ref _oauthCallbackUrl, value);
        }

        public void ShowDialog(object owner) => ViewCore.ShowDialog(owner);

        private void ViewClosed(object sender, EventArgs e)
        {
        }

        public Task<CookieCollection> GetCookies(String url) => ViewCore.GetCookies(url);

        public void AddUrl(string url) => ViewCore.AddUrl(url);

        public string GetUrl() => ViewCore.GetUrl();

        public void SetDomain(string domain) => ViewCore.SetDomain(domain);
    }
}
