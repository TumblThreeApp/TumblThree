namespace TumblThree.Domain.Models.Blogs
{
    public interface IBlogAll : IBlog
    {
        new bool? DownloadPhoto { get; set; }
        new bool? DownloadQuote { get; set; }
        new bool? DownloadText { get; set; }
        new bool? DownloadAnswer { get; set; }
        new bool? DownloadVideo { get; set; }
        new bool? CreatePhotoMeta { get; set; }
        new bool? CreateVideoMeta { get; set; }
        new bool? CreateAudioMeta { get; set; }
        new bool? DownloadRebloggedPosts { get; set; }
        new bool? SkipGif { get; set; }
        new bool? GroupPhotoSets { get; set; }
        new bool? ForceSize { get; set; }
        new bool? ForceRescan { get; set; }
        new bool? CheckDirectoryForFiles { get; set; }
        new bool? DownloadUrlList { get; set; }
        new bool? DownloadImgur { get; set; }
        new bool? DownloadGfycat { get; set; }
        new bool? DownloadWebmshare { get; set; }
        new bool? DownloadMixtape { get; set; }
        new bool? DownloadUguu { get; set; }
        new bool? DownloadSafeMoe { get; set; }
        new bool? DownloadLoliSafe { get; set; }
        new bool? DownloadCatBox { get; set; }
        new bool? DumpCrawlerData { get; set; }
        new bool? RegExPhotos { get; set; }
        new bool? RegExVideos { get; set; }
        new bool? DownloadAudio { get; set; }
        new bool? DownloadConversation { get; set; }
        new bool? DownloadLink { get; set; }

        bool DownloadPhotoDiff { get; set; }
        bool DownloadQuoteDiff { get; set; }
        bool DownloadTextDiff { get; set; }
        bool DownloadAnswerDiff { get; set; }
        bool DownloadVideoDiff { get; set; }
        bool CreatePhotoMetaDiff { get; set; }
        bool CreateVideoMetaDiff { get; set; }
        bool CreateAudioMetaDiff { get; set; }
        bool DownloadRebloggedPostsDiff { get; set; }
        bool SkipGifDiff { get; set; }
        bool GroupPhotoSetsDiff { get; set; }
        bool ForceSizeDiff { get; set; }
        bool ForceRescanDiff { get; set; }
        bool CheckDirectoryForFilesDiff { get; set; }
        bool DownloadUrlListDiff { get; set; }
        bool DownloadImgurDiff { get; set; }
        bool DownloadGfycatDiff { get; set; }
        bool DownloadWebmshareDiff { get; set; }
        bool DownloadMixtapeDiff { get; set; }
        bool DownloadUguuDiff { get; set; }
        bool DownloadSafeMoeDiff { get; set; }
        bool DownloadLoliSafeDiff { get; set; }
        bool DownloadCatBoxDiff { get; set; }
        bool DumpCrawlerDataDiff { get; set; }
        bool RegExPhotosDiff { get; set; }
        bool RegExVideosDiff { get; set; }
        bool DownloadAudioDiff { get; set; }
        bool DownloadConversationDiff { get; set; }
        bool DownloadLinkDiff { get; set; }

        bool DownloadPagesEnabled { get; set; }
        bool PageSizeEnabled { get; set; }
        bool DownloadFromEnabled { get; set; }
        bool DownloadToEnabled { get; set; }
        bool TagsEnabled { get; set; }
        bool PasswordEnabled { get; set; }
        bool FileDownloadLocationEnabled { get; set; }
        bool FilenameTemplateEnabled { get; set; }
        bool MetadataFormatEnabled { get; set; }
        bool BlogTypeEnabled { get; set; }
        bool CollectionIdEnabled { get; set; }
    }
}
