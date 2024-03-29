﻿using System;
using System.Linq;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Queue;

namespace TumblThree.Presentation.DesignData
{
    public class SampleQueueViewModel : QueueViewModel
    {
        public SampleQueueViewModel()
            : base(new MockQueueView(), new MockCrawlerService(), new MockShellService(), new MockManagerService())
        {
            var blogFiles = new[]
            {
                new Blog
                {
                    Name = "Nature Wallpapers",
                    Url = "http://nature-wallpaper.tumblr.com/",
                    DownloadedPhotos = 123,
                    DateAdded = DateTime.Now,
                    Progress = 66,
                    TotalCount = 234,
                },
                new Blog
                {
                    Name = "Landscape Wallpapers",
                    Url = "http://landscape-wallpaper.tumblr.com/",
                    DownloadedPhotos = 17236,
                    DateAdded = DateTime.Now,
                    Progress = 95,
                    TotalCount = 15739,
                },
                new Blog
                {
                    Name = "FX Wallpapers",
                    Url = "http://nature-wallpaper.tumblr.com/",
                    DownloadedPhotos = 12845,
                    DateAdded = DateTime.Now,
                    Progress = 12,
                    TotalCount = 82453,
                }
            };
            var queueManager = new QueueManager();
            queueManager.AddItems(blogFiles.Select(x => new QueueListItem(x)));
            QueueManager = queueManager;
            ((MockCrawlerService)CrawlerService).SetActiveBlogFiles(blogFiles.ToArray());
        }

        private class MockQueueView : MockView, IQueueView
        {
            public void FocusSelectedItem()
            {
            }

            public void ScrollIntoView(QueueListItem item)
            {
            }
        }
    }
}
