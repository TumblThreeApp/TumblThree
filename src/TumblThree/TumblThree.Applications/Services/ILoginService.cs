using System.Net;
using System.Threading.Tasks;

namespace TumblThree.Applications.Services
{
    public interface ILoginService
    {
        Task PerformTumblrLoginAsync(string login, string password);

        Task PerformLogoutAsync(Provider provider);

        Task PerformTumblrTFALoginAsync(string login, string tumblrTFAAuthCode);

        bool CheckIfTumblrTFANeeded();

        Task<bool> CheckIfLoggedInAsync();

        Task<string> GetUsernameAsync(Provider provider, string document = null);

        void AddCookies(CookieCollection cookies);
    }
}
