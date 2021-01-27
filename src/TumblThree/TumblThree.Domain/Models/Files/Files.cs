using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Waf.Foundation;
using System.Xml;

namespace TumblThree.Domain.Models.Files
{
    [DataContract]
    public class Files : Model, IFiles
    {
        [DataMember(Name = "Links")]
        protected List<string> links;

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
            Version = "2";
            links = new List<string>();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public BlogTypes BlogType { get; set; }

        [DataMember]
        public string Version { get; set; }

        public IList<string> Links => links;

        public bool IsDirty { get { return isDirty; } }

        public void AddFileToDb(string fileName)
        {
            lock (_lockList)
            {
                Links.Add(fileName);
                isDirty = true;
            }
        }

        public virtual bool CheckIfFileExistsInDB(string filename)
        {
            string fileName = filename.Split('/').Last();
            Monitor.Enter(_lockList);
            try
            {
                return Links.Contains(fileName);
            }
            finally
            {
                Monitor.Exit(_lockList);
            }
        }

        public IFiles Load(string fileLocation)
        {
            try
            {
                isDirty = false;
                return LoadCore(fileLocation);
            }
            catch (Exception ex) when (ex is SerializationException || ex is FileNotFoundException)
            {
                ex.Data.Add("Filename", fileLocation);
                throw;
            }
        }

        private IFiles LoadCore(string fileLocation)
        {
            using (var stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new DataContractJsonSerializer(GetType());
                var file = (Files)serializer.ReadObject(stream);

                if (file.Version == "1")
                {
                    for (int i = 0; i < file.Links.Count; i++)
                    {
                        if (file.Links[i].Count(c => c == '_') == 2)
                        {
                            file.Links[i] = file.Links[i].Substring(file.Links[i].LastIndexOf('_') + 1);
                        }
                    }
                    file.Version = "2";
                    file.isDirty = true;
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
