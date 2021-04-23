using System;
using System.Collections;
using System.Linq;
using System.Net;

namespace TumblThree.Applications
{
    public static class CookieParser
    {
        public static CookieCollection GetAllCookiesFromHeader(string strHeader, string strHost)
        {
            CookieCollection cc = new CookieCollection();
            if (strHeader.Length > 0)
            {
                ArrayList al = ConvertCookieHeaderToArrayList(strHeader);
                cc = ConvertCookieArraysToCookieCollection(al, strHost);
            }
            return cc;
        }

        private static ArrayList ConvertCookieHeaderToArrayList(string strCookHeader)
        {
            strCookHeader = strCookHeader.Replace("\r", "").Replace("\n", "");
            string[] strCookieParts = strCookHeader.Split(',');
            ArrayList al = new ArrayList();
            int i = 0;
            int n = strCookieParts.Length;
            while (i < n)
            {
                if (strCookieParts[i].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    al.Add(strCookieParts[i] + "," + strCookieParts[i + 1]);
                    i = i + 1;
                }
                else if (i + 1 < n && (!strCookieParts[i + 1].Contains("=") ||
                    strCookieParts[i + 1].Count(x => x == '=') == 1 && strCookieParts[i + 1].IndexOf("expires=", StringComparison.OrdinalIgnoreCase) > 0))
                {
                    strCookieParts[i + 1] = strCookieParts[i] + strCookieParts[i + 1];
                }
                else
                {
                    al.Add(strCookieParts[i]);
                }
                i = i + 1;
            }

            return al;
        }

        private static CookieCollection ConvertCookieArraysToCookieCollection(ArrayList al, string strHost)
        {
            CookieCollection cc = new CookieCollection();

            int alcount = al.Count;
            string strEachCook;
            string[] strEachCookParts;
            for (int i = 0; i < alcount; i++)
            {
                strEachCook = al[i].ToString();
                strEachCookParts = strEachCook.Split(';');
                int intEachCookPartsCount = strEachCookParts.Length;
                string[] NameValuePairTemp;
                Cookie cookTemp = new Cookie();

                for (int j = 0; j < intEachCookPartsCount; j++)
                {
                    if (j == 0)
                    {
                        string strCNameAndCValue = strEachCookParts[j];
                        if (strCNameAndCValue.Length > 0)
                        {
                            int firstEqual = strCNameAndCValue.IndexOf("=");
                            string firstName = strCNameAndCValue.Substring(0, firstEqual);
                            string allValue = strCNameAndCValue.Substring(firstEqual + 1, strCNameAndCValue.Length - (firstEqual + 1));
                            cookTemp.Name = firstName;
                            cookTemp.Value = allValue;
                        }
                        continue;
                    }
                    string strPNameAndPValue;
                    if (strEachCookParts[j].IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue.Length > 0)
                        {
                            NameValuePairTemp = strPNameAndPValue.Split('=');
                            if (NameValuePairTemp[1].Length > 0)
                            {
                                cookTemp.Path = NameValuePairTemp[1];
                            }
                            else
                            {
                                cookTemp.Path = "/";
                            }
                        }
                        continue;
                    }

                    if (strEachCookParts[j].IndexOf("domain", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        strPNameAndPValue = strEachCookParts[j];
                        if (strPNameAndPValue.Length > 0)
                        {
                            NameValuePairTemp = strPNameAndPValue.Split('=');

                            if (NameValuePairTemp[1].Length > 0)
                            {
                                cookTemp.Domain = NameValuePairTemp[1];
                            }
                            else
                            {
                                cookTemp.Domain = strHost;
                            }
                        }
                        continue;
                    }
                }

                if (cookTemp.Path.Length == 0)
                {
                    cookTemp.Path = "/";
                }
                if (cookTemp.Domain.Length == 0)
                {
                    cookTemp.Domain = strHost;
                }
                cc.Add(cookTemp);
            }
            return cc;
        }
    }
}
