using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using TumblThree.Applications.Properties;
using TumblThree.Domain;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IConfirmTumblrPrivacyConsent))]
    [Export]
    internal class ConfirmTumblrPrivacyConsent : IConfirmTumblrPrivacyConsent
    {
        private readonly IHttpRequestFactory webRequestFactory;
        private readonly IShellService shellService;
        protected readonly ISharedCookieService cookieService;
        private string tumblrKey = string.Empty;

        [ImportingConstructor]
        public ConfirmTumblrPrivacyConsent(IShellService shellService, ISharedCookieService cookieService, IHttpRequestFactory webRequestFactory)
        {
            this.webRequestFactory = webRequestFactory;
            this.cookieService = cookieService;
            this.shellService = shellService;
        }

        public async Task ConfirmPrivacyConsentAsync()
        {
            try
            {
                await PerformPrivacyConsentRequestAsync();
            }
            catch (TimeoutException timeoutException)
            {
                Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.TimeoutReachedShort, Resources.ConfirmingTumblrPrivacyConsent), timeoutException);
                shellService.ShowError(timeoutException, Resources.ConfirmingTumblrPrivacyConsentFailed);
            }
            catch (Exception exception)
            {
                Logger.Error("{0}, {1}", string.Format(CultureInfo.CurrentCulture, Resources.ConfirmingTumblrPrivacyConsentFailed), exception);
                shellService.ShowError(exception, Resources.ConfirmingTumblrPrivacyConsentFailed);
            }
        }

        private async Task PerformPrivacyConsentRequestAsync()
        {
            if (CheckIfLoggedIn())
                return;

            await UpdateTumblrKey();
            const string referer = @"https://www.tumblr.com/privacy/consent?redirect=";
            var headers = new Dictionary<string, string> { { "X-tumblr-form-key", tumblrKey } };
            HttpRequestMessage request =
                webRequestFactory.PostXhrReqeustMessage("https://www.tumblr.com/svc/privacy/consent", referer, headers);
            const string requestBody = "{\"eu_resident\":true,\"gdpr_is_acceptable_age\":true,\"gdpr_consent_core\":true,\"gdpr_consent_first_party_ads\":true,\"gdpr_consent_third_party_ads\":true,\"gdpr_consent_search_history\":true,\"redirect_to\":\"\"}";
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");
            await webRequestFactory.PostXHRReqeustAsync(request, requestBody);
        }

        private async Task UpdateTumblrKey()
        {
            const string requestUrl = "https://www.tumblr.com/";
            string document = await GetRequestAsync(requestUrl);
            tumblrKey = ExtractTumblrKey(document);
        }

        private static string ExtractTumblrKey(string document) => Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;

        private async Task<string> GetRequestAsync(string requestUrl)
        {
            var request = await webRequestFactory.GetReqeust(requestUrl);
            return await request.Content.ReadAsStringAsync();
        }

        public bool CheckIfLoggedIn()
        {
            return cookieService.CookieContainer.GetCookieHeader(new Uri("https://www.tumblr.com/")).Contains("pfs");
        }
    }
}
