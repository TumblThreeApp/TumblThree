using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ISharedCookieService))]
    [Export]
    public class SharedCookieService : ISharedCookieService
    {
        public CookieContainer CookieContainer { get; } = new CookieContainer();

        public void FillUriCookie(Uri uri, CookieContainer container = null)
        {
            if (container == null) container = this.CookieContainer;
            foreach (Cookie cookie in CookieContainer.GetCookies(uri))
            {
                container.Add(cookie);
            }
        }

        public void RefreshAllCookies(CookieContainer originCookies)
        {
            foreach (Cookie cookie in this.GetAllCookies(originCookies))
            {
                CookieContainer.Add(cookie);
            }
        }
        public void SetUriCookie(IEnumerable cookies)
        {
            foreach (Cookie cookie in cookies)
            {
                CookieContainer.Add(cookie);
            }
        }

        public void RemoveUriCookie(Uri uri)
        {
            foreach (Cookie cookie in this.CookieContainer.GetCookies(uri))
            {
                cookie.Expired = true;
            }
        }

        public IEnumerable<Cookie> GetAllCookies(CookieContainer cookieContainer)
        {
            var k = (Hashtable)cookieContainer
                                     .GetType().GetField("m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic)
                                     .GetValue(cookieContainer);
            foreach (DictionaryEntry element in k)
            {
                var l = (SortedList)element.Value.GetType()
                                                  .GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic)
                                                  .GetValue(element.Value);
                foreach (object e in l)
                {
                    var cl = (CookieCollection)((DictionaryEntry)e).Value;
                    foreach (Cookie fc in cl)
                    {
                        yield return fc;
                    }
                }
            }
        }
    }
}
