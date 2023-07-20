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
        public AuthenticateViewModel(IAuthenticateView view, IShellService shellService, ILoginService loginService, IEnvironmentService environmentService)
            : base(view)
        {
            view.Closed += ViewClosed;
            ShellService = shellService;
            LoginService = loginService;
            EnvironmentService = environmentService;
            _oauthCallbackUrl = shellService.Settings.OAuthCallbackUrl;
        }

        public IShellService ShellService { get; }
        public ILoginService LoginService { get; }
        public IEnvironmentService EnvironmentService { get; }

        public string OAuthCallbackUrl
        {
            get => _oauthCallbackUrl;
            set => SetProperty(ref _oauthCallbackUrl, value);
        }

        public void ShowDialog(object owner, string url, string cookieDomain) => ViewCore.ShowDialog(owner, url, cookieDomain);

        private void ViewClosed(object sender, EventArgs e)
        {
        }

        public Task<CookieCollection> GetCookies(string url) => ViewCore.GetCookies(url);

        public Task DeleteCookies(string url) => ViewCore.DeleteCookies(url);

        public string GetUrl() => ViewCore.GetUrl();

        public Task<string> GetDocument() => ViewCore.GetDocument();

        public string AppSettingsPath => EnvironmentService.AppSettingsPath;
    }
}
