using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TumblThree.Applications.Extensions;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;

namespace TumblThree.Applications.Services
{
    public enum Provider
    {
        Tumblr,
        Twitter
    }

    [Export]
    [Export(typeof(ILoginService))]
    internal class LoginService : ILoginService
    {
        private readonly IShellService shellService;
        private readonly ISharedCookieService cookieService;
        private readonly IWebRequestFactory webRequestFactory;
        private readonly ExportFactory<AuthenticateViewModel> authenticateViewModelFactory;
        private string tumblrKey = string.Empty;
        private bool tfaNeeded;
        private string tumblrTFAKey = string.Empty;

        [ImportingConstructor]
        public LoginService(IShellService shellService, IWebRequestFactory webRequestFactory, ISharedCookieService cookieService,
            ExportFactory<AuthenticateViewModel> authenticateViewModelFactory)
        {
            this.shellService = shellService;
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
            this.authenticateViewModelFactory = authenticateViewModelFactory;
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

        public async Task PerformLogoutAsync(Provider provider)
        {
            switch (provider)
            {
                case Provider.Tumblr:
                    const string url = "https://www.tumblr.com/logout";
                    var request = webRequestFactory.CreateGetRequest(url);
                    cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        cookieService.SetUriCookie(response.Cookies);
                    }
                    break;
                case Provider.Twitter:
                    cookieService.RemoveUriCookie(new Uri("https://twitter.com"));
                    AuthenticateViewModel authenticateViewModel = authenticateViewModelFactory.CreateExport().Value;
                    await authenticateViewModel.DeleteCookies("https://twitter.com");
                    break;
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

        public async Task<bool> CheckIfLoggedInAsync()
        {
            var request = webRequestFactory.CreateGetRequest("https://www.tumblr.com/");
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            return request.CookieContainer.GetCookieHeader(new Uri("https://www.tumblr.com/")).Contains("pfs");
        }

        public async Task<string> GetUsernameAsync(Provider provider, string document = null)
        {
            try
            {
                switch (provider)
                {
                    case Provider.Tumblr:
                        const string tumblrAccountSettingsUrl = "https://www.tumblr.com/settings/account";
                        var request = webRequestFactory.CreateGetRequest(tumblrAccountSettingsUrl);
                        cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
                        document = await webRequestFactory.ReadRequestToEndAsync(request).ConfigureAwait(false);
                        return ExtractUsername(provider, document);
                    case Provider.Twitter:
                        if (document is null)
                        {
                            AuthenticateViewModel authenticateViewModel = authenticateViewModelFactory.CreateExport().Value;
                            document = await authenticateViewModel.GetDocument();
                        }
                        return ExtractUsername(provider, document);
                    default:
                        return "";
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(WebException))
                {
                    var we = (WebException)ex;
                    if (we.Response != null && ((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                }
                Logger.Error("LoginService.GetTumblrUsernameAsync: {0}", ex);
                throw;
            }
        }

        private static string ExtractUsername(Provider provider, string document)
        {
            try
            {
                switch (provider)
                {
                    case Provider.Tumblr:
                        var regex = new Regex("window\\['___INITIAL_STATE___'] = ({.*});");
                        var json = regex.Match(document ?? "").Groups[1].Value;
                        var obj = JObject.Parse(json.Replace(":undefined", ":null"));
                        var value = obj["Settings"];
                        if (value == null) return null;
                        value = obj["Settings"]["email"];
                        if (value == null) value = obj["Settings"]["settings"]["email"];
                        return value.ToString();
                    case Provider.Twitter:
                        var regex2 = new Regex("window\\.__INITIAL_STATE__=({.*});window\\.__META_DATA__");
                        var json2 = regex2.Match(document ?? "").Groups[1].Value;
                        if (string.IsNullOrEmpty(json2)) return null;
                        var obj2 = JObject.Parse(json2.Replace(":undefined", ":null"));
                        var value2 = obj2["settings"]?["remote"]?["settings"]?["screen_name"];
                        if (value2 == null) return null;
                        return value2.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LoginService.ExtractUsername: {0}", ex);
            }
            return "n/a";
        }
    }
}
