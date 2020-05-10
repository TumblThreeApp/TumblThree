using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

using TumblThree.Applications.Extensions;
using TumblThree.Applications.Services;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ITumblrBlogDetector))]
    public class TumblrBlogDetector : ITumblrBlogDetector
    {
        private readonly IHttpRequestFactory webRequestFactory;
        private readonly IShellService shellService;
        protected readonly ISharedCookieService cookieService;

        [ImportingConstructor]
        public TumblrBlogDetector(IShellService shellService, ISharedCookieService cookieService,
            IHttpRequestFactory webRequestFactory)
        {
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
            this.shellService = shellService;
        }

        public async Task<bool> IsTumblrBlogAsync(string url)
        {
            string location = await GetUrlRedirection(url);
            return !location.Contains("login_required");
        }

        public async Task<bool> IsHiddenTumblrBlogAsync(string url)
        {
            string location = await GetUrlRedirection(url);
            return location.Contains("login_required") || location.Contains("dashboard/blog/");
        }

        public async Task<bool> IsPasswordProtectedTumblrBlogAsync(string url)
        {
            string location = await GetUrlRedirection(url);
            return location.Contains("blog_auth");
        }

        private async Task<string> GetUrlRedirection(string url)
        {
            var res = await webRequestFactory.GetReqeust(url);
            //cookieService.FillUriCookie(new Uri("https://www.tumblr.com/"));
            return res.Headers.Location.AbsoluteUri;
        }
    }
}
