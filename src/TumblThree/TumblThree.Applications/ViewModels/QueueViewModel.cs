using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.Services;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.ViewModels
{
    [Export]
    public class QueueViewModel : ViewModel<IQueueView>
    {
        private ICommand _clearQueueCommand;
        private ICommand _openQueueCommand;
        private ICommand _removeSelectedCommand;
        private ICommand _saveQueueCommand;
        private ICommand _showBlogDetailsCommand;

        private QueueManager _queueManager;
        private QueueListItem _selectedQueueItem;
        private readonly ObservableCollection<QueueListItem> _selectedQueueItems;

        [ImportingConstructor]
        public QueueViewModel(IQueueView view, ICrawlerService crawlerService)
            : base(view)
        {
            _selectedQueueItems = new ObservableCollection<QueueListItem>();
            CrawlerService = crawlerService;
        }

        public QueueManager QueueManager
        {
            get => _queueManager;
            set => SetProperty(ref _queueManager, value);
        }

        public ICrawlerService CrawlerService { get; }

        public QueueListItem SelectedQueueItem
        {
            get => _selectedQueueItem;
            set => SetProperty(ref _selectedQueueItem, value);
        }

        public IList<QueueListItem> SelectedQueueItems => _selectedQueueItems;

        public ICommand RemoveSelectedCommand
        {
            get => _removeSelectedCommand;
            set => SetProperty(ref _removeSelectedCommand, value);
        }

        public ICommand ShowBlogDetailsCommand
        {
            get => _showBlogDetailsCommand;
            set => SetProperty(ref _showBlogDetailsCommand, value);
        }

        public ICommand OpenQueueCommand
        {
            get => _openQueueCommand;
            set => SetProperty(ref _openQueueCommand, value);
        }

        public ICommand SaveQueueCommand
        {
            get => _saveQueueCommand;
            set => SetProperty(ref _saveQueueCommand, value);
        }

        public ICommand ClearQueueCommand
        {
            get => _clearQueueCommand;
            set => SetProperty(ref _clearQueueCommand, value);
        }

        public Action<int, IEnumerable<IBlog>> InsertBlogFilesAction { get; set; }
    }
}
