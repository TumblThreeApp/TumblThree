﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Net;
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
        private readonly IWebRequestFactory webRequestFactory;
        private readonly IShellService shellService;
        protected readonly ISharedCookieService cookieService;
        private string tumblrKey = string.Empty;

        [ImportingConstructor]
        public ConfirmTumblrPrivacyConsent(IShellService shellService, ISharedCookieService cookieService, IWebRequestFactory webRequestFactory)
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
                shellService.ShowError(new TumblrPrivacyConsentException(exception), Resources.ConfirmingTumblrPrivacyConsentFailed);
            }
        }

        private async Task PerformPrivacyConsentRequestAsync()
        {
            if (CheckIfLoggedInAsync())
            {
                return;
            }

            await UpdateTumblrKey();
            const string referer = @"https://www.tumblr.com/privacy/consent?redirect=";
            var headers = new Dictionary<string, string> { { "X-tumblr-form-key", tumblrKey } };
            HttpWebRequest request =
                webRequestFactory.CreatePostXhrRequest("https://www.tumblr.com/svc/privacy/consent", referer, headers);
            const string requestBody = "{\"eu_resident\":true,\"gdpr_is_acceptable_age\":true,\"gdpr_consent_core\":true,\"gdpr_consent_first_party_ads\":true,\"gdpr_consent_third_party_ads\":true,\"gdpr_consent_search_history\":true,\"redirect_to\":\"\"}";
            request.ContentType = "application/json";
            await webRequestFactory.PerformPostXHRRequestAsync(request, requestBody);
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                cookieService.SetUriCookie(response.Cookies);
            }
        }

        private async Task UpdateTumblrKey()
        {
            string document = await GetRequestAsync();
            tumblrKey = ExtractTumblrKey(document);
        }

        private static string ExtractTumblrKey(string document) => Regex.Match(document, "id=\"tumblr_form_key\" content=\"([\\S]*)\">").Groups[1].Value;

        private async Task<string> GetRequestAsync()
        {
            const string requestUrl = "https://www.tumblr.com/";
            HttpWebRequest request = webRequestFactory.CreateGetRequest(requestUrl);
            return await webRequestFactory.ReadRequestToEndAsync(request);
        }

        public bool CheckIfLoggedInAsync()
        {
            HttpWebRequest request = webRequestFactory.CreateGetRequest("https://www.tumblr.com/");
            cookieService.GetUriCookie(request.CookieContainer, new Uri("https://www.tumblr.com/"));
            return request.CookieContainer.GetCookieHeader(new Uri("https://www.tumblr.com/")).Contains("pfs");
        }
    }
}
