using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TumblThree.Applications.Parser
{
    public interface INewTumblParser
    {
        Regex GetPhotoUrlRegex();

        Regex GetGenericPhotoUrlRegex();

        IEnumerable<string> SearchForPhotoUrl(string searchableText);

        IEnumerable<string> SearchForGenericPhotoUrl(string searchableText);

        bool IsNewTumblUrl(string url);
    }
}
