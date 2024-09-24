namespace TumblThree.Domain.Models.Blogs
{
    /// <summary>
    /// Helper class for DetailsAll view
    /// </summary>
    /// <remarks>
    /// Maybe not the best solution, but it's only needed for the DetailsAll view.
    /// </remarks>
    public class BlogAll : Blog, IBlogAll
    {
        private bool? downloadPhoto;
        private bool? downloadQuote;
        private bool? downloadText;
        private bool? downloadAnswer;
        private bool? downloadVideo;
        private bool? createPhotoMeta;
        private bool? createVideoMeta;
        private bool? createAudioMeta;
        private bool? downloadRebloggedPosts;
        private bool? downloadVideoThumbnail;
        private bool? skipGif;
        private bool? groupPhotoSets;
        private bool? forceSize;
        private bool? forceRescan;
        private bool? checkDirectoryForFiles;
        private bool? downloadUrlList;
        private bool? downloadImgur;
        private bool? downloadGfycat;
        private bool? downloadWebmshare;
        private bool? downloadUguu;
        private bool? downloadCatBox;
        private bool? dumpCrawlerData;
        private bool? regExPhotos;
        private bool? regExVideos;
        private bool? downloadAudio;
        private bool? downloadConversation;
        private bool? downloadLink;
        private bool? downloadReplies;
        private bool? saveTextsIndividualFiles;

        private bool downloadPhotoDiff;
        private bool downloadQuoteDiff;
        private bool downloadTextDiff;
        private bool downloadAnswerDiff;
        private bool downloadVideoDiff;
        private bool createPhotoMetaDiff;
        private bool createVideoMetaDiff;
        private bool createAudioMetaDiff;
        private bool downloadRebloggedPostsDiff;
        private bool downloadVideoThumbnailDiff;
        private bool skipGifDiff;
        private bool groupPhotoSetsDiff;
        private bool forceSizeDiff;
        private bool forceRescanDiff;
        private bool checkDirectoryForFilesDiff;
        private bool downloadUrlListDiff;
        private bool downloadImgurDiff;
        private bool downloadGfycatDiff;
        private bool downloadWebmshareDiff;
        private bool downloadUguuDiff;
        private bool downloadCatBoxDiff;
        private bool dumpCrawlerDataDiff;
        private bool regExPhotosDiff;
        private bool regExVideosDiff;
        private bool downloadAudioDiff;
        private bool downloadConversationDiff;
        private bool downloadLinkDiff;
        private bool downloadRepliesDiff;
        private bool saveTextsIndividualFilesDiff;

        private bool downloadPagesEnabled;
        private bool pageSizeEnabled;
        private bool downloadFromEnabled;
        private bool downloadToEnabled;
        private bool tagsEnabled;
        private bool passwordEnabled;
        private bool fileDownloadLocationEnabled;
        private bool filenameTemplateEnabled;
        private bool metadataFormatEnabled;
        private bool blogTypeEnabled;
        private bool collectionIdEnabled;
        private bool selectionContainsTumblrBlogs;
        private bool selectionContainsTwitterBlogs;

        public new bool? DownloadPhoto
        {
            get { return downloadPhoto; }
            set
            {
                DownloadPhotoDiff = !value.HasValue;
                SetProperty(ref downloadPhoto, value);
            }
        }

        public bool DownloadPhotoDiff
        {
            get { return downloadPhotoDiff; }
            set
            {
                SetProperty(ref downloadPhotoDiff, value);
            }
        }

        public new bool? DownloadQuote
        {
            get { return downloadQuote; }
            set
            {
                DownloadQuoteDiff = !value.HasValue;
                SetProperty(ref downloadQuote, value);
            }
        }

        public bool DownloadQuoteDiff
        {
            get { return downloadQuoteDiff; }
            set
            {
                SetProperty(ref downloadQuoteDiff, value);
            }
        }

        public new bool? DownloadText
        {
            get { return downloadText; }
            set
            {
                DownloadTextDiff = !value.HasValue;
                SetProperty(ref downloadText, value);
            }
        }

        public bool DownloadTextDiff
        {
            get { return downloadTextDiff; }
            set
            {
                SetProperty(ref downloadTextDiff, value);
            }
        }

        public new bool? DownloadAnswer
        {
            get { return downloadAnswer; }
            set
            {
                DownloadAnswerDiff = !value.HasValue;
                SetProperty(ref downloadAnswer, value);
            }
        }

        public bool DownloadAnswerDiff
        {
            get { return downloadAnswerDiff; }
            set
            {
                SetProperty(ref downloadAnswerDiff, value);
            }
        }

        public new bool? DownloadVideo
        {
            get { return downloadVideo; }
            set
            {
                DownloadVideoDiff = !value.HasValue;
                SetProperty(ref downloadVideo, value);
            }
        }

        public bool DownloadVideoDiff
        {
            get { return downloadVideoDiff; }
            set
            {
                SetProperty(ref downloadVideoDiff, value);
            }
        }

        public new bool? CreatePhotoMeta
        {
            get { return createPhotoMeta; }
            set
            {
                CreatePhotoMetaDiff = !value.HasValue;
                SetProperty(ref createPhotoMeta, value);
            }
        }

        public bool CreatePhotoMetaDiff
        {
            get { return createPhotoMetaDiff; }
            set
            {
                SetProperty(ref createPhotoMetaDiff, value);
            }
        }

        public new bool? CreateVideoMeta
        {
            get { return createVideoMeta; }
            set
            {
                CreateVideoMetaDiff = !value.HasValue;
                SetProperty(ref createVideoMeta, value);
            }
        }

        public bool CreateVideoMetaDiff
        {
            get { return createVideoMetaDiff; }
            set
            {
                SetProperty(ref createVideoMetaDiff, value);
            }
        }

        public new bool? CreateAudioMeta
        {
            get { return createAudioMeta; }
            set
            {
                CreateAudioMetaDiff = !value.HasValue;
                SetProperty(ref createAudioMeta, value);
            }
        }

        public bool CreateAudioMetaDiff
        {
            get { return createAudioMetaDiff; }
            set
            {
                SetProperty(ref createAudioMetaDiff, value);
            }
        }

        public new bool? DownloadRebloggedPosts
        {
            get { return downloadRebloggedPosts; }
            set
            {
                DownloadRebloggedPostsDiff = !value.HasValue;
                SetProperty(ref downloadRebloggedPosts, value);
            }
        }

        public bool DownloadRebloggedPostsDiff
        {
            get { return downloadRebloggedPostsDiff; }
            set
            {
                SetProperty(ref downloadRebloggedPostsDiff, value);
            }
        }

        public new bool? DownloadVideoThumbnail
        {
            get { return downloadVideoThumbnail; }
            set
            {
                DownloadVideoThumbnailDiff = !value.HasValue;
                SetProperty(ref downloadVideoThumbnail, value);
            }
        }

        public bool DownloadVideoThumbnailDiff
        {
            get { return downloadVideoThumbnailDiff; }
            set
            {
                SetProperty(ref downloadVideoThumbnailDiff, value);
            }
        }

        public new bool? SkipGif
        {
            get { return skipGif; }
            set
            {
                SkipGifDiff = !value.HasValue;
                SetProperty(ref skipGif, value);
            }
        }

        public bool SkipGifDiff
        {
            get { return skipGifDiff; }
            set
            {
                SetProperty(ref skipGifDiff, value);
            }
        }

        public new bool? GroupPhotoSets
        {
            get { return groupPhotoSets; }
            set
            {
                GroupPhotoSetsDiff = !value.HasValue;
                SetProperty(ref groupPhotoSets, value);
            }
        }

        public bool GroupPhotoSetsDiff
        {
            get { return groupPhotoSetsDiff; }
            set
            {
                SetProperty(ref groupPhotoSetsDiff, value);
            }
        }

        public new bool? ForceSize
        {
            get { return forceSize; }
            set
            {
                ForceSizeDiff = !value.HasValue;
                SetProperty(ref forceSize, value);
            }
        }

        public bool ForceSizeDiff
        {
            get { return forceSizeDiff; }
            set
            {
                SetProperty(ref forceSizeDiff, value);
            }
        }

        public new bool? ForceRescan
        {
            get { return forceRescan; }
            set
            {
                ForceRescanDiff = !value.HasValue;
                SetProperty(ref forceRescan, value);
            }
        }

        public bool ForceRescanDiff
        {
            get { return forceRescanDiff; }
            set
            {
                SetProperty(ref forceRescanDiff, value);
            }
        }

        public new bool? CheckDirectoryForFiles
        {
            get { return checkDirectoryForFiles; }
            set
            {
                CheckDirectoryForFilesDiff = !value.HasValue;
                SetProperty(ref checkDirectoryForFiles, value);
            }
        }

        public bool CheckDirectoryForFilesDiff
        {
            get { return checkDirectoryForFilesDiff; }
            set
            {
                SetProperty(ref checkDirectoryForFilesDiff, value);
            }
        }

        public new bool? DownloadUrlList
        {
            get { return downloadUrlList; }
            set
            {
                DownloadUrlListDiff = !value.HasValue;
                SetProperty(ref downloadUrlList, value);
            }
        }

        public bool DownloadUrlListDiff
        {
            get { return downloadUrlListDiff; }
            set
            {
                SetProperty(ref downloadUrlListDiff, value);
            }
        }

        public new bool? DownloadImgur
        {
            get { return downloadImgur; }
            set
            {
                DownloadImgurDiff = !value.HasValue;
                SetProperty(ref downloadImgur, value);
            }
        }

        public bool DownloadImgurDiff
        {
            get { return downloadImgurDiff; }
            set
            {
                SetProperty(ref downloadImgurDiff, value);
            }
        }

        public new bool? DownloadGfycat
        {
            get { return downloadGfycat; }
            set
            {
                DownloadGfycatDiff = !value.HasValue;
                SetProperty(ref downloadGfycat, value);
            }
        }

        public bool DownloadGfycatDiff
        {
            get { return downloadGfycatDiff; }
            set
            {
                SetProperty(ref downloadGfycatDiff, value);
            }
        }

        public new bool? DownloadWebmshare
        {
            get { return downloadWebmshare; }
            set
            {
                DownloadWebmshareDiff = !value.HasValue;
                SetProperty(ref downloadWebmshare, value);
            }
        }

        public bool DownloadWebmshareDiff
        {
            get { return downloadWebmshareDiff; }
            set
            {
                SetProperty(ref downloadWebmshareDiff, value);
            }
        }

        public new bool? DownloadUguu
        {
            get { return downloadUguu; }
            set
            {
                DownloadUguuDiff = !value.HasValue;
                SetProperty(ref downloadUguu, value);
            }
        }

        public bool DownloadUguuDiff
        {
            get { return downloadUguuDiff; }
            set
            {
                SetProperty(ref downloadUguuDiff, value);
            }
        }

        public new bool? DownloadCatBox
        {
            get { return downloadCatBox; }
            set
            {
                DownloadCatBoxDiff = !value.HasValue;
                SetProperty(ref downloadCatBox, value);
            }
        }

        public bool DownloadCatBoxDiff
        {
            get { return downloadCatBoxDiff; }
            set
            {
                SetProperty(ref downloadCatBoxDiff, value);
            }
        }

        public new bool? DumpCrawlerData
        {
            get { return dumpCrawlerData; }
            set
            {
                DumpCrawlerDataDiff = !value.HasValue;
                SetProperty(ref dumpCrawlerData, value);
            }
        }

        public bool DumpCrawlerDataDiff
        {
            get { return dumpCrawlerDataDiff; }
            set
            {
                SetProperty(ref dumpCrawlerDataDiff, value);
            }
        }

        public new bool? RegExPhotos
        {
            get { return regExPhotos; }
            set
            {
                RegExPhotosDiff = !value.HasValue;
                SetProperty(ref regExPhotos, value);
            }
        }

        public bool RegExPhotosDiff
        {
            get { return regExPhotosDiff; }
            set
            {
                SetProperty(ref regExPhotosDiff, value);
            }
        }

        public new bool? RegExVideos
        {
            get { return regExVideos; }
            set
            {
                RegExVideosDiff = !value.HasValue;
                SetProperty(ref regExVideos, value);
            }
        }

        public bool RegExVideosDiff
        {
            get { return regExVideosDiff; }
            set
            {
                SetProperty(ref regExVideosDiff, value);
            }
        }

        public new bool? DownloadAudio
        {
            get { return downloadAudio; }
            set
            {
                DownloadAudioDiff = !value.HasValue;
                SetProperty(ref downloadAudio, value);
            }
        }

        public bool DownloadAudioDiff
        {
            get { return downloadAudioDiff; }
            set
            {
                SetProperty(ref downloadAudioDiff, value);
            }
        }

        public new bool? DownloadConversation
        {
            get { return downloadConversation; }
            set
            {
                DownloadConversationDiff = !value.HasValue;
                SetProperty(ref downloadConversation, value);
            }
        }

        public bool DownloadConversationDiff
        {
            get { return downloadConversationDiff; }
            set
            {
                SetProperty(ref downloadConversationDiff, value);
            }
        }

        public new bool? DownloadLink
        {
            get { return downloadLink; }
            set
            {
                DownloadLinkDiff = !value.HasValue;
                SetProperty(ref downloadLink, value);
            }
        }

        public bool DownloadLinkDiff
        {
            get { return downloadLinkDiff; }
            set
            {
                SetProperty(ref downloadLinkDiff, value);
            }
        }

        public new bool? DownloadReplies
        {
            get { return downloadReplies; }
            set
            {
                DownloadRepliesDiff = !value.HasValue;
                SetProperty(ref downloadReplies, value);
            }
        }

        public bool DownloadRepliesDiff
        {
            get { return downloadRepliesDiff; }
            set
            {
                SetProperty(ref downloadRepliesDiff, value);
            }
        }

        public new bool? SaveTextsIndividualFiles
        {
            get => saveTextsIndividualFiles;
            set
            {
                SaveTextsIndividualFilesDiff = !value.HasValue;
                SetProperty(ref saveTextsIndividualFiles, value);
            }
        }

        public bool SaveTextsIndividualFilesDiff
        {
            get => saveTextsIndividualFilesDiff;
            set => SetProperty(ref saveTextsIndividualFilesDiff, value);
        }

        public bool DownloadPagesEnabled
        {
            get { return downloadPagesEnabled; }
            set
            {
                SetProperty(ref downloadPagesEnabled, value);
            }
        }

        public bool PageSizeEnabled
        {
            get { return pageSizeEnabled; }
            set
            {
                SetProperty(ref pageSizeEnabled, value);
            }
        }

        public bool DownloadFromEnabled
        {
            get { return downloadFromEnabled; }
            set
            {
                SetProperty(ref downloadFromEnabled, value);
            }
        }

        public bool DownloadToEnabled
        {
            get { return downloadToEnabled; }
            set
            {
                SetProperty(ref downloadToEnabled, value);
            }
        }

        public bool TagsEnabled
        {
            get { return tagsEnabled; }
            set
            {
                SetProperty(ref tagsEnabled, value);
            }
        }

        public bool PasswordEnabled
        {
            get { return passwordEnabled; }
            set
            {
                SetProperty(ref passwordEnabled, value);
            }
        }

        public bool FileDownloadLocationEnabled
        {
            get { return fileDownloadLocationEnabled; }
            set
            {
                SetProperty(ref fileDownloadLocationEnabled, value);
            }
        }

        public bool FilenameTemplateEnabled
        {
            get { return filenameTemplateEnabled; }
            set
            {
                SetProperty(ref filenameTemplateEnabled, value);
            }
        }

        public bool MetadataFormatEnabled
        {
            get { return metadataFormatEnabled; }
            set
            {
                SetProperty(ref metadataFormatEnabled, value);
            }
        }

        public bool BlogTypeEnabled
        {
            get { return blogTypeEnabled; }
            set
            {
                SetProperty(ref blogTypeEnabled, value);
            }
        }

        public bool CollectionIdEnabled
        {
            get { return collectionIdEnabled; }
            set
            {
                SetProperty(ref collectionIdEnabled, value);
            }
        }

        public bool SelectionContainsTumblrBlogs
        {
            get { return selectionContainsTumblrBlogs; }
            set
            {
                SetProperty(ref selectionContainsTumblrBlogs, value);
            }
        }

        public bool SelectionContainsTwitterBlogs
        {
            get { return selectionContainsTwitterBlogs; }
            set
            {
                SetProperty(ref selectionContainsTwitterBlogs, value);
            }
        }
    }
}
