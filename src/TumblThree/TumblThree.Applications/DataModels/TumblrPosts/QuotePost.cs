using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class QuotePost : TumblrPost
    {
        public QuotePost(string url, string id, string date, string filename)
            : base(url, null, id, date, filename)
        {
            PostType = PostType.Text;
            DbType = "DownloadedQuotes";
            TextFileLocation = Resources.FileNameQuotes;
        }

        public QuotePost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
