using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models.Files
{
    public interface IFiles : INotifyPropertyChanged
    {
        string Name { get; }

        BlogTypes BlogType { get; }

        //IList<string> Links { get; }

        IEnumerable<FileEntry> Entries { get; }

        bool IsDirty { get; }

        string Version { get; set; }

        void AddFileToDb(string fileNameUrl, string fileNameOriginalUrl, string fileName);

        string AddFileToDb(string fileNameUrl, string fileNameOriginalUrl, string fileName, string appendTemplate);

        void UpdateOriginalLink(string filenameUrl, string filenameOriginalUrl);

        bool CheckIfFileExistsInDB(string filenameUrl, bool checkOriginalLinkFirst);

        bool Save();
    }
}
