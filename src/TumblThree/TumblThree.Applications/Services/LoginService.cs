using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TumblThree.Applications.Extensions;

namespace TumblThree.Applications.Services
{
    [Export]
    [Export(typeof(ILoginService))]
    internal class LoginService : ILoginService
    {
        private readonly IShellService shellService;
        private readonly ISharedCookieService cookieService;
        private readonly IWebRequestFactory webRequestFactory;
        private string tumblrKey = string.Empty;
        private bool tfaNeeded = false;
        private string tumblrTFAKey = string.Empty;

        [ImportingConstructor]
        public LoginService(IShellService shellService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService)
        {
            this.shellService = shellService;
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
        }

        public async Task PerformTumblrLoginAsync(string login, string password)
        {
            try
            {
                var document = await RequestTumblrKey().ConfigureAwait(false);
                tumblrKey = ExtractTumblrKey(document);
                await Register(login, password).ConfigureAwait(false);
                document = await Authenticate(login, password).ConfigureAwait(false);
                if (tfaNeeded)
                {
                    tumblrTFAKey = ExtractTumblrTFAKey(document);
                }
            }
            catch (TimeoutException)
            {
            }
        }

        public void AddCookies(CookieCollection cookies)
        {
            cookieService.SetUriCookie(cookies);
        }

        public void PerformTumblrLogout()
        {
            const string url = "https://www.tumblr.com/logout";
            var request = webRequestFactory.CreateGetRequest(url);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            using (var response = request.GetResponse() as HttpWebResponse)
            {
                cookieService.SetUriCookie(response.Cookies);
            }
        }

        public bool CheckIfTumblrTFANeeded() => tfaNeeded;

        public async Task PerformTumblrTFALoginAsync(string login, string tumblrTFAAuthCode)
        {
            try
            {
                await SubmitTFAAuthCode(login, tumblrTFAAuthCode).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
            }
        }

        private static string ExtractTumblrKey(string document) => Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;

        private async Task<string> RequestTumblrKey()
        {
            const string url = "https://www.tumblr.com/login";
            var request = webRequestFactory.CreateGetRequest(url);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut).ConfigureAwait(false) as HttpWebResponse)
            {
                cookieService.SetUriCookie(response.Cookies);
                using (var stream = webRequestFactory.GetStreamForApiRequest(response.GetResponseStream()))
                {
                    using (var buffer = new BufferedStream(stream))
                    {
                        using (var reader = new StreamReader(buffer))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }

        private async Task Register(string login, string password)
        {
            const string url = "https://www.tumblr.com/svc/account/register";
            const string referer = "https://www.tumblr.com/login";
            var headers = new Dictionary<string, string>();
            var request = webRequestFactory.CreatePostXhrRequest(url, referer, headers);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            var parameters = new Dictionary<string, string>
            {
                { "determine_email", login },
                { "user[email]", string.Empty },
                { "user[password]", string.Empty },
                { "tumblelog[name]", string.Empty },
                { "user[age]", string.Empty },
                { "context", "no_referer" },
                { "version", "STANDARD" },
                { "follow", string.Empty },
                { "form_key", tumblrKey },
                { "seen_suggestion", "0" },
                { "used_suggestion", "0" },
                { "used_auto_suggestion", "0" },
                { "about_tumblr_slide", string.Empty },
                { "tracking_url", "/login" },
                { "tracking_version", "modal" },
                {
                    "random_username_suggestions",
                    "[\"KawaiiBouquetStranger\",\"KeenTravelerFury\",\"RainyMakerTastemaker\",\"SuperbEnthusiastCollective\",\"TeenageYouthFestival\"]"
                },
                { "action", "signup_determine" },
            };
            await webRequestFactory.PerformPostRequestAsync(request, parameters).ConfigureAwait(false);
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut).ConfigureAwait(false) as HttpWebResponse)
            {
                cookieService.SetUriCookie(response.Cookies);
            }
        }

