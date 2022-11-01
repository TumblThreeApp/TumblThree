namespace TumblThree.Applications.DataModels.Twitter
{
    public class TwitterPost : AbstractPost
    {
        public TwitterPost(string url, string id, string date, string filename)
            : base(url, null, id, date, filename)
        {
        }
    }
}
