using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class VideoPost : TumblrPost
    {
        public VideoPost(string url, string postedUrl, string id, string date, string filename)
            : base(url, postedUrl, id, date, filename)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedVideos";
            TextFileLocation = Resources.FileNameVideos;
        }

        public VideoPost(string url, string postedUrl, string id, string filename)
            : this(url, postedUrl, id, string.Empty, filename)
        {
        }
    }
}
