using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrCrawlerData;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Downloader
{
    public sealed class TumblrXmlDownloader : ICrawlerDataDownloader
    {
        private readonly IBlog _blog;
        private readonly ICrawlerService _crawlerService;
        private readonly IPostQueue<TumblrCrawlerData<XDocument>> _xmlQueue;
        private readonly IShellService _shellService;
        private readonly CancellationToken _ct;
        private readonly PauseToken _pt;

        public TumblrXmlDownloader(IShellService shellService, PauseToken pt, IPostQueue<TumblrCrawlerData<XDocument>> xmlQueue, ICrawlerService crawlerService, IBlog blog, CancellationToken ct)
        {
            _shellService = shellService;
            _crawlerService = crawlerService;
            _blog = blog;
            _ct = ct;
            _pt = pt;
            _xmlQueue = xmlQueue;
        }

        public async Task DownloadCrawlerDataAsync()
        {
            var trackedTasks = new List<Task>();
            _blog.CreateDataFolder();

            try
            {
                foreach (TumblrCrawlerData<XDocument> downloadItem in _xmlQueue.GetConsumingEnumerable(_ct))
                {
                    if (_ct.IsCancellationRequested)
                    {
                        break;
                    }

                    if (_pt.IsPaused)
                    {
                        _pt.WaitWhilePausedWithResponseAsyc().Wait();
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

        private async Task DownloadPostAsync(TumblrCrawlerData<XDocument> downloadItem)
        {
            try
            {
                await DownloadTextPostAsync(downloadItem);
            }
            catch
            {
            }
        }

        private async Task DownloadTextPostAsync(TumblrCrawlerData<XDocument> crawlerData)
        {
            string blogDownloadLocation = _blog.DownloadLocation();
            string fileLocation = FileLocation(blogDownloadLocation, crawlerData.Filename);
            await AppendToTextFileAsync(fileLocation, crawlerData.Data);
        }

        private async Task AppendToTextFileAsync(string fileLocation, XContainer data)
        {
            try
            {
                using (var sw = new StreamWriter(fileLocation, true))
                {
                    await sw.WriteAsync(PrettyXml(data));
                }
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("TumblrXmlDownloader:AppendToTextFile: {0}", ex);
                _shellService.ShowError(ex, Resources.DiskFull);
                _crawlerService.StopCommand.Execute(null);
            }
            catch
            {
            }
        }

        private static string PrettyXml(XContainer xml)
        {
            var stringBuilder = new StringBuilder();

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = true
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                xml.WriteTo(xmlWriter);
            }

            return stringBuilder.ToString();
        }

        private static string FileLocation(string blogDownloadLocation, string fileName)
        {
            return Path.Combine(blogDownloadLocation, fileName);
        }
    }
}
