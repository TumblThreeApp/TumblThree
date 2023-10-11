﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Waf.Foundation;
using System.Xml;

namespace TumblThree.Domain.Models.Blogs
{
#pragma warning disable SX1309 // Field names should begin with underscore
    [DataContract]
    public class Blog : Model, IBlog
    {
        private bool checkDirectoryForFiles;
        private bool createAudioMeta;
        private bool createPhotoMeta;
        private bool createVideoMeta;
        private bool downloadReplies;
        private bool downloadAudio;
        private bool downloadConversation;
        private bool downloadLink;
        private bool downloadPhoto;
        private bool downloadQuote;
        private bool downloadText;
        private bool downloadAnswer;
        private bool downloadUrlList;
        private bool downloadVideo;
        private bool dumpCrawlerData;
        private bool regExPhotos;
        private bool regExVideos;
        private string fileDownloadLocation;
        private bool forceRescan;
        private bool forceSize;
        private string lastDownloadedPhoto;
        private string lastDownloadedVideo;
        private string notes;
        private int rating;
        private bool skipGif;
        private bool downloadVideoThumbnail;
        private string tags;
        private int duplicatePhotos;
        private int duplicateVideos;
        private int duplicateAudios;
        private int totalCount;
        private int posts;
        private int texts;
        private int answers;
        private int photos;
        private int numberOfLinks;
        private int conversations;
        private int videos;
        private int audios;
        private int photoMetas;
        private int videoMetas;
        private int audioMetas;
        private int downloadedTexts;
        private int downloadedQuotes;
        private int downloadedPhotos;
        private int downloadedLinks;
        private int downloadedAnswers;
        private int downloadedConversations;
        private int downloadedVideos;
        private int downloadedAudios;
        private int downloadedPhotoMetas;
        private int downloadedVideoMetas;
        private int downloadedAudioMetas;
        private MetadataType metadataFormat;
        private bool downloadGfycat;
        private bool downloadImgur;
        private bool downloadWebmshare;
        private bool downloadUguu;
        private bool downloadCatBox;
        private GfycatTypes gfycatType;
        private WebmshareTypes webmshareType;
        private UguuTypes uguuType;
        private CatBoxTypes catboxType;
        private string downloadPages;
        private int pageSize;
        private string downloadFrom;
        private string downloadTo;
        private string password;
        private bool downloadRebloggedPosts;
        private DateTime dateAdded;
        private DateTime lastCompleteCrawl;
        private DateTime latestPost;
        private string filenameTemplate;
        private bool online;
        private int settingsTabIndex;
        private int progress;
        private int quotes;
        private BlogTypes blogType;
        private PostType states;
        private bool groupPhotoSets;
        private int collectionId;
        private int downloadedItemsNew;
        private string pnjDownloadFormat;

        [DataMember(Name = "Links")]
        private List<string> links;

        private object lockObjectProgress = new object();
        private object lockObjectPostCount = new object();
        private object lockObjectDb = new object();
        private object lockObjectDirectory = new object();

        public enum PostType
        {
            Photo,
            Video
        }

        [DataMember]
        public PostType States
        {
            get => states;
            set => SetProperty(ref states, value);
        }

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public BlogTypes OriginalBlogType { get; set; }

        [DataMember]
        public int DuplicatePhotos
        {
            get => duplicatePhotos;
            set => SetProperty(ref duplicatePhotos, value);
        }

        [DataMember]
        public int DuplicateVideos
        {
            get => duplicateVideos;
            set => SetProperty(ref duplicateVideos, value);
        }

        [DataMember]
        public int DuplicateAudios
        {
            get => duplicateAudios;
            set => SetProperty(ref duplicateAudios, value);
        }

