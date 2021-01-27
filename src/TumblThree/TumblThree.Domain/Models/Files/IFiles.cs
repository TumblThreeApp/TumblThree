using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Domain.Models.Files
{
    public interface IFiles : INotifyPropertyChanged
    {
        string Name { get; }

        BlogTypes BlogType { get; }

        IList<string> Links { get; }

        bool IsDirty { get; }

        string Version { get; set; }

        void AddFileToDb(string fileName);

        bool CheckIfFileExistsInDB(string filename);

        bool Save();

        IFiles Load(string fileLocation);
    }
}
