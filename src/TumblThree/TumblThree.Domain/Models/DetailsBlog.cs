using System.Runtime.Serialization;

using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Domain.Models
{
    [DataContract]
    public class DetailsBlog : Blog
    {
        private bool? _checkDirectoryForFiles;
        private bool? _createAudioMeta;
        private bool? _createPhotoMeta;
        private bool? _createVideoMeta;
        private bool? _downloadAudio;
        private bool? _downloadConversation;
        private bool? _downloadLink;
        private bool? _downloadPhoto;
        private bool? _downloadQuote;
        private bool? _downloadText;
        private bool? _downloadAnswer;
        private bool? _downloadUrlList;
        private bool? _downloadVideo;
        private bool? _forceRescan;
        private bool? _forceSize;
        private bool? _skipGif;
        private bool? _downloadRebloggedPosts;
        private bool? _online;

        [DataMember]
        public new bool? DownloadText
        {
            get => _downloadText;
            set
            {
                SetProperty(ref _downloadText, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadQuote
        {
            get => _downloadQuote;
            set
            {
                SetProperty(ref _downloadQuote, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadPhoto
        {
            get => _downloadPhoto;
            set
            {
                SetProperty(ref _downloadPhoto, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadLink
        {
            get => _downloadLink;
            set
            {
                SetProperty(ref _downloadLink, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadAnswer
        {
            get => _downloadAnswer;
            set
            {
                SetProperty(ref _downloadAnswer, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadConversation
        {
            get => _downloadConversation;
            set
            {
                SetProperty(ref _downloadConversation, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadVideo
        {
            get => _downloadVideo;
            set
            {
                SetProperty(ref _downloadVideo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadAudio
        {
            get => _downloadAudio;
            set
            {
                SetProperty(ref _downloadAudio, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreatePhotoMeta
        {
            get => _createPhotoMeta;
            set
            {
                SetProperty(ref _createPhotoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreateVideoMeta
        {
            get => _createVideoMeta;
            set
            {
                SetProperty(ref _createVideoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? CreateAudioMeta
        {
            get => _createAudioMeta;
            set
            {
                SetProperty(ref _createAudioMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadRebloggedPosts
        {
            get => _downloadRebloggedPosts;
            set
            {
                SetProperty(ref _downloadRebloggedPosts, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? Online
        {
            get => _online;
            set => SetProperty(ref _online, value);
        }

        [DataMember]
        public new bool? CheckDirectoryForFiles
        {
            get => _checkDirectoryForFiles;
            set
            {
                SetProperty(ref _checkDirectoryForFiles, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? DownloadUrlList
        {
            get => _downloadUrlList;
            set
            {
                SetProperty(ref _downloadUrlList, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? Dirty { get; set; }

        [DataMember]
        public new bool? SkipGif
        {
            get => _skipGif;
            set
            {
                SetProperty(ref _skipGif, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? ForceSize
        {
            get => _forceSize;
            set
            {
                SetProperty(ref _forceSize, value);
                Dirty = true;
            }
        }

        [DataMember]
        public new bool? ForceRescan
        {
            get => _forceRescan;
            set
            {
                SetProperty(ref _forceRescan, value);
                Dirty = true;
            }
        }
    }
}
