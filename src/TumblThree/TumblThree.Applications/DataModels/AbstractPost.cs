namespace TumblThree.Applications.DataModels
{
    public enum PostType
    {
        Binary,
        Text
    }

    public abstract class AbstractPost
    {
        public PostType PostType { get; protected set; }

        public string Url { get; protected set; }

        public string PostedUrl { get; }

        public string Id { get; }

        public string Date { get; }

        public string Filename { get; }

        public string DbType { get; protected set; }

        public string TextFileLocation { get; protected set; }

        protected AbstractPost(string url, string postedUrl, string id, string date, string filename)
        {
            Url = url;
            PostedUrl = postedUrl;
            Id = id;
            Date = date;
            Filename = filename;
        }
    }
}
