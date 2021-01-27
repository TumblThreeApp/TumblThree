using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class PhotoPost : TumblrPost
    {
        public PhotoPost(string url, string id, string date, string filename)
            : base(url, id, date, filename)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedPhotos";
            TextFileLocation = Resources.FileNamePhotos;
        }

        public PhotoPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