        [DataMember]
        public bool DownloadText
        {
            get => downloadText;
            set
            {
                SetProperty(ref downloadText, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadQuote
        {
            get => downloadQuote;
            set
            {
                SetProperty(ref downloadQuote, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadPhoto
        {
            get => downloadPhoto;
            set
            {
                SetProperty(ref downloadPhoto, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadLink
        {
            get => downloadLink;
            set
            {
                SetProperty(ref downloadLink, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadAnswer
        {
            get => downloadAnswer;
            set
            {
                SetProperty(ref downloadAnswer, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadConversation
        {
            get => downloadConversation;
            set
            {
                SetProperty(ref downloadConversation, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadVideo
        {
            get => downloadVideo;
            set
            {
                SetProperty(ref downloadVideo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DumpCrawlerData
        {
            get => dumpCrawlerData;
            set
            {
                SetProperty(ref dumpCrawlerData, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool RegExPhotos
        {
            get => regExPhotos;
            set
            {
                SetProperty(ref regExPhotos, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool RegExVideos
        {
            get => regExVideos;
            set
            {
                SetProperty(ref regExVideos, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string FileDownloadLocation
        {
            get => fileDownloadLocation;
            set
            {
                SetProperty(ref fileDownloadLocation, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadAudio
        {
            get => downloadAudio;
            set
            {
                SetProperty(ref downloadAudio, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CreatePhotoMeta
        {
            get => createPhotoMeta;
            set
            {
                SetProperty(ref createPhotoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CreateVideoMeta
        {
            get => createVideoMeta;
            set
            {
                SetProperty(ref createVideoMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CreateAudioMeta
        {
            get => createAudioMeta;
            set
            {
                SetProperty(ref createAudioMeta, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadReplies
        {
            get => downloadReplies;
            set
            {
                SetProperty(ref downloadReplies, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadRebloggedPosts
        {
            get => downloadRebloggedPosts;
            set
            {
                SetProperty(ref downloadRebloggedPosts, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public string ChildId { get; set; }

        [DataMember]
        public BlogTypes BlogType
        {
            get => blogType;
            set
            {
                SetProperty(ref blogType, value);
                Dirty = true;
            }
        }

        public int DownloadedItemsNew
        {
            get => downloadedItemsNew;
            set => SetProperty(ref downloadedItemsNew, value);
        }

        /// <summary>
        /// Number of already downloaded (media) items.
        /// </summary>
        public int DownloadedItems
        {
            get
            {
                return downloadedAnswers + downloadedAudioMetas + downloadedAudios + downloadedConversations + downloadedLinks 
                    + downloadedPhotoMetas + downloadedPhotos + downloadedQuotes + downloadedTexts + downloadedVideoMetas + downloadedVideos;
            }
        }

        /// <summary>
        /// Number of items to download.
        /// </summary>
        [DataMember]
        public int TotalCount
        {
            get => totalCount;
            set => SetProperty(ref totalCount, value);
        }

        [DataMember]
        public string Tags
        {
            get => tags;
            set
            {
                SetProperty(ref tags, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int Rating
        {
            get => rating;
            set
            {
                SetProperty(ref rating, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int Posts
        {
            get => posts;
            set => SetProperty(ref posts, value);
        }

        [DataMember]
        public int Texts
        {
            get => texts;
            set => SetProperty(ref texts, value);
        }

        [DataMember]
        public int Answers
        {
            get => answers;
            set => SetProperty(ref answers, value);
        }

        [DataMember]
        public int Quotes
        {
            get => quotes;
            set => SetProperty(ref quotes, value);
        }

        [DataMember]
        public int Photos
        {
            get => photos;
            set => SetProperty(ref photos, value);
        }

        [DataMember]
        public int NumberOfLinks
        {
            get => numberOfLinks;
            set => SetProperty(ref numberOfLinks, value);
        }

        [DataMember]
        public int Conversations
        {
            get => conversations;
            set => SetProperty(ref conversations, value);
        }

        [DataMember]
        public int Videos
        {
            get => videos;
            set => SetProperty(ref videos, value);
        }

        [DataMember]
        public int Audios
        {
            get => audios;
            set => SetProperty(ref audios, value);
        }

        [DataMember]
        public int PhotoMetas
        {
            get => photoMetas;
            set => SetProperty(ref photoMetas, value);
        }

        [DataMember]
        public int VideoMetas
        {
            get => videoMetas;
            set => SetProperty(ref videoMetas, value);
        }

        [DataMember]
        public int AudioMetas
        {
            get => audioMetas;
            set => SetProperty(ref audioMetas, value);
        }

        [DataMember]
        public int DownloadedTexts
        {
            get => downloadedTexts;
            set
            {
                if (SetProperty(ref downloadedTexts, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedQuotes
        {
            get => downloadedQuotes;
            set
            {
                if (SetProperty(ref downloadedQuotes, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedPhotos
        {
            get => downloadedPhotos;
            set
            {
                if (SetProperty(ref downloadedPhotos, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedLinks
        {
            get => downloadedLinks;
            set
            {
                if (SetProperty(ref downloadedLinks, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedConversations
        {
            get => downloadedConversations;
            set
            {
                if (SetProperty(ref downloadedConversations, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedAnswers
        {
            get => downloadedAnswers;
            set
            {
                if (SetProperty(ref downloadedAnswers, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedVideos
        {
            get => downloadedVideos;
            set
            {
                if (SetProperty(ref downloadedVideos, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedAudios
        {
            get => downloadedAudios;
            set
            {
                if (SetProperty(ref downloadedAudios, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedPhotoMetas
        {
            get => downloadedPhotoMetas;
            set
            {
                if (SetProperty(ref downloadedPhotoMetas, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedVideoMetas
        {
            get => downloadedVideoMetas;
            set
            {
                if (SetProperty(ref downloadedVideoMetas, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public int DownloadedAudioMetas
        {
            get => downloadedAudioMetas;
            set
            {
                if (SetProperty(ref downloadedAudioMetas, value))
                {
                    RaisePropertyChanged(nameof(DownloadedItems));
                }
            }
        }

        [DataMember]
        public MetadataType MetadataFormat
        {
            get => metadataFormat;
            set => SetProperty(ref metadataFormat, value);
        }

        [DataMember]
        public bool DownloadGfycat
        {
            get => downloadGfycat;
            set => SetProperty(ref downloadGfycat, value);
        }

        [DataMember]
        public GfycatTypes GfycatType
        {
            get => gfycatType;
            set => SetProperty(ref gfycatType, value);
        }

        [DataMember]
        public bool DownloadImgur
        {
            get => downloadImgur;
            set => SetProperty(ref downloadImgur, value);
        }

        [DataMember]
        public bool DownloadWebmshare
        {
            get => downloadWebmshare;
            set => SetProperty(ref downloadWebmshare, value);
        }

        [DataMember]
        public WebmshareTypes WebmshareType
        {
            get => webmshareType;
            set => SetProperty(ref webmshareType, value);
        }

        [DataMember]
        public bool DownloadUguu
        {
            get => downloadUguu;
            set => SetProperty(ref downloadUguu, value);
        }

        [DataMember]
        public UguuTypes UguuType
        {
            get => uguuType;
            set => SetProperty(ref uguuType, value);
        }

        [DataMember]
        public bool DownloadCatBox
        {
            get => downloadCatBox;
            set => SetProperty(ref downloadCatBox, value);
        }

        [DataMember]
        public CatBoxTypes CatBoxType
        {
            get => catboxType;
            set => SetProperty(ref catboxType, value);
        }

        [DataMember]
        public string DownloadPages
        {
            get => downloadPages;
            set
            {
                SetProperty(ref downloadPages, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int PageSize
        {
            get => pageSize;
            set
            {
                SetProperty(ref pageSize, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string DownloadFrom
        {
            get => downloadFrom;
            set
            {
                SetProperty(ref downloadFrom, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string DownloadTo
        {
            get => downloadTo;
            set
            {
                SetProperty(ref downloadTo, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string Password
        {
            get => password;
            set
            {
                SetProperty(ref password, value);
                Dirty = true;
            }
        }

        [DataMember]
        public DateTime DateAdded
        {
            get => dateAdded;
            set => SetProperty(ref dateAdded, value);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LastCompleteCrawl
        {
            get => lastCompleteCrawl;
            set => SetProperty(ref lastCompleteCrawl, value);
        }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LatestPost
        {
            get => latestPost;
            set => SetProperty(ref latestPost, value);
        }

        [DataMember]
        public string FilenameTemplate
        {
            get => filenameTemplate;
            set {
                if (string.IsNullOrEmpty(value)) value = "%f";
                SetProperty(ref filenameTemplate, value);
            }
        }

        [DataMember]
        public bool Online
        {
            get => online;
            set => SetProperty(ref online, value);
        }

        [DataMember]
        public int SettingsTabIndex
        {
            get => settingsTabIndex;
            set => SetProperty(ref settingsTabIndex, value);
        }

        [DataMember]
        public int Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }

        [DataMember]
        public string Notes
        {
            get => notes;
            set
            {
                SetProperty(ref notes, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool CheckDirectoryForFiles
        {
            get => checkDirectoryForFiles;
            set
            {
                SetProperty(ref checkDirectoryForFiles, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadUrlList
        {
            get => downloadUrlList;
            set
            {
                SetProperty(ref downloadUrlList, value);
                Dirty = true;
            }
        }

        // no DataMember
        public bool Dirty { get; set; }

        // no DataMember
        public Exception LoadError { get; set; }

        public List<string> Links => links;

        public string LastDownloadedPhoto
        {
            get => lastDownloadedPhoto;
            set
            {
                SetProperty(ref lastDownloadedPhoto, value);
                States = PostType.Photo;
            }
        }

        public string LastDownloadedVideo
        {
            get => lastDownloadedVideo;
            set
            {
                SetProperty(ref lastDownloadedVideo, value);
                States = PostType.Video;
            }
        }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public ulong LastId { get; set; }

        [DataMember]
        public bool SkipGif
        {
            get => skipGif;
            set
            {
                SetProperty(ref skipGif, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool DownloadVideoThumbnail
        {
            get => downloadVideoThumbnail;
            set
            {
                SetProperty(ref downloadVideoThumbnail, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool ForceSize
        {
            get => forceSize;
            set
            {
                SetProperty(ref forceSize, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool ForceRescan
        {
            get => forceRescan;
            set
            {
                SetProperty(ref forceRescan, value);
                Dirty = true;
            }
        }

        [DataMember]
        public bool GroupPhotoSets
        {
            get => groupPhotoSets;
            set
            {
                SetProperty(ref groupPhotoSets, value);
                Dirty = true;
            }
        }

        [DataMember]
        public int CollectionId
        {
            get => collectionId;
            set
            {
                SetProperty(ref collectionId, value);
                Dirty = true;
            }
        }

        [DataMember]
        public string PnjDownloadFormat
        {
            get => pnjDownloadFormat;
            set
            {
                SetProperty(ref pnjDownloadFormat, value);
                Dirty = true;
            }
        }

        [IgnoreDataMember]
        public long LastPreviewShown { get; set; }

        public void UpdateProgress(bool doCount)
        {
            lock (lockObjectProgress)
            {
                if (doCount) { DownloadedItemsNew++; }
                Progress = (int)(DownloadedItems / (double)TotalCount * 100);
            }
        }

        public void UpdatePostCount(string propertyName)
        {
            lock (lockObjectPostCount)
            {
                PropertyInfo property = typeof(IBlog).GetProperty(propertyName);
                var postCounter = (int)property.GetValue(this);
                postCounter++;
                property.SetValue(this, postCounter, null);
            }
        }

        public void AddFileToDb(string fileName)
        {
            lock (lockObjectDb)
            {
                Links.Add(fileName);
            }
        }

        public bool CreateDataFolder()
        {
            if (!Directory.Exists(DownloadLocation()))
            {
                Directory.CreateDirectory(DownloadLocation());
                return true;
            }

            return false;
        }

        public virtual bool CheckIfFileExistsInDB(string filename)
        {
            Monitor.Enter(lockObjectDb);
            try
            {
                return Links.Contains(filename);
            }
            finally
            {
                Monitor.Exit(lockObjectDb);
            }
        }

        public virtual bool CheckIfBlogShouldCheckDirectory(string filename, string filenameNew)
        {
            return CheckDirectoryForFiles && CheckIfFileExistsInDirectory(filename, filenameNew);
        }

        public virtual bool CheckIfFileExistsInDirectory(string filename, string filenameNew)
        {
            Monitor.Enter(lockObjectDirectory);
            string blogPath = DownloadLocation();
            try
            {
                string filepath = Path.Combine(blogPath, filename);
                string filepathNew = Path.Combine(blogPath, filenameNew);
                bool result = File.Exists(filepath);
                if (result && !string.IsNullOrEmpty(filenameNew))
                {
                    if (File.Exists(filepathNew))
                    {
                        Logger.Warning("{0}: Cannot rename file to '{1}', a file with that name already exists!", Name, filenameNew);
                    }
                    else
                    {
                        File.Move(filepath, filepathNew);
                    }
                }
                if (result || string.IsNullOrEmpty(filenameNew)) return result;
                return File.Exists(filepathNew);
            }
            finally
            {
                Monitor.Exit(lockObjectDirectory);
            }
        }

        public string DownloadLocation()
        {
            if (string.IsNullOrWhiteSpace(FileDownloadLocation))
            {
                return Path.Combine(Directory.GetParent(Location).FullName, Name);
            }

            return FileDownloadLocation;
        }

        public IBlog Load(string fileLocation)
        {
            try
            {
                return LoadCore(fileLocation);
            }
            catch (Exception ex) when (ex is SerializationException || ex is FileNotFoundException)
            {
                ex.Data.Add("Filename", fileLocation);
                throw;
            }
        }

        private IBlog LoadCore(string fileLocation)
        {
            using (var stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new DataContractJsonSerializer(GetType());
                var blog = (Blog)serializer.ReadObject(stream);

                if (blog.Version == "3")
                {
                    _ = Enum.TryParse(Path.GetExtension(fileLocation).Replace(".", ""), out BlogTypes blogType);
                    blog.OriginalBlogType = blogType;
                    blog.Version = "4";
                }

                if (string.IsNullOrEmpty(blog.FilenameTemplate))
                    blog.FilenameTemplate = "%f";
                if (string.IsNullOrEmpty(blog.Location))
                    blog.Location = Path.Combine(Directory.GetParent(fileLocation).FullName, "Index");
                if (string.IsNullOrEmpty(blog.ChildId))
                    blog.ChildId = Path.Combine(blog.Location, blog.Name + "_files." + blog.OriginalBlogType);
                if (blog.Links != null && blog.BlogType != BlogTypes.twitter && blog.BlogType != BlogTypes.newtumbl && blog.BlogType != BlogTypes.instagram)
                {
                    // use leftover property Links to indicate if one-time update of DownloadVideoThumbnail was done
                    blog.DownloadVideoThumbnail = true;
                    blog.links = null;
                }
                if (string.IsNullOrEmpty(blog.PnjDownloadFormat))
                {
                    blog.PnjDownloadFormat = nameof(PnjDownloadType.png);
                }
                if (blog.BlogType == BlogTypes.twitter && blog.DownloadAnswer && !blog.DownloadReplies)
                {
                    blog.DownloadReplies = true;
                    blog.DownloadAnswer = false;
                }

                return blog;
            }
        }

        public bool Save()
        {
            try
            {
                Dirty = false;
                SaveBlog();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Blog:Save: {0}", ex);
                throw;
            }
        }

        private void SaveBlog()
        {
            string currentIndex = Path.Combine(Location, Name + "." + OriginalBlogType);
            string newIndex = Path.Combine(Location, Name + "." + OriginalBlogType + ".new");
            string backupIndex = Path.Combine(Location, Name + "." + OriginalBlogType + ".bak");

            if (File.Exists(currentIndex))
            {
                SaveCore(newIndex);

                File.Replace(newIndex, currentIndex, backupIndex, true);
                File.Delete(backupIndex);
            }
            else
            {
                SaveCore(currentIndex);
            }
        }

        private void SaveCore(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(GetType());
                    serializer.WriteObject(writer, this);
                    writer.Flush();
                }
            }
        }

        protected static string ExtractSubDomain(string url)
        {
            string[] source = url.Split('.');
            if ((source.Length >= 3) && source[0].StartsWith("http://", true, null))
            {
                return source[0].Replace("http://", string.Empty);
            }

            if ((source.Length >= 3) && source[0].StartsWith("https://", true, null))
            {
                return source[0].Replace("https://", string.Empty);
            }

            return null;
        }

        public static string ExtractName(string url)
        {
            return ExtractSubDomain(url);
        }

        protected static string ExtractUrl(string url)
        {
            return ("https://" + ExtractSubDomain(url) + ".tumblr.com/");
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            lockObjectProgress = new object();
            lockObjectPostCount = new object();
            lockObjectDb = new object();
            lockObjectDirectory = new object();
        }
    }
#pragma warning restore SX1309 // Field names should begin with underscore
}
