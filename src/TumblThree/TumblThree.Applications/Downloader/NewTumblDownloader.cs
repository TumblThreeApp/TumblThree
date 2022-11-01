using System;
using System.Threading;
using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrPosts;
using TumblThree.Applications.Services;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Downloader
{
    public class NewTumblDownloader : AbstractDownloader
    {
        public NewTumblDownloader(IShellService shellService, IManagerService managerService, CancellationToken ct, PauseToken pt, IProgress<DownloadProgress> progress,
            IPostQueue<AbstractPost> postQueue, FileDownloader fileDownloader, ICrawlerService crawlerService = null, IBlog blog = null, IFiles files = null)
            : base(shellService, managerService, ct, pt, progress, postQueue, fileDownloader, crawlerService, blog, files)
        {
        }

        protected new string AddFileToDb(TumblrPost downloadItem)
        {
            // url filenames are unique and can't identify duplicates, so use mediaIx for now

            if (AppendTemplate == null)
            {
                files.AddFileToDb(FileNameUrl(downloadItem), "", downloadItem.Filename);
                return downloadItem.Filename;
            }
            return files.AddFileToDb(FileNameUrl(downloadItem), "", downloadItem.Filename, AppendTemplate);
        }

        protected new bool CheckIfFileExistsInDB(TumblrPost downloadItem)
        {
            string filename = FileNameUrl(downloadItem);
            if (shellService.Settings.LoadAllDatabases)
            {
                return managerService.CheckIfFileExistsInDB(filename, false, shellService.Settings.LoadArchive);
            }

            return files.CheckIfFileExistsInDB(filename, false);
        }

        protected override string FileNameUrl(TumblrPost downloadItem)
        {
            return "m" + downloadItem.Id;
        }
    }
}
