﻿using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class QuotePost : TumblrPost
    {
        public QuotePost(string url, string id, string date)
            : base(url, null, id, date, null)
        {
            PostType = PostType.Text;
            DbType = "DownloadedQuotes";
            TextFileLocation = Resources.FileNameQuotes;
        }

        public QuotePost(string url, string id)
            : this(url, id, string.Empty)
        {
        }
    }
}
