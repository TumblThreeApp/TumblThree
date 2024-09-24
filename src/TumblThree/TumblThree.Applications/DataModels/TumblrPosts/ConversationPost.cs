using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class ConversationPost : TumblrPost
    {
        public ConversationPost(string url, string id, string date, string filename)
            : base(url, null, id, date, filename)
        {
            PostType = PostType.Text;
            DbType = "DownloadedConversations";
            TextFileLocation = Resources.FileNameConversations;
        }

        public ConversationPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
