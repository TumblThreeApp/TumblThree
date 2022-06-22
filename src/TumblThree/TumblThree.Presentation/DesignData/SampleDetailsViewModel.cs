using System;

using TumblThree.Applications.ViewModels.DetailsViewModels;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Presentation.DesignData
{
    public class SampleDetailsViewModel : DetailsAllViewModel
    {
        public SampleDetailsViewModel()
            : base(new MockDetailsView(), null, null)
        {
            var BlogFile = new[]
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
                    Posts = 912713,
                    Texts = 10299,
                    Photos = 69418,
                    Videos = 7435,
                    Conversations = 891,
                    NumberOfLinks = 0,
                }
            };
            Count = 1;
        }

        private class MockDetailsView : MockView, IDetailsView
        {
            public int TabsCount => 3;
        }
    }
}
