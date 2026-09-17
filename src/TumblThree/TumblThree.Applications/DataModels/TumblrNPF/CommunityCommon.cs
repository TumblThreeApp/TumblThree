using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    public class AvatarImage
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("accessories")]
        public List<object> Accessories { get; private set; } = new List<object>();
    }

    public class Community
    {
        [JsonProperty("isMember")]
        public bool IsMember { get; set; }

        [JsonProperty("about", NullValueHandling = NullValueHandling.Ignore)]
        public string About { get; set; }

        [JsonProperty("avatarImage")]
        public List<AvatarImage> AvatarImage { get; private set; } = new List<AvatarImage>();

        [JsonProperty("blogIds", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> BlogIds { get; private set; } = new List<object>();

        [JsonProperty("boards", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> Boards { get; private set; } = new List<object>();

        [JsonProperty("canEdit", NullValueHandling = NullValueHandling.Ignore)]
        public bool CanEdit { get; set; }

        [JsonProperty("canInteract", NullValueHandling = NullValueHandling.Ignore)]
        public bool CanInteract { get; set; }

        [JsonProperty("canInvite", NullValueHandling = NullValueHandling.Ignore)]
        public bool CanInvite { get; set; }

        [JsonProperty("canPost", NullValueHandling = NullValueHandling.Ignore)]
        public bool CanPost { get; set; }

        [JsonProperty("canView")]
        public bool CanView { get; set; }

        [JsonProperty("canViewComments", NullValueHandling = NullValueHandling.Ignore)]
        public bool CanViewComments { get; set; }

        [JsonProperty("categories", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> Categories { get; private set; } = new List<object>();

        [JsonProperty("createdTs", NullValueHandling = NullValueHandling.Ignore)]
        public int CreatedTs { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("guidelines", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> Guidelines { get; private set; } = new List<object>();

        [JsonProperty("guidelinesSummary", NullValueHandling = NullValueHandling.Ignore)]
        public string GuidelinesSummary { get; set; }

        [JsonProperty("hasContentLabel", NullValueHandling = NullValueHandling.Ignore)]
        public bool HasContentLabel { get; set; }

        [JsonProperty("headerImage", NullValueHandling = NullValueHandling.Ignore)]
        public List<HeaderImage> HeaderImage { get; private set; } = new List<HeaderImage>();

        [JsonProperty("joinType")]
        public string JoinType { get; set; }

        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string Language { get; set; }

        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public string Location { get; set; }

        [JsonProperty("memberCount")]
        public int MemberCount { get; set; }

        [JsonProperty("membersOnlineCount", NullValueHandling = NullValueHandling.Ignore)]
        public int MembersOnlineCount { get; set; }

        [JsonProperty("mentionTag")]
        public string MentionTag { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("populationCap", NullValueHandling = NullValueHandling.Ignore)]
        public int PopulationCap { get; set; }

        [JsonProperty("postCount", NullValueHandling = NullValueHandling.Ignore)]
        public int PostCount { get; set; }

        [JsonProperty("relatedCommunities", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> RelatedCommunities { get; private set; } = new List<object>();

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; private set; } = new List<string>();

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("unreadPostCount", NullValueHandling = NullValueHandling.Ignore)]
        public int UnreadPostCount { get; set; }

        [JsonProperty("visibility")]
        public string Visibility { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("invitation", NullValueHandling = NullValueHandling.Ignore)]
        public object Invitation { get; set; }
    }

    public class HeaderImage
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("hasOriginalDimensions")]
        public bool HasOriginalDimensions { get; set; }

        [JsonProperty("mediaKey")]
        public string MediaKey { get; set; }

        [JsonProperty("cropped")]
        public bool? Cropped { get; set; }
    }
}
