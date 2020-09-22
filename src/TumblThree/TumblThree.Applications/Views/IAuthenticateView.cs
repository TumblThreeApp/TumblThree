using System;
using System.Net;
using System.Threading.Tasks;
using System.Waf.Applications;

namespace TumblThree.Applications.Views
{
    public interface IAuthenticateView : IView
    {
        void ShowDialog(object owner);

        event EventHandler Closed;

        void AddUrl(string url);

        string GetUrl();

        Task<CookieCollection> GetCookies(String url);
    }
}
