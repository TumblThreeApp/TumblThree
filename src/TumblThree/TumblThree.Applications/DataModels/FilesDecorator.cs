using System.Collections.Generic;
using System.ComponentModel;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;

namespace TumblThree.Domain.Models.Files
{
    internal class FilesDecorator : IFiles
    {
        private readonly IFiles blogDatabase;
        private readonly IGlobalDatabaseService globalDatabaseService;
        private readonly AppSettings settings;

        public FilesDecorator(IFiles blogDatabase, IGlobalDatabaseService globalDatabaseService, AppSettings settings)
        {
            this.blogDatabase = blogDatabase;
            this.globalDatabaseService = globalDatabaseService;
            this.settings = settings;
            blogDatabase.PropertyChanged += BlogDatabase_PropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void BlogDatabase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged.Invoke(this, e);
        }

        public string Name => blogDatabase.Name;

        public string Location => blogDatabase.Location;

        public BlogTypes BlogType => blogDatabase.BlogType;

        public IEnumerable<FileEntry> Entries => blogDatabase.Entries;

        public bool IsDirty => blogDatabase.IsDirty;

        public string Version
        {
            get
            {
                return blogDatabase.Version;
            }
            set
            {
                blogDatabase.Version = value;
            }
        }

        public void AddFileToDb(string fileNameUrl, string fileNameOriginalUrl, string fileName)
        {
            if (settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.AddFileToDb(blogDatabase.Name, blogDatabase.BlogType, fileNameUrl, fileNameOriginalUrl, fileName);
            }
            else
            {
                blogDatabase.AddFileToDb(fileNameUrl, fileNameOriginalUrl, fileName);
            }
        }

        public string AddFileToDb(string fileNameUrl, string fileNameOriginalUrl, string fileName, string appendTemplate)
        {
            if (settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.AddFileToDb(blogDatabase.Name, blogDatabase.BlogType, fileNameUrl, fileNameOriginalUrl, fileName, appendTemplate);
            }
            return blogDatabase.AddFileToDb(fileNameUrl, fileNameOriginalUrl, fileName, appendTemplate);
        }

        public bool CheckIfFileExistsInDB(string filenameUrl, bool checkOriginalLinkFirst)
        {
            if (settings.LoadAllDatabasesIntoDb)
            {
                return globalDatabaseService.CheckIfFileExistsAsync(filenameUrl, checkOriginalLinkFirst, settings.LoadArchive).GetAwaiter().GetResult();
            }
            else
            {
                return blogDatabase.CheckIfFileExistsInDB(filenameUrl, checkOriginalLinkFirst);
            }
        }

        public bool Save()
        {
            return blogDatabase.Save();
        }

        public void UpdateOriginalLink(string filenameUrl, string filenameOriginalUrl)
        {
            if (settings.LoadAllDatabasesIntoDb)
            {
                globalDatabaseService.UpdateOriginalLink(blogDatabase.Name, (int)blogDatabase.BlogType, filenameUrl, filenameOriginalUrl);
            }
            else
            {
                blogDatabase.UpdateOriginalLink(filenameUrl, filenameOriginalUrl);
            }
        }
    }
}
