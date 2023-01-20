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
        private const int MAX_SUPPORTED_DB_VERSION = 6;

        [DataMember(Name = "Links", IsRequired = false, EmitDefaultValue = false)]
        protected List<string> links;

        [DataMember(Name = "Entries")]
        protected HashSet<FileEntry> entries;

        private HashSet<string> originalLinks;

        protected bool isDirty;

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public Files()
        {
            // DO NOT USE
        }

        public Files(string name, string location)
        {
            Name = name;
            Location = location;
            Version = MAX_SUPPORTED_DB_VERSION.ToString();
            //links = new List<string>();
            entries = new HashSet<FileEntry>(new FileEntryComparer());
            originalLinks = new HashSet<string>();
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Location { get; set; }

        [DataMember]
        public BlogTypes BlogType { get; set; }

        [DataMember]
        public string Updates { get; set; }

        [DataMember]
        public string Version { get; set; }

        //public IList<string> Links => links;

        public IEnumerable<FileEntry> Entries => entries;

        public bool IsDirty { get { return isDirty; } }

        private static bool IsMatch(FileEntry x, string filename, string appendTemplate)
        {
            if (x.Filename == filename) return true;
            if (string.Compare(Path.GetExtension(x.Filename), Path.GetExtension(filename), StringComparison.InvariantCultureIgnoreCase) != 0) return false;
            var pattern = Regex.Escape(Path.GetFileNameWithoutExtension(filename) + appendTemplate).Replace("<0>", @"[\d]+");
            return Regex.IsMatch(Path.GetFileNameWithoutExtension(x.Filename), pattern);
        }

        public void AddFileToDb(string fileNameUrl, string fileNameOriginalUrl, string fileName)
        {
            _lock.EnterWriteLock();
            try
            {
                entries.Add(new FileEntry() { Link = fileNameUrl, OriginalLink = fileNameOriginalUrl, Filename = fileName });
                originalLinks.Add(fileNameOriginalUrl);
                isDirty = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public string AddFileToDb(string fileNameUrl, string fileNameOriginalUrl, string fileName, string appendTemplate)
        {
            _lock.EnterWriteLock();
            try
            {
                int n = entries.Count(x => IsMatch(x, fileName, appendTemplate));
                if (n > 0) fileName = Path.GetFileNameWithoutExtension(fileName) + appendTemplate.Replace("<0>", (n + 1).ToString()) + Path.GetExtension(fileName);

                entries.Add(new FileEntry() { Link = fileNameUrl, OriginalLink = fileNameOriginalUrl, Filename = fileName });
                originalLinks.Add(fileNameOriginalUrl);
                isDirty = true;
                return fileName;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void UpdateOriginalLink(string filenameUrl, string filenameOriginalUrl)
        {
            _lock.EnterWriteLock();
            try
            {
                var entry = entries.First(x => x.Link == filenameUrl);
                originalLinks.Remove(entry.OriginalLink);
                entry.OriginalLink = filenameOriginalUrl;
                originalLinks.Add(filenameOriginalUrl);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public virtual bool CheckIfFileExistsInDB(string filenameUrl, bool checkOriginalLinkFirst)
        {
            _lock.EnterReadLock();
            try
            {
                if (checkOriginalLinkFirst)
                {
                    if (originalLinks.Contains(filenameUrl)) return true;
                }
                return entries.Contains(new FileEntry() { Link = filenameUrl });
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public static IFiles Load(string fileLocation, bool isArchive = false)
        {
            try
            {
                IFiles file = LoadCore(fileLocation, isArchive);
                if (!isArchive && file.IsDirty) file.Save();
                return file;
            }
            catch (Exception ex) when (ex is SerializationException || ex is FileNotFoundException || ex is IOException)
            {
                ex.Data.Add("Filename", fileLocation);
                throw;
            }
        }

        private static IFiles LoadCore(string fileLocation, bool isArchive)
        {
            using (var stream = new FileStream(fileLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var serializer = new DataContractJsonSerializer(typeof(Files));
                var file = (Files)serializer.ReadObject(stream);
                file.entries = file.entries is null ? new HashSet<FileEntry>(new FileEntryComparer()) : new HashSet<FileEntry>(file.entries, new FileEntryComparer());

                if (!isArchive) DoUpdates(file);
                if (file.Version == "1")
                {
                    for (int i = 0; i < file.links.Count; i++)
                    {
                        if (!file.links[i].StartsWith("tumblr") && file.links[i].Count(c => c == '_') == 2)
                        {
                            file.links[i] = file.links[i].Substring(file.links[i].LastIndexOf('_') + 1);
                        }
                    }
                    file.Version = "2";
                    file.isDirty = true;
                }
                if (file.Version == "2")
                {
                    file.entries = new HashSet<FileEntry>(new FileEntryComparer());
                    for (int i = 0; i < file.links.Count; i++)
                    {
                        var fn = file.links[i];
                        file.entries.Add(new FileEntry() { Link = fn, Filename = fn });
                    }
                    file.Version = "3";
                    file.isDirty = true;
                    if (!Path.GetDirectoryName(fileLocation).Contains("\\Index\\Archive\\"))
                    {
                        var backupPath = Path.Combine(Path.GetDirectoryName(fileLocation), "backup");
                        var backupFilename = Path.Combine(backupPath, Path.GetFileName(fileLocation));
                        Directory.CreateDirectory(backupPath);
                        if (!File.Exists(backupFilename)) File.Copy(fileLocation, backupFilename);
                    }
                }
                if (file.Version == "3")
                {
                    // the cleanup 1 -> 2 destroyed valid links too, so clean up these orphaned links
                    var invalidChars = Path.GetInvalidFileNameChars();
                    var newList = new HashSet<FileEntry>(new FileEntryComparer());
                    if (file.entries != null)
                    {
                        foreach (var entry in file.entries)
                        {
                            var re = new Regex(@"^(1280|540|500|400|250|100|75sq|720|640)(\.[^.]*$|$)");
                            if (!re.IsMatch(entry.Link))
                                newList.Add(entry);
                        }
                    }
                    file.Version = "4";
                    file.isDirty = true;
                    file.entries = newList;
                }
                if (file.Version == "4")
                {
                    // cleanup wrong twitter links (incompletely fixing issue 231)
                    if (file.BlogType == BlogTypes.twitter && file.entries.Any(a => a.Link.Contains("?format=")))
                    {
                        foreach (var entry in file.entries)
                        {
                            var newLink = Regex.Replace(entry.Link, @"([^?]+)\?format=([\w]+)&name=[\w]+", "$1.$2");
                            if (newLink != entry.Link)
                            {
                                entry.Link = newLink;
                                file.isDirty = true;
                            }
                        }
                        if (file.isDirty) file.entries = new HashSet<FileEntry>(file.entries, new FileEntryComparer());
                    }
                    file.Version = "5";
                    file.isDirty = true;
                }
                if (file.Version == "5")
                {
                    // going back to older app version would drop OriginalLinks
                    file.Version = "6";
                    file.isDirty = true;
                }
                if (int.Parse(file.Version) > MAX_SUPPORTED_DB_VERSION)
                {
                    Logger.Error("{0}: DB version {1} not supported!", file.Name, file.Version);
                    throw new SerializationException($"{file.Name}: DB version {file.Version} not supported!");
                }

                foreach (var entry in file.entries)
                {
                    // remove erroneously added original links
                    if (file.BlogType == BlogTypes.newtumbl && entry.OriginalLink != null)
                    {
                        entry.OriginalLink = null;
                    }

                    file.originalLinks.Add(entry.OriginalLink);
                }

                file.Location = Path.Combine(Directory.GetParent(fileLocation).FullName);
                return file;
            }
        }

        private static void DoUpdates(Files file)
        {
            // T01
            if (!(file.Updates ?? "").Contains("T01") &&
                (file.BlogType == BlogTypes.tumblr || file.BlogType == BlogTypes.tmblrpriv) &&
                new string[] { "1", "2", "3", "4", "5" }.Contains(file.Version) &&
                Directory.Exists(file.Location))
            {
                if (new string[] { "4", "5" }.Contains(file.Version))
                {
                    foreach (var entry in file.entries.ToArray())
                    {
                        if (!entry.Filename.ToLower().EndsWith(".mp4")) { continue; }
                        var filepath = Path.Combine(file.Location.Replace("\\Index", ""), file.Name, entry.Filename);
                        if (!File.Exists(filepath)) { continue; }

                        var fi = new FileInfo(filepath);
                        var fileLength = fi.Length;
                        if (fi.Length <= 50 * 1024 && 
                            fi.CreationTime > new DateTime(2022, 4, 1))
                        {
                            bool redo = false;
                            if (fi.Length < 8)
                            {
                                redo = true;
                            }
                            else
                            {
                                using (var fs = File.OpenRead(filepath))
                                {
                                    byte[] ba = new byte[8];
                                    fs.Read(ba, 0, 8);

                                    if (ba[4] == 0x66 && ba[5] == 0x74 && ba[6] == 0x79 && ba[7] == 0x70) { continue; }
                                    if (ba[0] == 0x7B && ba[1] == 0x0D && ba[2] == 0x0A) { redo = true; }
                                }
                            }

                            if (redo)
                            {
                                File.Delete(filepath);
                                file.entries.Remove(entry);
                            }
                        }
                    }
                }
                file.Updates = (string.IsNullOrEmpty(file.Updates) ? "" : "|") + "T01";
                file.isDirty = true;
            }
        }

        public bool Save()
        {
            _lock.EnterWriteLock();
            try
            {
                string currentIndex = Path.Combine(Location, Name + "_files." + BlogType);
                string newIndex = Path.Combine(Location, Name + "_files." + BlogType + ".new");
                string backupIndex = Path.Combine(Location, Name + "_files." + BlogType + ".bak");

                if (File.Exists(currentIndex))
                {
                    Save(newIndex);

                    File.Replace(newIndex, currentIndex, backupIndex, true);
                    File.Delete(backupIndex);
                }
                else
                {
                    Save(currentIndex);
                }

                isDirty = false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Files:Save: {0}", ex);
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void Save(string path)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(
                    stream, Encoding.UTF8, true, true, "  "))
                {
                    var serializer = new DataContractJsonSerializer(GetType());
                    links = null;
                    serializer.WriteObject(writer, this);
                    writer.Flush();
                }
            }
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            _lock = new ReaderWriterLockSlim();
            originalLinks = new HashSet<string>();
        }
    }
}
