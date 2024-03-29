﻿using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ExternalVideoPost : TumblrPost
    {
        public ExternalVideoPost(string url, string id, string date)
            : base(url, null, id, date, null)
        {
            PostType = PostType.Binary;
            DbType = "DownloadedVideos";
            TextFileLocation = Resources.FileNameVideos;
        }

        public ExternalVideoPost(string url, string id)
            : this(url, id, string.Empty)
        {
        }
    }
}
