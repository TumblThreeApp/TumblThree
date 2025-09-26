using System.Collections.Generic;
using Newtonsoft.Json;

namespace TumblThree.Applications.DataModels.TumblrNPF
{
    public class PostsResponse
    {
        [JsonProperty("meta")]
        public Meta Meta { get; set; }

        [JsonProperty("response")]
        public Response Response { get; set; }
    }

    public class Response
    {
        [JsonProperty("blog")]
        public Blog Blog { get; set; }

        [JsonProperty("posts")]
        public List<Post> Posts { get; private set; } = new List<Post>();

        [JsonProperty("total_posts")]
        public int TotalPosts { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }
}
