using System.Runtime.Serialization;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class NewTumblBlogFiles : Files
    {
        public NewTumblBlogFiles(string name, string location)
            : base(name, location)
        {
            BlogType = BlogTypes.newtumbl;
        }
    }
}
