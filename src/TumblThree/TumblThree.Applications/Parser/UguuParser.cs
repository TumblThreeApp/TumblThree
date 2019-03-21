using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TumblThree.Domain.Models;

namespace TumblThree.Applications.Parser
{
    public class UguuParser : IUguuParser
    {
        public Regex GetUguuUrlRegex() => new Regex("(http[A-Za-z0-9_/:.]*a.uguu.se/(.*))");

        public string GetUguuId(string url) => GetUguuUrlRegex().Match(url).Groups[2].Value;

        public string CreateUguuUrl(string uguuId, string detectedUrl, UguuTypes uguuType)
        {
            string url;
            switch (uguuType)
            {
                case UguuTypes.Mp4:
                    url = @"https://a.uguu.se/" + uguuId + ".mp4";
                    break;
                case UguuTypes.Webm:
                    url = @"https://a.uguu.se/" + uguuId + ".webm";
                    break;
                case UguuTypes.Any:
                    url = detectedUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(uguuType));
            }

            return url;
        }

        public IEnumerable<string> SearchForUguuUrl(string searchableText, UguuTypes uguuType)
        {
            var regex = GetUguuUrlRegex();
            foreach (Match match in regex.Matches(searchableText))
            {
                var temp = match.Groups[0].ToString();
                var id = match.Groups[2].Value;
                var url = temp.Split('\"').First();

                yield return CreateUguuUrl(id, url, uguuType);
            }
        }
    }
}
