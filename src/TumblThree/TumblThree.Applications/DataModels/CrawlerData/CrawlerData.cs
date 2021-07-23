namespace TumblThree.Applications.DataModels.CrawlerData
{
    public class CrawlerData<T>
    {
        public T Data { get; protected set; }

        public string Filename { get; protected set; }

        public CrawlerData(string filename, T data)
        {
            Filename = filename;
            Data = data;
        }
    }
}
