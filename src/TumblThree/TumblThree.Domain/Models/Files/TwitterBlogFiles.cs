using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class TwitterBlogFiles : Files
    {
        public TwitterBlogFiles(string name, string location)
            : base(name, location)
        {
            BlogType = BlogTypes.twitter;
        }
    }
}
