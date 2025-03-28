using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.Bluesky
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    public class ProfileAssociated
    {
        [JsonProperty("lists", NullValueHandling = NullValueHandling.Ignore)]
        public int Lists { get; set; }

        [JsonProperty("feedgens", NullValueHandling = NullValueHandling.Ignore)]
        public int Feedgens { get; set; }

        [JsonProperty("starterPacks", NullValueHandling = NullValueHandling.Ignore)]
        public int StarterPacks { get; set; }

        [JsonProperty("labeler", NullValueHandling = NullValueHandling.Ignore)]
        public bool Labeler { get; set; }

        [JsonProperty("chat", NullValueHandling = NullValueHandling.Ignore)]
        public ProfileChat Chat { get; set; }
    }

    public class ProfileChat
    {
        [JsonProperty("allowIncoming", NullValueHandling = NullValueHandling.Ignore)]
        public string AllowIncoming { get; set; }
    }

    public class PinnedPost
    {
        [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
        public string Cid { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }
    }

    public class Profile
    {
        [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
        public string Did { get; set; }

        [JsonProperty("handle", NullValueHandling = NullValueHandling.Ignore)]
        public string Handle { get; set; }

        [JsonProperty("displayName", NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }

        [JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string Avatar { get; set; }

        [JsonProperty("associated", NullValueHandling = NullValueHandling.Ignore)]
        public ProfileAssociated Associated { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> Labels { get; } = new List<object>();

        [JsonProperty("createdAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("indexedAt", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IndexedAt { get; set; }

        [JsonProperty("banner", NullValueHandling = NullValueHandling.Ignore)]
        public string Banner { get; set; }

        [JsonProperty("followersCount", NullValueHandling = NullValueHandling.Ignore)]
        public int FollowersCount { get; set; }

        [JsonProperty("followsCount", NullValueHandling = NullValueHandling.Ignore)]
        public int FollowsCount { get; set; }

        [JsonProperty("postsCount", NullValueHandling = NullValueHandling.Ignore)]
        public int PostsCount { get; set; }

        [JsonProperty("pinnedPost", NullValueHandling = NullValueHandling.Ignore)]
        public PinnedPost PinnedPost { get; set; }
    }
}
