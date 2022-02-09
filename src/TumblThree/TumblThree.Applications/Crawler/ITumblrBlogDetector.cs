using System.Threading.Tasks;

namespace TumblThree.Applications.Crawler
{
    public interface ITumblrBlogDetector
    {
        Task<bool> IsHiddenTumblrBlogAsync(string url);

        Task<bool> IsPasswordProtectedTumblrBlogAsync(string url);

        Task<bool> IsTumblrBlogAsync(string url);

        Task<bool> IsTumblrBlogWithCustomDomainAsync(string url);

        Task<string> GetUrlRedirection(string url);
    }
}
