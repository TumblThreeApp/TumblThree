using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IGlobalDatabaseService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class GlobalDatabaseService : IGlobalDatabaseService, IDisposable
    {
        readonly IShellService _shellService;

        [ThreadStatic]
        private static MD5 _md5;

        private SQLiteConnection _connection;
        private bool? _DbExisted;
        private bool _disposed;

        [ImportingConstructor]
        public GlobalDatabaseService(ShellService shellService)
        {
            _shellService = shellService;
        }

        public bool DbExisted { get { return _DbExisted.Value; } }

        private async Task PrepareGlobalDatabaseAsync(bool deleteOldDb = false)
        {
            if (_DbExisted.HasValue && !deleteOldDb) { return; }
            try
            {
                var path = Path.Combine(_shellService.Settings.DownloadLocation, "globaldatabase.sqlite");
                if (_DbExisted.GetValueOrDefault())
                {
                    _connection?.Close();
                    _connection = null;
                    Thread.Sleep(1000);
                    File.Delete(path);
                }
                _DbExisted = File.Exists(path);
                _connection = new SQLiteConnection($@"Data Source={path}");
                SQLiteFunction.RegisterFunction(typeof(RegExSQLiteFunction));
                _connection.Open();
                // use WAL Mode for concurrency, no-journaling for initial copying
                var pragmas = DbExisted ? "PRAGMA journal_mode=WAL; PRAGMA synchronous = NORMAL;" : "PRAGMA journal_mode=OFF; PRAGMA synchronous = OFF;";
                using (var command = new SQLiteCommand(pragmas, _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                // optimize PRAGMA settings
                using (var command = new SQLiteCommand("PRAGMA locking_mode = EXCLUSIVE;" +
                    "PRAGMA cache_size = 10000;" +
                    "PRAGMA page_size = 4096;" +
                    "PRAGMA temp_store = MEMORY;", _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                string sql = "CREATE TABLE IF NOT EXISTS BlogFiles (BlogId INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, BlogType INT NOT NULL, Location TEXT NOT NULL, IsArchive INT NOT NULL);" +
                    "CREATE UNIQUE INDEX IF NOT EXISTS idx_BlogFiles_NameBlogType ON BlogFiles (Name, BlogType)";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                sql = "CREATE TABLE IF NOT EXISTS FileEntries (BlogId INT, HashLink VARCHAR(32) NOT NULL, Link TEXT NOT NULL, Filename TEXT NULL, HashOriginalLink VARCHAR(32) NULL, OriginalLink TEXT NULL);" +
                    "CREATE UNIQUE INDEX IF NOT EXISTS idx_FileEntries_HashLink ON FileEntries (HashLink);" + (DbExisted ? 
                    "CREATE INDEX IF NOT EXISTS idx_FileEntries_HashOriginalLink ON FileEntries (HashOriginalLink);" +
                    "CREATE INDEX IF NOT EXISTS idx_FileEntries_BlogId ON FileEntries (BlogId)" : "");
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task Init(bool deleteOldDb = false)
        {
            await PrepareGlobalDatabaseAsync(deleteOldDb);
        }

        public async Task PostInit()
        {
            if (!DbExisted)
            {
                using (var command = new SQLiteCommand("PRAGMA journal_mode=WAL; PRAGMA synchronous = NORMAL;", _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                var sql = "CREATE INDEX IF NOT EXISTS idx_FileEntries_HashOriginalLink ON FileEntries (HashOriginalLink);" +
                    "CREATE INDEX IF NOT EXISTS idx_FileEntries_BlogId ON FileEntries (BlogId)";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task AddFileToDb(string blogName, BlogTypes blogType, string fileNameUrl, string fileNameOriginalUrl, string fileName)
        {
            try
            {
                var blogId = AddFileEntry_Blog(blogName, blogType, "", false);

                string sql = "INSERT OR IGNORE INTO FileEntries(BlogId, HashLink, Link, Filename, HashOriginalLink, OriginalLink) VALUES (@BlogId, @HashLink, @Link, @Filename, @HashOriginalLink, @OriginalLink)";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    SetParameterValues(command.Parameters, blogId, fileNameUrl, fileNameOriginalUrl, fileName, true);
                    await command.ExecuteNonQueryAsync();
                }
                return;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<string> AddFileToDb(string blogName, BlogTypes blogType, string fileNameUrl, string fileNameOriginalUrl, string fileName, string appendTemplate)
        {
            try
            {
                var blogId = AddFileEntry_Blog(blogName, blogType, "", false);

                var pattern = Regex.Escape(Path.GetFileNameWithoutExtension(fileName) + appendTemplate).Replace("<0>", @"[\d]+") + Path.GetExtension(fileName);
                var sql = $"select count(*) from FileEntries where BlogId = {blogId} AND Filename = '{fileName}' OR Filename REGEXP '{pattern}'";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    long n = (long)await command.ExecuteScalarAsync();
                    if (n > 0) fileName = Path.GetFileNameWithoutExtension(fileName) + appendTemplate.Replace("<0>", (n + 1).ToString()) + Path.GetExtension(fileName);
                }

                sql = "INSERT OR IGNORE INTO FileEntries(BlogId, HashLink, Link, Filename, HashOriginalLink, OriginalLink) VALUES (@BlogId, @HashLink, @Link, @Filename, @HashOriginalLink, @OriginalLink)";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    SetParameterValues(command.Parameters, blogId, fileNameUrl, fileNameOriginalUrl, fileName, true);
                    await command.ExecuteNonQueryAsync();
                }

                return fileName;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static void SetParameter(SQLiteParameterCollection parameters, string name, object value)
        {
            if (parameters.Contains(name))
                parameters[name].Value = value;
            else
                parameters.AddWithValue(name, value);
        }

        private void SetParameterValues(SQLiteParameterCollection parameters, int blogId, string fileNameUrl, string fileNameOriginalUrl, string fileName, bool cleanup)
        {
            if (cleanup)
            {
                fileName = fileName == fileNameUrl ? null : fileName;
                fileNameOriginalUrl = (string.IsNullOrEmpty(fileNameOriginalUrl) || fileNameOriginalUrl == fileNameUrl) ? null : fileNameOriginalUrl;
            }
            var hashFileNameUrl = GetHash(fileNameUrl);
            var hashFileNameOriginalUrl = GetHash(fileNameOriginalUrl);
            SetParameter(parameters, "@BlogId", blogId);
            SetParameter(parameters, "@Link", fileNameUrl);
            SetParameter(parameters, "@HashLink", hashFileNameUrl);
            SetParameter(parameters, "@Filename", fileName);
            SetParameter(parameters, "@OriginalLink", fileNameOriginalUrl);
            SetParameter(parameters, "@HashOriginalLink", hashFileNameOriginalUrl);
        }

        private int AddFileEntry_Blog(string name, BlogTypes blogType, string location, bool isArchiv)
        {
            try
            {
                int blogId;

                string sql = "INSERT OR IGNORE INTO BlogFiles(Name, BlogType, Location, IsArchive) VALUES (@Name, @BlogType, @Location, @IsArchive)";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@BlogType", (int)blogType);
                    command.Parameters.AddWithValue("@Location", location);
                    command.Parameters.AddWithValue("@IsArchive", isArchiv ? 1 : 0);
                    int affected = command.ExecuteNonQuery();
                    if (affected > 0)
                    {
                        blogId = (int)_connection.LastInsertRowId;
                    }
                    else
                    {
                        command.CommandText = "SELECT BlogId FROM BlogFiles WHERE Name = @Name AND BlogType = @BlogType";
                        blogId = Convert.ToInt32(command.ExecuteScalar());
                    }
                }
                return blogId;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void AddFileEntry_File(int blogId, IFiles files)
        {
            try
            {
                string sql = "INSERT OR IGNORE INTO FileEntries(BlogId, HashLink, Link, Filename, HashOriginalLink, OriginalLink) VALUES (@BlogId, @HashLink, @Link, @Filename, @HashOriginalLink, @OriginalLink)";
                using (var command = new SQLiteCommand(sql, _connection))
                {
                    foreach (var entry in files.Entries)
                    {
                        SetParameterValues(command.Parameters, blogId, entry.Link, entry.OriginalLink, entry.Filename, false);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task AddFileEntriesAsync(IFiles files, bool isArchive)
        {
            if (DbExisted)
            {
                return;
            }

            SQLiteTransaction transaction = null;
            try
            {
                int blogId;

                transaction = _connection.BeginTransaction();

                blogId = AddFileEntry_Blog(files.Name, files.BlogType, files.Location, isArchive);

                AddFileEntry_File(blogId, files);

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Logger.Error("GlobalDatabaseService.AddFileEntriesAsync: {0}", ex);
                if (transaction != null) transaction.Rollback();
            }
            await Task.CompletedTask;
        }

        public async Task<bool> CheckIfFileExistsAsync(string filenameUrl, bool checkOriginalLinkFirst, bool checkArchive)
        {
            try
            {
                string query;
                var hashedFilenameUrl = GetHash(filenameUrl);
                if (checkOriginalLinkFirst)
                {
                    query = checkArchive ?
                        "SELECT EXISTS(SELECT 1 FROM FileEntries WHERE HashOriginalLink = @value LIMIT 1);" :
                        "SELECT EXISTS(SELECT 1 FROM FileEntries fe INNER JOIN BlogFiles bf ON fe.BlogId = bf.BlogId WHERE HashOriginalLink = @value AND IsArchive=0 LIMIT 1);";
                    using (var command = new SQLiteCommand(query, _connection))
                    {
                        command.Parameters.AddWithValue("@value", hashedFilenameUrl);
                        bool exists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                        if (exists) return exists;
                    }
                }
                query = checkArchive ?
                    "SELECT EXISTS(SELECT 1 FROM FileEntries WHERE HashLink = @value);" :
                    "SELECT EXISTS(SELECT 1 FROM FileEntries fe INNER JOIN BlogFiles bf ON fe.BlogId=bf.BlogId WHERE HashLink = @value AND IsArchive=0);";
                using (var command = new SQLiteCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@value", hashedFilenameUrl);
                    bool exists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                    return exists;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        public async Task RemoveFileEntriesAsync(string name, int blogType, bool isArchive)
        {
            try
            {
                var where = name != null && blogType != -1 ? $"Name = @name AND BlogType = {blogType} AND " : "";
                var sql = $"DELETE FROM FileEntries WHERE BlogId IN (SELECT BlogId FROM BlogFiles WHERE {where}IsArchive = {(isArchive ? "1" : "0")})";
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    if (!string.IsNullOrEmpty(where)) cmd.Parameters.AddWithValue("@name", name);
                    await cmd.ExecuteNonQueryAsync();
                }
                sql = $"DELETE FROM BlogFiles WHERE {where}IsArchive = {(isArchive ? "1" : "0")}";
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    if (!string.IsNullOrEmpty(where)) cmd.Parameters.AddWithValue("@name", name);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task ClearFileEntries(bool isArchive)
        {
            //await RemoveFileEntriesAsync(null, -1, isArchive);
            await Task.CompletedTask;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        public async Task UpdateOriginalLink(string blogName, int blogType, string filenameUrl, string filenameOriginalUrl)
        {
            try
            {
                string hash;
                var sql = $"SELECT HashOriginalLink FROM FileEntries WHERE Link = @link AND BlogId IN (SELECT BlogId FROM BlogFiles WHERE Name = @name AND BlogType = {blogType}) LIMIT 1";
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@link", filenameUrl);
                    cmd.Parameters.AddWithValue("@name", blogName);
                    hash = (string)await cmd.ExecuteScalarAsync();
                }
                if (hash is null)
                {
                    await AddFileToDb(blogName, (BlogTypes)blogType, filenameUrl, filenameOriginalUrl, null);
                }
                else
                {
                    sql = $"UPDATE FileEntries SET OriginalLink = @originalLink, HashOriginalLink = @hashOriginalLink " +
                        $"WHERE Link = @link AND BlogId IN (SELECT BlogId FROM BlogFiles WHERE Name = @name AND BlogType = {blogType})";
                    var hashFilenameOriginalUrl = GetHash(filenameOriginalUrl);
                    using (var cmd = new SQLiteCommand(sql, _connection))
                    {
                        cmd.Parameters.AddWithValue("@link", filenameUrl);
                        cmd.Parameters.AddWithValue("@originalLink", filenameOriginalUrl);
                        cmd.Parameters.AddWithValue("@hashOriginalLink", hashFilenameOriginalUrl);
                        cmd.Parameters.AddWithValue("@name", blogName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static string GetHash(string fileNameUrl)
        {
            if (fileNameUrl is null) return null;

            string normalized = fileNameUrl.ToUpperInvariant().Normalize(NormalizationForm.FormC);

            _md5 = _md5 ?? MD5.Create();
            return BitConverter.ToString(_md5.ComputeHash(Encoding.UTF8.GetBytes(normalized))).Replace("-", "").ToLowerInvariant();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
                    _md5?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    [SQLiteFunction(Name = "REGEXP", Arguments = 2, FuncType = FunctionType.Scalar)]
    internal class RegExSQLiteFunction : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            if (args.Length != 2) return false;
            string pattern = Convert.ToString(args[0]);
            string input = Convert.ToString(args[1]);
            return Regex.IsMatch(input, pattern);
        }
    }
}
