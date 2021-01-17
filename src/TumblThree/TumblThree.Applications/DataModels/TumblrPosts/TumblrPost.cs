namespace TumblThree.Applications.DataModels.TumblrPosts
{
    public enum PostType
    {
        Binary,
        Text
    }

    public abstract class TumblrPost
    {
        public PostType PostType { get; protected set; }

        public string Url { get; }

        public string Id { get; }

        public int Index { get; }

        public string Date { get; }

        public string DbType { get; protected set; }

        public string TextFileLocation { get; protected set; }

        protected TumblrPost(string url, string id, int index, string date)
        {
            Url = url;
            Id = id;
            Index = index;
            Date = date;
        }
    }
}
