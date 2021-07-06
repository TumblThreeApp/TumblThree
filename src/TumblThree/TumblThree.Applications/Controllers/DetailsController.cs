using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Windows;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels.DetailsViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    [Export(typeof(IDetailsService))]
    internal class DetailsController : IDetailsService
    {
        private readonly ISelectionService _selectionService;
        private readonly IShellService _shellService;

        private Lazy<IDetailsViewModel> _detailsViewModel;

        private readonly HashSet<IBlog> _blogsToSave;

        private delegate void PropertySetter(bool value);
        private delegate void PropertySetter<T>(T value);

        [ImportingConstructor]
        public DetailsController(IShellService shellService, ISelectionService selectionService, IManagerService managerService)
        {
            _shellService = shellService;
            _selectionService = selectionService;
            _blogsToSave = new HashSet<IBlog>();
        }

        public QueueManager QueueManager { get; set; }

        [ImportMany(typeof(IDetailsViewModel))]
        private IEnumerable<Lazy<IDetailsViewModel, ICrawlerData>> ViewModelFactoryLazy { get; set; }

        public Lazy<IDetailsViewModel> GetViewModel(IBlog blog)
        {
            Lazy<IDetailsViewModel, ICrawlerData> viewModel =
                ViewModelFactoryLazy.FirstOrDefault(list => list.Metadata.BlogType == blog.GetType());

            if (viewModel == null)
            {
                throw new ArgumentException("Website is not supported!", nameof(blog));
            }

            return viewModel;
        }

        private IDetailsViewModel DetailsViewModel => _detailsViewModel.Value;

        public void UpdateBlogPreview(IReadOnlyList<IBlog> blogFiles)
        {
            if (DetailsViewModel?.BlogFile?.SettingsTabIndex == ((IDetailsView)DetailsViewModel?.View)?.TabsCount - 1)
            {
                DetailsViewModel.BlogFile.PropertyChanged -= ChangeBlogSettings;
                SelectBlogFiles(blogFiles, true);
            }
        }

        public void SelectBlogFiles(IReadOnlyList<IBlog> blogFiles, bool showPreview)
        {
            UpdateViewModelBasedOnSelection(blogFiles);

            ClearBlogSelection();

            if (blogFiles.Count <= 1 || showPreview && _shellService.Settings.EnablePreview)
            {
                DetailsViewModel.Count = 1;
                DetailsViewModel.BlogFile = blogFiles.FirstOrDefault();
                if (DetailsViewModel.BlogFile != null)
                {
                    var tabIndex = ((IDetailsView)DetailsViewModel.View).TabsCount - 1;
                    DetailsViewModel.BlogFile.SettingsTabIndex = (showPreview && _shellService.Settings.EnablePreview) ? tabIndex : DetailsViewModel.BlogFile.SettingsTabIndex;
                }
            }
            else if (DetailsViewModel.GetType() == typeof(ViewModels.DetailsViewModels.DetailsAllViewModel))
            {
                DetailsViewModel.Count = blogFiles.Count;
                ((DetailsAllViewModel)DetailsViewModel).BlogAll = CreateFromMultiple(blogFiles.ToArray());
                ((DetailsAllViewModel)DetailsViewModel).BlogAll.PropertyChanged += ChangeBlogSettings;
            }
        }

        public bool FilenameTemplateValidate(string enteredFilenameTemplate)
        {
            if (string.IsNullOrEmpty(enteredFilenameTemplate) || enteredFilenameTemplate == "%f") return true;
            //var tokens = new List<string>() { "%f", "%d", "%p", "%i", "%s" };
            //if (!tokens.Any(x => enteredFilenameTemplate.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) >= 0))
            //{
            //    MessageBox.Show(Resources.FilenameTemplateTokenNotFound, Resources.Warning);
            //    return false;
            //}
            var needed = new List<string>() { "%x", "%y" };
            if (enteredFilenameTemplate.IndexOf("%f", StringComparison.InvariantCultureIgnoreCase) == -1 &&
                !needed.Any(x => enteredFilenameTemplate.IndexOf(x, StringComparison.InvariantCultureIgnoreCase) >= 0))
            {
                MessageBox.Show(Resources.FilenameTemplateAppendTokenNotFound, Resources.Warning);
                return false;
            }
            return true;
        }

        private void UpdateViewModelBasedOnSelection(IReadOnlyList<IBlog> blogFiles)
        {
            if (blogFiles.Count == 0)
            {
                return;
            }

            _detailsViewModel = GetViewModel(blogFiles.Count < 2
                ? blogFiles.FirstOrDefault()
                : new Blog());
            _shellService.DetailsView = DetailsViewModel.View;
            _shellService.UpdateDetailsView();
        }

        private void ChangeBlogSettings(object sender, PropertyChangedEventArgs e)
        {
            foreach (IBlog blog in _blogsToSave)
            {
                PropertyInfo property = typeof(IBlogAll).GetProperty(e.PropertyName);
                if (property == null)
                    property = typeof(IBlog).GetProperty(e.PropertyName);
                if (CheckIfCanUpdateTumblrBlogCrawler(blog, property))
                    continue;
                var value = property.GetValue(((DetailsAllViewModel)DetailsViewModel).BlogAll);
                if (value == null)
                    continue;
                PropertyInfo propertySet = typeof(IBlog).GetProperty(e.PropertyName);
                if (propertySet == null)
                    continue;
                propertySet.SetValue(blog, value);
            }
        }

        public void Initialize()
        {
            ((INotifyCollectionChanged)_selectionService.SelectedBlogFiles).CollectionChanged += SelectedBlogFilesCollectionChanged;
            _detailsViewModel = GetViewModel(new Blog());
            _shellService.DetailsView = DetailsViewModel.View;
        }

        public void Shutdown()
        {
        }

        public IBlogAll CreateFromMultiple(IEnumerable<IBlog> blogFiles)
        {
            List<IBlog> sharedBlogFiles = blogFiles.ToList();
            if (!sharedBlogFiles.Any())
            {
                throw new ArgumentException("The collection must have at least one item.", nameof(blogFiles));
            }

            foreach (IBlog blog in sharedBlogFiles)
            {
                _blogsToSave.Add(blog);
            }

            IBlogAll ba = new BlogAll
            {
                Name = string.Join(", ", sharedBlogFiles.Select(blog => blog.Name).ToArray()),
                Url = string.Join(", ", sharedBlogFiles.Select(blog => blog.Url).ToArray()),
                Posts = sharedBlogFiles.Sum(blogs => blogs.Posts),
                TotalCount = sharedBlogFiles.Sum(blogs => blogs.TotalCount),
                Texts = sharedBlogFiles.Sum(blogs => blogs.Texts),
                Answers = sharedBlogFiles.Sum(blogs => blogs.Answers),
                Quotes = sharedBlogFiles.Sum(blogs => blogs.Quotes),
                Photos = sharedBlogFiles.Sum(blogs => blogs.Photos),
                NumberOfLinks = sharedBlogFiles.Sum(blogs => blogs.NumberOfLinks),
                Conversations = sharedBlogFiles.Sum(blogs => blogs.Conversations),
                Videos = sharedBlogFiles.Sum(blogs => blogs.Videos),
                Audios = sharedBlogFiles.Sum(blogs => blogs.Audios),
                PhotoMetas = sharedBlogFiles.Sum(blogs => blogs.PhotoMetas),
                VideoMetas = sharedBlogFiles.Sum(blogs => blogs.VideoMetas),
                AudioMetas = sharedBlogFiles.Sum(blogs => blogs.AudioMetas),
                DownloadedTexts = sharedBlogFiles.Sum(blogs => blogs.DownloadedTexts),
                DownloadedQuotes = sharedBlogFiles.Sum(blogs => blogs.DownloadedQuotes),
                DownloadedPhotos = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotos),
                DownloadedLinks = sharedBlogFiles.Sum(blogs => blogs.DownloadedLinks),
                DownloadedConversations = sharedBlogFiles.Sum(blogs => blogs.DownloadedConversations),
                DownloadedAnswers = sharedBlogFiles.Sum(blogs => blogs.DownloadedAnswers),
                DownloadedVideos = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideos),
                DownloadedAudios = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudios),
                DownloadedPhotoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedPhotoMetas),
                DownloadedVideoMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedVideoMetas),
                DownloadedAudioMetas = sharedBlogFiles.Sum(blogs => blogs.DownloadedAudioMetas),

                DownloadAnswer = SetCheckBox(sharedBlogFiles, "DownloadAnswer"),
                DownloadAudio = SetCheckBox(sharedBlogFiles, "DownloadAudio"),
                DownloadConversation = SetCheckBox(sharedBlogFiles, "DownloadConversation"),
                DownloadLink = SetCheckBox(sharedBlogFiles, "DownloadLink"),
                DownloadPhoto = SetCheckBox(sharedBlogFiles, "DownloadPhoto"),
                DownloadQuote = SetCheckBox(sharedBlogFiles, "DownloadQuote"),
                DownloadText = SetCheckBox(sharedBlogFiles, "DownloadText"),
                DownloadVideo = SetCheckBox(sharedBlogFiles, "DownloadVideo"),
                CreatePhotoMeta = SetCheckBox(sharedBlogFiles, "CreatePhotoMeta"),
                CreateVideoMeta = SetCheckBox(sharedBlogFiles, "CreateVideoMeta"),
                CreateAudioMeta = SetCheckBox(sharedBlogFiles, "CreateAudioMeta"),
                DownloadRebloggedPosts = SetCheckBox(sharedBlogFiles, "DownloadRebloggedPosts"),
                SkipGif = SetCheckBox(sharedBlogFiles, "SkipGif"),
                GroupPhotoSets = SetCheckBox(sharedBlogFiles, "GroupPhotoSets"),
                ForceSize = SetCheckBox(sharedBlogFiles, "ForceSize"),
                ForceRescan = SetCheckBox(sharedBlogFiles, "ForceRescan"),
                CheckDirectoryForFiles = SetCheckBox(sharedBlogFiles, "CheckDirectoryForFiles"),
                DownloadUrlList = SetCheckBox(sharedBlogFiles, "DownloadUrlList"),
                DownloadImgur = SetCheckBox(sharedBlogFiles, "DownloadImgur"),
                DownloadGfycat = SetCheckBox(sharedBlogFiles, "DownloadGfycat"),
                DownloadWebmshare = SetCheckBox(sharedBlogFiles, "DownloadWebmshare"),
                DownloadMixtape = SetCheckBox(sharedBlogFiles, "DownloadMixtape"),
                DownloadUguu = SetCheckBox(sharedBlogFiles, "DownloadUguu"),
                DownloadSafeMoe = SetCheckBox(sharedBlogFiles, "DownloadSafeMoe"),
                DownloadLoliSafe = SetCheckBox(sharedBlogFiles, "DownloadLoliSafe"),
                DownloadCatBox = SetCheckBox(sharedBlogFiles, "DownloadCatBox"),
                DumpCrawlerData = SetCheckBox(sharedBlogFiles, "DumpCrawlerData"),
                RegExPhotos = SetCheckBox(sharedBlogFiles, "RegExPhotos"),
                RegExVideos = SetCheckBox(sharedBlogFiles, "RegExVideos")
            };

            ba.DownloadPages = SetProperty<string>(sharedBlogFiles, "DownloadPages", (outval) => ba.DownloadPagesEnabled = outval);
            ba.PageSize = SetProperty<int>(sharedBlogFiles, "PageSize", (outval) => ba.PageSizeEnabled = outval);
            ba.DownloadFrom = SetProperty<string>(sharedBlogFiles, "DownloadFrom", (outval) => ba.DownloadFromEnabled = outval);
            ba.DownloadTo = SetProperty<string>(sharedBlogFiles, "DownloadTo", (outval) => ba.DownloadToEnabled = outval);
            ba.Tags = SetProperty<string>(sharedBlogFiles, "Tags", (outval) => ba.TagsEnabled = outval);
            ba.Password = SetProperty<string>(sharedBlogFiles, "Password", (outval) => ba.PasswordEnabled = outval);
            bool dummy = false;
            ba.GfycatType = SetProperty<GfycatTypes>(sharedBlogFiles, "GfycatType", (outval) => dummy = outval);
            ba.WebmshareType = SetProperty<WebmshareTypes>(sharedBlogFiles, "WebmshareType", (outval) => dummy = outval);
            ba.MixtapeType = SetProperty<MixtapeTypes>(sharedBlogFiles, "MixtapeType", (outval) => dummy = outval);
            ba.UguuType = SetProperty<UguuTypes>(sharedBlogFiles, "UguuType", (outval) => dummy = outval);
            ba.SafeMoeType = SetProperty<SafeMoeTypes>(sharedBlogFiles, "SafeMoeType", (outval) => dummy = outval);
            ba.LoliSafeType = SetProperty<LoliSafeTypes>(sharedBlogFiles, "LoliSafeType", (outval) => dummy = outval);
            ba.CatBoxType = SetProperty<CatBoxTypes>(sharedBlogFiles, "CatBoxType", (outval) => dummy = outval);
            ba.MetadataFormat = SetProperty<MetadataType>(sharedBlogFiles, "MetadataFormat", (outval) => ba.MetadataFormatEnabled = outval);
            ba.BlogType = SetProperty<BlogTypes>(sharedBlogFiles, "BlogType", (outval) => dummy = outval);
            ba.BlogTypeEnabled = false;
            ba.FileDownloadLocation = SetProperty<string>(sharedBlogFiles, "FileDownloadLocation", (outval) => dummy = outval);
            ba.FileDownloadLocationEnabled = false;
            ba.FilenameTemplate = SetProperty<string>(sharedBlogFiles, "FilenameTemplate", (outval) => ba.FilenameTemplateEnabled = outval);
            ba.SettingsTabIndex = SetProperty<int>(sharedBlogFiles, "SettingsTabIndex", (outval) => dummy = outval);

            ba.Dirty = false;

            return ba;
        }

        private static T SetProperty<T>(IReadOnlyCollection<IBlog> blogs, string propertyName, PropertySetter allEqual) where T : IConvertible
        {
            PropertyInfo property = typeof(IBlog).GetProperty(propertyName);
            var value = (T)property.GetValue(blogs.FirstOrDefault());
            if (value == null)
            {
                if (typeof(T) == typeof(string))
                    value = (T)(object)string.Empty;
                else
                    value = default(T);
            }

            int numberOfBlogs = blogs.Count;
            int sameValues = blogs.Select(blog => (T)property.GetValue(blog)).Count(v => (v == null ? typeof(T) == typeof(string) ? (T)(object)string.Empty : default(T) : v).Equals(value));
            allEqual(sameValues == numberOfBlogs);

            return (sameValues == numberOfBlogs) ? value : default(T);
        }

        private static bool? SetCheckBox(IReadOnlyCollection<IBlog> blogs, string propertyName)
        {
            PropertyInfo property = typeof(IBlog).GetProperty(propertyName);

            int numberOfBlogs = blogs.Count;
            int checkedBlogs = blogs.Select(blog => (bool)property.GetValue(blog)).Count(state => state);

            if (checkedBlogs == numberOfBlogs) return true;
            if (checkedBlogs == 0) return false;
            return null;
        }

        private static bool CheckIfCanUpdateTumblrBlogCrawler(IBlog blog, PropertyInfo property)
        {
            return property.PropertyType == typeof(BlogTypes) && !(blog is TumblrBlog || blog is TumblrHiddenBlog);
        }

        private void ClearBlogSelection()
        {
            if (_blogsToSave.Any())
            {
                _blogsToSave.Clear();
            }
        }

        private void SelectedBlogFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DetailsViewModel.BlogFile != null)
            {
                DetailsViewModel.BlogFile.PropertyChanged -= ChangeBlogSettings;
            }

            SelectBlogFiles(_selectionService.SelectedBlogFiles.ToArray(), false);
        }
    }
}
