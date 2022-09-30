using System;
using System.Net;
using System.Threading.Tasks;
using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IAuthenticateView : IView
    {
        void ShowDialog(object owner, string url, string cookieDomain);

        event EventHandler Closed;

        string GetUrl();

        Task<CookieCollection> GetCookies(String url);
    }
}