        private async Task<string> Authenticate(string login, string password)
        {
            const string url = "https://www.tumblr.com/login";
            const string referer = "https://www.tumblr.com/login";
            var headers = new Dictionary<string, string>();
            var request = webRequestFactory.CreatePostRequest(url, referer, headers);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            var parameters = new Dictionary<string, string>
            {
                { "determine_email", login },
                { "user[email]", login },
                { "user[password]", password },
                { "tumblelog[name]", string.Empty },
                { "user[age]", string.Empty },
                { "context", "no_referer" },
                { "version", "STANDARD" },
                { "follow", string.Empty },
                { "form_key", tumblrKey },
                { "seen_suggestion", "0" },
                { "used_suggestion", "0" },
                { "used_auto_suggestion", "0" },
                { "about_tumblr_slide", string.Empty },
                {
                    "random_username_suggestions",
                    "[\"KawaiiBouquetStranger\",\"KeenTravelerFury\",\"RainyMakerTastemaker\",\"SuperbEnthusiastCollective\",\"TeenageYouthFestival\"]"
                },
                { "action", "signup_determine" }
            };
            await webRequestFactory.PerformPostRequestAsync(request, parameters).ConfigureAwait(false);
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut).ConfigureAwait(false) as HttpWebResponse)
            {
                if (request.Address == new Uri("https://www.tumblr.com/login")) // TFA required
                {
                    tfaNeeded = true;
                    cookieService.SetUriCookie(response.Cookies);
                    using (var stream = webRequestFactory.GetStreamForApiRequest(response.GetResponseStream()))
                    {
                        using (var buffer = new BufferedStream(stream))
                        {
                            using (var reader = new StreamReader(buffer))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }

                //cookieService.SetUriCookie(request.CookieContainer.GetCookies(new Uri("https://www.tumblr.com/")));
                cookieService.SetUriCookie(response.Cookies);
                return string.Empty;
            }
        }

        private static string ExtractTumblrTFAKey(string document) => Regex.Match(document, "name=\"tfa_form_key\" value=\"([\\S]*)\"/>").Groups[1].Value;

        private async Task SubmitTFAAuthCode(string login, string tumblrTFAAuthCode)
        {
            const string url = "https://www.tumblr.com/login";
            const string referer = "https://www.tumblr.com/login";
            var headers = new Dictionary<string, string>();
            var request = webRequestFactory.CreatePostRequest(url, referer, headers);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            var parameters = new Dictionary<string, string>
            {
                { "determine_email", login },
                { "user[email]", login },
                { "tumblelog[name]", string.Empty },
                { "user[age]", string.Empty },
                { "context", "login" },
                { "version", "STANDARD" },
                { "follow", string.Empty },
                { "form_key", tumblrKey },
                { "tfa_form_key", tumblrTFAKey },
                { "tfa_response_field", tumblrTFAAuthCode },
                { "http_referer", "https://www.tumblr.com/login" },
                { "seen_suggestion", "0" },
                { "used_suggestion", "0" },
                { "used_auto_suggestion", "0" },
                { "about_tumblr_slide", string.Empty },
                {
                    "random_username_suggestions",
                    "[\"KawaiiBouquetStranger\",\"KeenTravelerFury\",\"RainyMakerTastemaker\",\"SuperbEnthusiastCollective\",\"TeenageYouthFestival\"]"
                },
                { "action", "signup_determine" }
            };
            await webRequestFactory.PerformPostRequestAsync(request, parameters).ConfigureAwait(false);
            using (var response = await request.GetResponseAsync().TimeoutAfter(shellService.Settings.TimeOut).ConfigureAwait(false) as HttpWebResponse)
            {
                //cookieService.SetUriCookie(request.CookieContainer.GetCookies(new Uri("https://www.tumblr.com/")));
                cookieService.SetUriCookie(response.Cookies);
            }
        }

        public bool CheckIfLoggedInAsync()
        {
            var request = webRequestFactory.CreateGetRequest("https://www.tumblr.com/");
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            return request.CookieContainer.GetCookieHeader(new Uri("https://www.tumblr.com/")).Contains("pfs");
        }

        public async Task<string> GetTumblrUsernameAsync()
        {
            const string tumblrAccountSettingsUrl = "https://www.tumblr.com/settings/account";
            var request = webRequestFactory.CreateGetRequest(tumblrAccountSettingsUrl);
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            var document = await webRequestFactory.ReadRequestToEndAsync(request).ConfigureAwait(false);
            return ExtractTumblrUsername(document);
        }

        private static string ExtractTumblrUsername(string document) => Regex.Match(document, "<p class=\"accordion_label accordion_trigger\">([\\S]*)</p>").Groups[1].Value;
    }
}
