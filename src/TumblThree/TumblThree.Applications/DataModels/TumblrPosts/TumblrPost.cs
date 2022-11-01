namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public abstract class TumblrPost : AbstractPost
    {
        protected TumblrPost(string url, string postedUrl, string id, string date, string filename)
            : base(url, postedUrl, id, date, filename)
        {
        }

        public TumblrPost CloneWithAdjustedUrl(string newUrl)
        {
            var obj = (TumblrPost)MemberwiseClone();
            obj.Url = newUrl;
            return obj;
        }
    }
}
