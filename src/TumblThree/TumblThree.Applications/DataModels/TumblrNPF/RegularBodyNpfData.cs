using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    public class RegularBodyNpfData
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("media")]
        public Media Media { get; set; }

        [JsonProperty("poster")]
        public List<Poster> Poster { get; private set; } = new List<Poster>();

        [JsonProperty("filmstrip")]
        public Filmstrip Filmstrip { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }
    }

    public class Media
    {
        [JsonProperty("media_key")]
        public string MediaKey { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
