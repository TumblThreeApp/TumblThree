using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class TumblrCommunityBlogFiles : Files
    {
        public TumblrCommunityBlogFiles(string name, string location)
            : base(name, location)
        {
            BlogType = Models.BlogTypes.tumblrcommunity;
        }
    }
}
