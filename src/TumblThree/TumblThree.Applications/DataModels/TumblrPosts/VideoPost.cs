using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class VideoPost : TumblrPost
    {
        public VideoPost(string url, string id, string date)
            : base(url, id, date, null)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedVideos";
            TextFileLocation = Resources.FileNameVideos;
        }

        public VideoPost(string url, string id)
            : this(url, id, string.Empty)
        {
        }
    }
}
