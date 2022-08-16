using System.Net;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface ILoginService
    {
        Task PerformTumblrLoginAsync(string login, string password);

        void PerformTumblrLogout();

        Task PerformTumblrTFALoginAsync(string login, string tumblrTFAAuthCode);

        bool CheckIfTumblrTFANeeded();

        Task<bool> CheckIfLoggedInAsync();

        Task<string> GetTumblrUsernameAsync();

        void AddCookies(CookieCollection cookies);
    }
}
