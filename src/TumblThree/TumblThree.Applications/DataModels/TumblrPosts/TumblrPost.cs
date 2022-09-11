namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public abstract class TumblrPost : AbstractPost
    {
        protected TumblrPost(string url, string id, string date, string filename)
            : base(url, id, date, filename)
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
