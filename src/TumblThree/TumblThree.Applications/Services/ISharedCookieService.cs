using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace TumblThree.Applications.Services
{
    public interface ISharedCookieService
    {
        CookieContainer CookieContainer { get; }
        IEnumerable<Cookie> GetAllCookies(CookieContainer cookieContainer);

        void FillUriCookie(Uri uri, CookieContainer container = null);

        void RefreshAllCookies(CookieContainer cookies);
        void SetUriCookie(IEnumerable cookies);

        void RemoveUriCookie(Uri uri);
    }
}
