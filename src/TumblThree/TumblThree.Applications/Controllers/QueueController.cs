using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Waf.Foundation;
using System.Xml;

using TumblThree.Applications.Data;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export]
    internal class QueueController
    {
        private readonly ICrawlerService _crawlerService;
        private readonly IDetailsService _detailsService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IManagerService _managerService;
        private readonly IShellService _shellService;

        private readonly DelegateCommand _clearQueueCommand;
        private readonly DelegateCommand _openQueueCommand;
        private readonly DelegateCommand _removeSelectedCommand;
        private readonly DelegateCommand _saveQueueCommand;
        private readonly DelegateCommand _showBlogDetailsCommand;

        private readonly Lazy<QueueViewModel> _queueViewModel;

        private readonly FileType _saveQueuelistFileType;

        private readonly FileType _openQueuelistFileType;

        [ImportingConstructor]
        public QueueController(IFileDialogService fileDialogService, IShellService shellService, IDetailsService detailsService,
            IManagerService managerService, ICrawlerService crawlerService, Lazy<QueueViewModel> queueViewModel)
        {
            _fileDialogService = fileDialogService;
            _shellService = shellService;
            _queueViewModel = queueViewModel;
            _managerService = managerService;
            _crawlerService = crawlerService;
            _detailsService = detailsService;
            _removeSelectedCommand = new DelegateCommand(RemoveSelected, CanRemoveSelected);
            _showBlogDetailsCommand = new DelegateCommand(ShowBlogDetails);
            _openQueueCommand = new DelegateCommand(OpenList);
            _saveQueueCommand = new DelegateCommand(SaveList);
            _clearQueueCommand = new DelegateCommand(ClearList);
            _openQueuelistFileType = new FileType(Resources.Queuelist, SupportedFileTypes.QueueFileExtensions);
            _saveQueuelistFileType = new FileType(Resources.Queuelist, SupportedFileTypes.QueueFileExtensions[0]);
        }

        public QueueSettings QueueSettings { get; set; }

        public QueueManager QueueManager { get; set; }

        private QueueViewModel QueueViewModel => _queueViewModel.Value;

        public void Initialize()
        {
            QueueViewModel.QueueManager = QueueManager;
            QueueViewModel.RemoveSelectedCommand = _removeSelectedCommand;
            QueueViewModel.ShowBlogDetailsCommand = _showBlogDetailsCommand;
            QueueViewModel.OpenQueueCommand = _openQueueCommand;
            QueueViewModel.SaveQueueCommand = _saveQueueCommand;
            QueueViewModel.ClearQueueCommand = _clearQueueCommand;
            QueueViewModel.InsertBlogFilesAction = InsertBlogFiles;

            _crawlerService.RemoveBlogFromQueueCommand = _removeSelectedCommand;
            _crawlerService.ActiveItems.CollectionChanged += CrawlerServiceActiveItemsCollectionChanged;

            QueueViewModel.PropertyChanged += QueueViewModelPropertyChanged;

            _shellService.QueueView = QueueViewModel.View;
        }

        public void Run()
        {
        }

        public void LoadQueue()
        {
            ClearList();
            IReadOnlyList<string> blogNamesToLoad = QueueSettings.Names;
            IReadOnlyList<BlogTypes> blogTypesToLoad = QueueSettings.Types;
            InsertFilesCore(0, blogNamesToLoad, blogTypesToLoad);
        }

        public void Shutdown() => QueueSettings.ReplaceAll(QueueManager.Items.Select(x => x.Blog.Name),
            QueueManager.Items.Select(x => x.Blog.BlogType));

        private bool CanRemoveSelected() => QueueViewModel.SelectedQueueItem != null;

        private void RemoveSelected()
        {
            QueueListItem[] queueItemsToExclude = QueueViewModel.SelectedQueueItems.Except(new[] { QueueViewModel.SelectedQueueItem }).ToArray();
            QueueListItem nextQueueItem = CollectionHelper.GetNextElementOrDefault(QueueManager.Items.Except(queueItemsToExclude).ToArray(), QueueViewModel.SelectedQueueItem);

            QueueManager.RemoveItems(QueueViewModel.SelectedQueueItems);
            QueueViewModel.SelectedQueueItem = nextQueueItem ?? QueueManager.Items.LastOrDefault();
        }

        private void ShowBlogDetails()
        {
            _detailsService.SelectBlogFiles(QueueViewModel.SelectedQueueItems.Select(x => x.Blog).ToArray(), true);
            _shellService.ShowDetailsView();
        }

        private void InsertBlogFiles(int index, IEnumerable<IBlog> blogFiles) =>
            QueueManager.InsertItems(index, blogFiles.Select(x => new QueueListItem(x)));

        private void OpenList()
        {
            FileDialogResult result = _fileDialogService.ShowOpenFileDialog(_shellService.ShellView, _openQueuelistFileType);
            if (!result.IsValid)
            {
                return;
            }

            OpenListCore(result.FileName);
        }

        private void OpenListCore(string queuelistFileName)
        {
            QueueSettings queueList;

            try
            {
                using (var stream = new FileStream(queuelistFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(QueueSettings));
                    queueList = (QueueSettings)serializer.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController:OpenListCore: {0}", ex);
                _shellService.ShowError(new QueuelistLoadException(ex), Resources.CouldNotLoadQueuelist);
                return;
            }

            InsertFilesCore(QueueManager.Items.Count, queueList.Names.ToArray(), queueList.Types.ToArray());
        }

        private void InsertFilesCore(int index, IEnumerable<string> names, IEnumerable<BlogTypes> blogTypes)
        {
            try
            {
                InsertBlogFiles(index, names.Zip(blogTypes, Tuple.Create).Select(x => _managerService.BlogFiles.First(blogs => blogs.Name.Equals(x.Item1) && blogs.BlogType.Equals(x.Item2))));
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController.InsertFileCore: {0}", ex);
                _shellService.ShowError(new QueuelistLoadException(ex), Resources.CouldNotLoadQueuelist);
            }
        }

        private void SaveList()
        {
            FileDialogResult result = _fileDialogService.ShowSaveFileDialog(_shellService.ShellView, _saveQueuelistFileType);
            if (!result.IsValid)
            {
                return;
            }

            var queueList = new QueueSettings();
            queueList.ReplaceAll(QueueManager.Items.Select(item => item.Blog.Name).ToList(), QueueManager.Items.Select(item => item.Blog.BlogType).ToList());

            try
            {
                string targetFolder = Path.GetDirectoryName(result.FileName);
                string name = Path.GetFileNameWithoutExtension(result.FileName);

                using (
                    var stream = new FileStream(Path.Combine(targetFolder, name) + ".que", FileMode.Create, FileAccess.Write,
                        FileShare.None))
                {
                    using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                        stream, Encoding.UTF8, true, true, "  "))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(QueueSettings));
                        serializer.WriteObject(writer, queueList);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("QueueController:SaveList: {0}", ex);
                _shellService.ShowError(new QueuelistSaveException(ex), Resources.CouldNotSaveQueueList);
            }
        }

        private void ClearList() => QueueManager.ClearItems();

        private void QueueViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QueueViewModel.SelectedQueueItem))
            {
                UpdateCommands();
            }
        }

        private void CrawlerServiceActiveItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_crawlerService.ActiveItems.Count > 0)
            {
                _detailsService.UpdateBlogPreview(_crawlerService.ActiveItems.OrderByDescending(x => x.Blog.LastPreviewShown).Take(1).Select(x => x.Blog).ToArray());
            }
        }

        private void UpdateCommands() => _removeSelectedCommand.RaiseCanExecuteChanged();
    }
}
