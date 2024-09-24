using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class TextPost : TumblrPost
    {
        public TextPost(string url, string id, string date, string filename)
            : base(url, null, id, date, filename)
        {
            PostType = PostType.Text;
            DbType = "DownloadedTexts";
            TextFileLocation = Resources.FileNameTexts;
        }

        public TextPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
