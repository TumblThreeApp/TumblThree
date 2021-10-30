using System;
using System.Collections.Generic;
using TumblThree.Applications.Views;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;

namespace TumblThree.Applications.Services
{
    public interface IDetailsService
    {
        void SelectBlogFiles(IReadOnlyList<IBlog> blogFiles, bool showPreview);
        void UpdateBlogPreview(IReadOnlyList<IBlog> blogFiles);
        bool FilenameTemplateValidate(string enteredFilenameTemplate);

        IDetailsViewModel DetailsViewModel { get; }

        void ViewFullScreenMedia();

        bool ChangeCollection(IBlog blog, IList<Collection> oldItem, IList<Collection> newItem);

        event EventHandler DetailsViewModelChanged;

        event EventHandler FinishedCrawlingLastBlog;
    }
}
