using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    public class JsonDownloader<T> : ICrawlerDataDownloader, IDisposable
    {
        private readonly IBlog blog;
        private readonly ICrawlerService crawlerService;
        private readonly IPostQueue<CrawlerData<T>> jsonQueue;
        private readonly IShellService shellService;
        private CancellationToken ct;
        private readonly PauseToken pt;
        private readonly IList<string> existingCrawlerData = new List<string>();
        private readonly SemaphoreSlim existingCrawlerDataLock = new SemaphoreSlim(1);
        private ZipStorer archive;
        private bool disposed;

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
                        pt.WaitWhilePausedWithResponseAsync().Wait();
                    }

                    trackedTasks.Add(DownloadPostAsync(downloadItem));
                }
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            await Task.WhenAll(trackedTasks);

            CloseArchive();
        }

        public async Task GetAlreadyExistingCrawlerDataFilesAsync(IProgress<DownloadProgress> progress)
        {
            await existingCrawlerDataLock.WaitAsync();
            try
            {
                if (blog.ZipCrawlerData)
                {
                    var zipPath = Path.Combine(blog.DownloadLocation(), "CrawlerData.zip");
                    List<ZipStorer.ZipFileEntry> archiveEntries;
                    if (shellService.Settings.ZipExistingCrawlerData &&
                        Directory.EnumerateFiles(blog.DownloadLocation(), "*.json").Any())
                    {
                        progress?.Report(new DownloadProgress { Progress = Resources.CompressExistingCrawlerDataFiles });
                        var zipExists = File.Exists(zipPath);
                        var fs = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1048576, FileOptions.SequentialScan);
                        archive = zipExists ? ZipStorer.Open(fs, FileAccess.ReadWrite) : ZipStorer.Create(fs);
                        archiveEntries = archive.ReadCentralDir();
                        foreach (var filepath in Directory.EnumerateFiles(blog.DownloadLocation(), "*.json"))
                        {
                            var filename = Path.GetFileName(filepath);
                            var foundEntries = archiveEntries.Where(x => x.FilenameInZip == filename).ToList();
                            foundEntries.ForEach(x => x.FilenameInZip = "<DELETE>");
                            var fi = new FileInfo(filepath);
                            using (var fileStream = fi.OpenRead())
                            {
                                await archive.AddStreamAsync(ZipStorer.Compression.Deflate, fi.Name, fileStream, fi.LastWriteTime);
                            }
                            fi.Delete();
                        }
                        List<ZipStorer.ZipFileEntry> entriesToRemove = archiveEntries.Where(x => x.FilenameInZip == "<DELETE>").ToList();
                        ZipStorer.RemoveEntries(ref archive, entriesToRemove);
                    }
                    if (!File.Exists(zipPath)) return;
                    progress?.Report(new DownloadProgress { Progress = Resources.LoadExistingCrawlerDataFiles });
                    if (archive is null)
                    {
                        var fs = new FileStream(zipPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 1048576, FileOptions.SequentialScan);
                        archive = ZipStorer.Open(fs, FileAccess.ReadWrite);
                    }
                    archiveEntries = archive.ReadCentralDir();
                    foreach (var entry in archiveEntries)
                    {
                        existingCrawlerData.Add(entry.FilenameInZip);
                    }
                }
                else
                {
                    progress?.Report(new DownloadProgress { Progress = Resources.LoadExistingCrawlerDataFiles });
                    foreach (var filepath in Directory.EnumerateFiles(blog.DownloadLocation(), "*.json"))
                    {
                        existingCrawlerData.Add(Path.GetFileName(filepath));
                    }
                }
                progress?.Report(new DownloadProgress { Progress = "" });
            }
            catch (Exception ex)
            {
                Logger.Error("JsonDownloader.GetAlreadyExistingCrawlerDataFilesAsync(): {0}", ex);
            }
            finally
            {
                existingCrawlerDataLock.Release();
            }
        }

        public bool ExistingCrawlerDataContainsOrAdd(string filename)
        {
            existingCrawlerDataLock.Wait();
            try
            {
                if (existingCrawlerData.Contains(filename)) return true;
                existingCrawlerData.Add(filename);
                return false;
            }
            finally
            {
                existingCrawlerDataLock.Release();
            }
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
            catch (Exception ex)
            {
                Logger.Error("JsonDownloader.DownloadPostAsync(): {0}", ex.ToString());
            }
        }

        private async Task DownloadTextPostAsync(CrawlerData<T> crawlerData)
        {
            string blogDownloadLocation = blog.DownloadLocation();
            await WriteDataAsync(blogDownloadLocation, crawlerData.Filename, crawlerData.Data);
        }

        private async Task WriteDataAsync(string downloadLocation, string filename, T data)
        {
            var filePath = Path.Combine(downloadLocation, filename);
            try
            {
                using (var ms = new MemoryStream())
                {
                    if (typeof(T) == typeof(DataModels.TumblrSearchJson.Datum) ||
                        typeof(T) == typeof(DataModels.Twitter.TimelineTweets.Tweet) ||
                        typeof(T) == typeof(DataModels.NewTumbl.Post))
                    {
                        var serializer = new JsonSerializer();
                        using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                        using (JsonWriter writer = new JsonTextWriter(sw) { Formatting = Newtonsoft.Json.Formatting.Indented })
                        {
                            serializer.Serialize(writer, data);
                        }
                    }
                    else
                    {
                        using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                            ms, Encoding.UTF8, false, true, "  "))
                        {
                            var serializer = new DataContractJsonSerializer(data.GetType());
                            serializer.WriteObject(writer, data);
                            writer.Flush();
                        }
                    }
                    if (blog.ZipCrawlerData)
                    {
                        await existingCrawlerDataLock.WaitAsync();
                        try
                        {
                            filePath = Path.Combine(downloadLocation, "CrawlerData.zip");
                            if (archive is null)
                            {
                                var zipPath = filePath;
                                var zipExists = File.Exists(zipPath);
                                var fs = new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1048576, FileOptions.SequentialScan);
                                archive = zipExists ? ZipStorer.Open(fs, FileAccess.ReadWrite) : ZipStorer.Create(fs);
                            }
                            var entry = archive.GetEntry(filename);
                            if (entry != null) ZipStorer.RemoveEntries(ref archive, new List<ZipStorer.ZipFileEntry>() { entry });
                            ms.Position = 0;
                            archive.AddStream(ZipStorer.Compression.Deflate, filename, ms, DateTime.Now);
                        }
                        finally
                        {
                            existingCrawlerDataLock.Release();
                        }
                    }
                    else
                    {
                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            ms.WriteTo(fs);
                        }
                    }
                }
                await Task.CompletedTask;
            }
            catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x27 || (ex.HResult & 0xFFFF) == 0x70)
            {
                Logger.Error("JsonDownloader:WriteDataAsync: {0}", ex);
                shellService.ShowError(ex, Resources.DiskFull);
                crawlerService.StopCommand.Execute(null);
            }
            catch (Exception ex2)
            {
                Logger.Error("JsonDownloader:WriteDataAsync: {0}, {1}", filePath, ex2);
            }
        }

        private void CloseArchive()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    archive?.Dispose();
                    existingCrawlerDataLock?.Dispose();
                }

                disposed = true;
            }
        }
    }
}
