using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Downloader
{
    public class JsonDownloader<T> : ICrawlerDataDownloader
    {
        private readonly IBlog blog;
        private readonly ICrawlerService crawlerService;
        private readonly IPostQueue<CrawlerData<T>> jsonQueue;
        private readonly IShellService shellService;
        private CancellationToken ct;
        private readonly PauseToken pt;

        public JsonDownloader(IShellService shellService, PauseToken pt, IPostQueue<CrawlerData<T>> jsonQueue,
            ICrawlerService crawlerService, IBlog blog, CancellationToken ct)
        {
            this.shellService = shellService;
            this.crawlerService = crawlerService;
            this.blog = blog;
            this.ct = ct;
            this.pt = pt;
            this.jsonQueue = jsonQueue;
        }

        public virtual async Task DownloadCrawlerDataAsync()
        {
            var trackedTasks = new List<Task>();
            blog.CreateDataFolder();

            try
            {
                while (await jsonQueue.OutputAvailableAsync(ct))
                {
                    CrawlerData<T> downloadItem = await jsonQueue.ReceiveAsync();

                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    if (pt.IsPaused)
                    {
                        pt.WaitWhilePausedWithResponseAsyc().Wait();
                    }

                    trackedTasks.Add(DownloadPostAsync(downloadItem));
                }
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            await Task.WhenAll(trackedTasks);
        }

        public void ChangeCancellationToken(CancellationToken ct)
        {
            this.ct = ct;
        }

        private async Task DownloadPostAsync(CrawlerData<T> downloadItem)
        {
            try
            {
                await DownloadTextPostAsync(downloadItem);
            }
            catch
            {
            }
        }

        private async Task DownloadTextPostAsync(CrawlerData<T> crawlerData)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            string fileLocation = FileLocation(blogDownloadLocation, crawlerData.Filename);
            await AppendToTextFileAsync(fileLocation, crawlerData.Data);
        }

        private async Task AppendToTextFileAsync(string fileLocation, T data)
        {
            try
            {
                if (typeof(T) == typeof(DataModels.TumblrSearchJson.Datum) || typeof(T) == typeof(DataModels.Twitter.TimelineTweets.Tweet))
                {
                    var serializer = new JsonSerializer();
                    using (StreamWriter sw = new StreamWriter(fileLocation, false))
                    using (JsonWriter writer = new JsonTextWriter(sw) { Formatting = Newtonsoft.Json.Formatting.Indented })
                    {
                        serializer.Serialize(writer, data);
                    }
                }
                else
                {
                    using (var stream = new FileStream(fileLocation, FileMode.Create, FileAccess.Write))
                    {
                        using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                            stream, Encoding.UTF8, true, true, "  "))
                        {
                            var serializer = new DataContractJsonSerializer(data.GetType());
                            serializer.WriteObject(writer, data);
                            writer.Flush();
                        }
                    }
                }
                await Task.CompletedTask;
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("TumblrJsonDownloader:AppendToTextFile: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
            }
            catch
            {
            }
        }

        private static string FileLocation(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, fileName);
        }
    }
}
