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

        void AddFileToDb(string fileNameUrl, string fileName);

        string AddFileToDb(string fileNameUrl, string fileName, string appendTemplate);

        bool CheckIfFileExistsInDB(string filenameUrl);

        bool Save();
    }
}
