using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AudioPost : TumblrPost
    {
        public AudioPost(string url, string id, string date, string filename)
            : base(url, id, date, filename)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedAudios";
            TextFileLocation = Resources.FileNameAudios;
        }

        public AudioPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
