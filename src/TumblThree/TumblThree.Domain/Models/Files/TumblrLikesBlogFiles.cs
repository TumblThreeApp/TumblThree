using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class TumblrLikesBlogFiles : Files
    {
        public TumblrLikesBlogFiles(string name, string location)
            : base(name, location)
        {
            BlogType = BlogTypes.tl;
        }
    }
}
