using System.ComponentModel.Composition;

using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Services
{
    [Export(typeof(ISettingsService))]
    public class SettingsService : ISettingsService
    {
        private readonly IShellService shellService;

        [ImportingConstructor]
        public SettingsService(IShellService shellService) => this.shellService = shellService;

        public IBlog TransferGlobalSettingsToBlog(IBlog blog)
        {
            blog.DownloadAudio = shellService.Settings.DownloadAudios;
            blog.DownloadPhoto = shellService.Settings.DownloadImages;
            blog.DownloadVideo = shellService.Settings.DownloadVideos;
            blog.DownloadText = shellService.Settings.DownloadTexts;
            blog.DownloadAnswer = shellService.Settings.DownloadAnswers;
            blog.DownloadQuote = shellService.Settings.DownloadQuotes;
            blog.DownloadConversation = shellService.Settings.DownloadConversations;
            blog.DownloadLink = shellService.Settings.DownloadLinks;
            blog.CreatePhotoMeta = shellService.Settings.CreateImageMeta;
            blog.CreateVideoMeta = shellService.Settings.CreateVideoMeta;
            blog.CreateAudioMeta = shellService.Settings.CreateAudioMeta;
            blog.DownloadReplies = shellService.Settings.DownloadReplies;
            blog.MetadataFormat = blog.BlogType == Domain.Models.BlogTypes.twitter ? Domain.Models.MetadataType.Json : shellService.Settings.MetadataFormat;
            blog.SkipGif = shellService.Settings.SkipGif;
            blog.DownloadVideoThumbnail = shellService.Settings.DownloadVideoThumbnails;
            blog.DownloadRebloggedPosts = shellService.Settings.DownloadRebloggedPosts;
            blog.ForceSize = shellService.Settings.ForceSize;
            blog.ForceRescan = shellService.Settings.ForceRescan;
            blog.CheckDirectoryForFiles = shellService.Settings.CheckDirectoryForFiles;
            blog.DownloadUrlList = shellService.Settings.DownloadUrlList;
            blog.DownloadPages = shellService.Settings.DownloadPages;
            blog.PageSize = blog.BlogType == Domain.Models.BlogTypes.twitter ? 20 : shellService.Settings.PageSize;
            blog.DownloadFrom = shellService.Settings.DownloadFrom;
            blog.DownloadTo = shellService.Settings.DownloadTo;
            blog.Tags = shellService.Settings.Tags;
            blog.DownloadImgur = shellService.Settings.DownloadImgur;
            blog.DownloadWebmshare = shellService.Settings.DownloadWebmshare;
            blog.DownloadUguu = shellService.Settings.DownloadUguu;
            blog.DownloadCatBox = shellService.Settings.DownloadCatBox;
            blog.WebmshareType = shellService.Settings.WebmshareType;
            blog.UguuType = shellService.Settings.UguuType;
            blog.CatBoxType = shellService.Settings.CatBoxType;
            blog.DumpCrawlerData = shellService.Settings.DumpCrawlerData;
            blog.RegExPhotos = shellService.Settings.RegExPhotos;
            blog.RegExVideos = shellService.Settings.RegExVideos;
            blog.GroupPhotoSets = shellService.Settings.GroupPhotoSets;
            blog.FilenameTemplate = shellService.Settings.FilenameTemplate;
            blog.CollectionId = shellService.Settings.ActiveCollectionId;
            blog.PnjDownloadFormat = shellService.Settings.PnjDownloadFormat;
            blog.SaveTextsIndividualFiles = shellService.Settings.SaveTextsIndividualFiles;
            blog.ZipCrawlerData = shellService.Settings.ZipCrawlerData;
            return blog;
        }
    }
}
