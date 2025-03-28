using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class BlueskyBlogFiles : Files
    {
        public BlueskyBlogFiles(string name, string location)
            : base(name, location)
        {
            BlogType = BlogTypes.bluesky;
        }
    }
}
