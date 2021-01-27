using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ExternalPhotoPost : TumblrPost
    {
        public ExternalPhotoPost(string url, string id, string date, string filename)
            : base(url, id, date, filename)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedPhotos";
            TextFileLocation = Resources.FileNamePhotos;
        }

        public ExternalPhotoPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
