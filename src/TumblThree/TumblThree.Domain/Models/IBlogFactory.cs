using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Models
{
    public interface IBlogFactory
    {
        bool IsValidBlogUrl(string blogUrl);

        bool IsValidUrl(string blogUrl);

        IBlog GetBlog(string blogUrl, string path, string filenameTemplate);
    }
}
