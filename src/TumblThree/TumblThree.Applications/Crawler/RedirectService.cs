using System.Collections.Generic;

namespace TumblThree.Applications.Crawler
{
    public class RedirectService
    {

        protected static string CurrentUrl;
        protected static Dictionary<string, string> Cache = new Dictionary<string, string>();

        public static void CloseCurrent()
        {
            CurrentUrl = null;
        }

        public static void StartNew(string url)
        {
            CurrentUrl = url;
        }

        public static void UpdateCurrent(string newUrl)
        {
            if(CurrentUrl != null)
            {
                if (!Cache.ContainsKey(CurrentUrl)) Cache.Add(CurrentUrl, newUrl);
                else Cache[CurrentUrl] = newUrl;
            }
        }

        public static string GetRedirectedUrl(string url)
        {
            return Cache.ContainsKey(url) ? Cache[url] : url;
        }
    }
}