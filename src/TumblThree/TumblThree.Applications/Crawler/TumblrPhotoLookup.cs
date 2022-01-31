using System;
using System.Collections.Generic;

namespace TumblThree.Applications.Crawler
{
    public class TumblrPhotoLookup
    {
        private readonly Dictionary<string, Tuple<string, int>> data = new Dictionary<string, Tuple<string, int>>();

        private int ResolutionOf(string id)
        {
            return data.ContainsKey(id) ? data[id].Item2 : 0;
        }

        public void AddOrReplace(string id, string url, int resolution)
        {
            if (resolution <= ResolutionOf(id)) return;
            data[id] = new Tuple<string, int>(url, resolution);
        }

        public IEnumerable<string> GetUrls()
        {
            foreach(var tuple in data.Values)
            {
                yield return tuple.Item1;
            }
        }
    }
}
