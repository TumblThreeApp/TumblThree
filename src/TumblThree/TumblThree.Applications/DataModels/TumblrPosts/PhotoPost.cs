using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class PhotoPost : TumblrPost
    {
        public PhotoPost(string url, string id, int index, string date)
            : base(url, id, index, date)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedPhotos";
            TextFileLocation = Resources.FileNamePhotos;
        }

        public PhotoPost(string url, string id, int index)
            : this(url, id, index, string.Empty)
        {
        }
    }
}
