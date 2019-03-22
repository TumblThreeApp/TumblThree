namespace TumblThree.Applications.DataModels.TumblrCrawlerData
{
    public class TumblrCrawlerData<T>
    {
        public T Data { get; protected set; }

        public string Filename { get; protected set; }

        public TumblrCrawlerData(string filename, T data)
        {
            Filename = filename;
            Data = data;
        }
    }
}
