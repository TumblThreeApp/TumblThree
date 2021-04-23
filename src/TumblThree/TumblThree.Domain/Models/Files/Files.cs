using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Waf.Foundation;
using System.Xml;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class Files : Model, IFiles
    {
        private const int MAX_SUPPORTED_DB_VERSION = 3;

        [DataMember(Name = "Links", IsRequired = false, EmitDefaultValue = false)]
        protected List<string> links;

        [DataMember(Name = "Entries")]
        protected List<FileEntry> entries;

        protected bool isDirty;

        private object _lockList = new object();

        public Files()
        {
            // DO NOT USE
        }

        public Files(string name, string location)
        {
            Name = name;
            Location = location;
            Version = "3";
            //links = new List<string>();
            entries = new List<FileEntry>();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public BlogTypes BlogType { get; set; }

        [DataMember]
        public string Version { get; set; }

        //public IList<string> Links => links;

        public bool IsDirty { get { return isDirty; } }

        private static bool IsMatch(FileEntry x, string filename, string appendTemplate)
        {
            if (x.Filename == filename) return true;
            if (string.Compare(Path.GetExtension(x.Filename), Path.GetExtension(filename), StringComparison.InvariantCultureIgnoreCase) != 0) return false;
            var pattern = Regex.Escape(Path.GetFileNameWithoutExtension(filename) + appendTemplate).Replace("<0>", @"[\d]+");
            return Regex.IsMatch(Path.GetFileNameWithoutExtension(x.Filename), pattern);
        }

        public void AddFileToDb(string fileNameUrl, string fileName)
        {
            lock (_lockList)
            {
                entries.Add(new FileEntry() { Link = fileNameUrl, Filename = fileName });
                isDirty = true;
            }
        }

        public string AddFileToDb(string fileNameUrl, string fileName, string appendTemplate)
        {
            lock (_lockList)
            {
                int n = entries.Count(x => IsMatch(x, fileName, appendTemplate));
                if (n > 0) fileName = Path.GetFileNameWithoutExtension(fileName) + appendTemplate.Replace("<0>", (n + 1).ToString()) + Path.GetExtension(fileName);

                entries.Add(new FileEntry() { Link = fileNameUrl, Filename = fileName });
                isDirty = true;
                return fileName;
            }
        }

        public virtual bool CheckIfFileExistsInDB(string filenameUrl)
        {
            //string fileName = filenameUrl.Split('/').Last();
            Monitor.Enter(_lockList);
            try
            {
                return entries.Any(x => x.Link.Equals(filenameUrl));
            }
            finally
            {
                Monitor.Exit(_lockList);
            }
        }

        public static IFiles Load(string fileLocation)
        {
            try
            {
                //isDirty = false;
                return LoadCore(fileLocation);
            }
            catch (Exception ex) when (ex is SerializationException || ex is FileNotFoundException)
            {
                ex.Data.Add("Filename", fileLocation);
                throw;
            }
        }

        private static IFiles LoadCore(string fileLocation)
        {
            using (var stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new DataContractJsonSerializer(typeof(Files));
                var file = (Files)serializer.ReadObject(stream);

                if (file.Version == "1")
                {
                    for (int i = 0; i < file.links.Count; i++)
                    {
                        if (file.links[i].Count(c => c == '_') == 2)
                        {
                            file.links[i] = file.links[i].Substring(file.links[i].LastIndexOf('_') + 1);
                        }
                    }
                    file.Version = "2";
                    file.isDirty = true;
                }
                if (file.Version == "2")
                {
                    file.entries = new List<FileEntry>();
                    for (int i = 0; i < file.links.Count; i++)
                    {
                        var fn = file.links[i];
                        file.entries.Add(new FileEntry() { Link = fn, Filename = fn });
                    }
                    file.Version = "3";
                    file.isDirty = true;
                    var backupPath = Path.Combine(Path.GetDirectoryName(fileLocation), "backup");
                    var backupFilename = Path.Combine(backupPath, Path.GetFileName(fileLocation));
                    Directory.CreateDirectory(backupPath);
                    if (!File.Exists(backupFilename)) File.Copy(fileLocation, backupFilename);
                }
                if (int.Parse(file.Version) > MAX_SUPPORTED_DB_VERSION)
                {
                    Logger.Error("{0}: DB version {1} not supported!", file.Name, file.Version);
                    throw new SerializationException($"{file.Name}: DB version {file.Version} not supported!");
                }

                file.Location = Path.Combine(Directory.GetParent(fileLocation).FullName);
                return file;
            }
        }

        public bool Save()
        {
            lock (_lockList)
            {
                string currentIndex = Path.Combine(Location, Name + "_files." + BlogType);
                string newIndex = Path.Combine(Location, Name + "_files." + BlogType + ".new");
                string backupIndex = Path.Combine(Location, Name + "_files." + BlogType + ".bak");

                try
                {
                    if (File.Exists(currentIndex))
                    {
                        SaveBlog(newIndex);

                        File.Replace(newIndex, currentIndex, backupIndex, true);
                        File.Delete(backupIndex);
                    }
                    else
                    {
                        SaveBlog(currentIndex);
                    }

                    isDirty = false;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Files:Save: {0}", ex);
                    throw;
                }
            }
        }

        private void SaveBlog(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(GetType());
                    this.links = null;
                    serializer.WriteObject(writer, this);
                    writer.Flush();
                }
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            _lockList = new object();
        }
    }
}
