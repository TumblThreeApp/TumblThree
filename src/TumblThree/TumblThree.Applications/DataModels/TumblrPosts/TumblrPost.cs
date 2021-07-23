namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public abstract class TumblrPost : AbstractPost
    {
        protected TumblrPost(string url, string id, string date, string filename)
            : base(url, id, date, filename)
        {
        }
    }
}
