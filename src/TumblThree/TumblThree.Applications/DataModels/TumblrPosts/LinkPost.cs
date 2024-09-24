using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class LinkPost : TumblrPost
    {
        public LinkPost(string url, string id, string date, string filename)
            : base(url, null, id, date, filename)
        {
            PostType = PostType.Text;
            DbType = "DownloadedLinks";
            TextFileLocation = Resources.FileNameLinks;
        }

        public LinkPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
