using TumblThree.Applications.Properties;

namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public class AnswerPost : TumblrPost
    {
        public AnswerPost(string url, string id, string date, string filename)
            : base(url, null, id, date, filename)
        {
            PostType = PostType.Text;
            DbType = "DownloadedAnswers";
            TextFileLocation = Resources.FileNameAnswers;
        }

        public AnswerPost(string url, string id, string filename)
            : this(url, id, string.Empty, filename)
        {
        }
    }
}
