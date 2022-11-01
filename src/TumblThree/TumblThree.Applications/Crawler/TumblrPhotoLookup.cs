using System;
using System.Collections.Generic;

namespace TumblThree.Applications.Crawler
{
    public class TumblrPhotoLookup
    {
        private readonly Dictionary<string, Tuple<string, string, int>> data = new Dictionary<string, Tuple<string, string, int>>();

        private int ResolutionOf(string id)
        {
            return data.ContainsKey(id) ? data[id].Item3 : 0;
        }

        public void AddOrReplace(string id, string url, string postedUrl, int resolution)
        {
            if (resolution <= ResolutionOf(id)) return;
            data[id] = new Tuple<string, string, int>(url, postedUrl, resolution);
        }

        public IEnumerable<(string, string)> GetUrls()
        {
            foreach(var tuple in data.Values)
            {
                yield return (tuple.Item1, tuple.Item2);
            }
        }
    }
}
