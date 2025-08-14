using System;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Files;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IGlobalDatabaseService))]
    [Export]
    internal class GlobalDatabaseService : IGlobalDatabaseService, IDisposable
    {
        IShellService _shellService;

        private SQLiteConnection _connection;
        private bool _disposed;

        [ImportingConstructor]
        public GlobalDatabaseService(ShellService shellService)
        {
            _shellService = shellService;
            _ = PrepareGlobalDatabaseAsync();
        }

        public async Task PrepareGlobalDatabaseAsync()
        {
            var path = Path.Combine(_shellService.Settings.DownloadLocation, "globaldatabase.sqlite");
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                throw;
            }
            _connection = new SQLiteConnection($@"Data Source={path}");
            SQLiteFunction.RegisterFunction(typeof(RegExSQLiteFunction));
            _connection.Open();
            string sql = "CREATE TABLE IF NOT EXISTS BlogFiles (BlogId INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, BlogType INT NOT NULL, Location TEXT NOT NULL, IsArchive INT NOT NULL);" +
                "CREATE UNIQUE INDEX idx_BlogFiles_NameBlogType ON BlogFiles (Name, BlogType)";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            sql = "CREATE TABLE IF NOT EXISTS FileEntries (BlogId INT, Link TEXT NOT NULL, Filename TEXT NULL, OriginalLink TEXT NULL);" +
                "CREATE UNIQUE INDEX idx_FileEntries_Link ON FileEntries (Link);" +
                "CREATE INDEX idx_FileEntries_OriginalLink ON FileEntries (OriginalLink)";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task AddFileToDb(string blogName, BlogTypes blogType, string fileNameUrl, string fileNameOriginalUrl, string fileName)
        {
            var blogId = AddFileEntry_Blog(blogName, blogType, "", false);

            string sql = "INSERT OR IGNORE INTO FileEntries(BlogId, Link, Filename, OriginalLink) VALUES (@BlogId, @Link, @Filename, @OriginalLink)";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                SetParameterValues(command.Parameters, blogId, fileNameUrl, fileNameOriginalUrl, fileName, true);
                await command.ExecuteNonQueryAsync();
            }
            return;
        }

        public async Task<string> AddFileToDb(string blogName, BlogTypes blogType, string fileNameUrl, string fileNameOriginalUrl, string fileName, string appendTemplate)
        {
            var blogId = AddFileEntry_Blog(blogName, blogType, "", false);

            var pattern = Regex.Escape(Path.GetFileNameWithoutExtension(fileName) + appendTemplate).Replace("<0>", @"[\d]+") + Path.GetExtension(fileName);
            var sql = $"select count(*) from FileEntries where BlogId = {blogId} AND Filename = '{fileName}' OR Filename REGEXP '{pattern}'";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                long n = (long)await command.ExecuteScalarAsync();
                if (n > 0) fileName = Path.GetFileNameWithoutExtension(fileName) + appendTemplate.Replace("<0>", (n + 1).ToString()) + Path.GetExtension(fileName);
            }

            sql = "INSERT OR IGNORE INTO FileEntries(BlogId, Link, Filename, OriginalLink) VALUES (@BlogId, @Link, @Filename, @OriginalLink)";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                SetParameterValues(command.Parameters, blogId, fileNameUrl, fileNameOriginalUrl, fileName, true);
                await command.ExecuteNonQueryAsync();
            }
            return fileName;
        }

        private static void SetParameter(SQLiteParameterCollection parameters, string name, object value)
        {
            if (parameters.Contains(name))
                parameters[name].Value = value;
            else
                parameters.AddWithValue(name, value);
        }

        private static void SetParameterValues(SQLiteParameterCollection parameters, int blogId, string fileNameUrl, string fileNameOriginalUrl, string fileName, bool cleanup)
        {
            if (cleanup)
            {
                fileName = fileName == fileNameUrl ? null : fileName;
                fileNameOriginalUrl = (string.IsNullOrEmpty(fileNameOriginalUrl) || fileNameOriginalUrl == fileNameUrl) ? null : fileNameOriginalUrl;
            }
            SetParameter(parameters, "@BlogId", blogId);
            SetParameter(parameters, "@Link", fileNameUrl);
            SetParameter(parameters, "@Filename", fileName);
            SetParameter(parameters, "@OriginalLink", fileNameOriginalUrl);
        }

        private int AddFileEntry_Blog(string name, BlogTypes blogType, string location, bool isArchiv)
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

        private void AddFileEntry_File(int blogId, IFiles files)
        {
            string sql = "INSERT OR IGNORE INTO FileEntries(BlogId, Link, Filename, OriginalLink) VALUES (@BlogId, @Link, @Filename, @OriginalLink)";
            using (var command = new SQLiteCommand(sql, _connection))
            {
                foreach (var entry in files.Entries)
                {
                    SetParameterValues(command.Parameters, blogId, entry.Link, entry.OriginalLink, entry.Filename, false);
                    command.ExecuteNonQuery();
                }
            }
        }

        public async Task AddFileEntriesAsync(IFiles files, bool isArchive)
        {
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
            string query;
            if (checkOriginalLinkFirst)
            {
                query = checkArchive ?
                    "SELECT EXISTS(SELECT 1 FROM FileEntries WHERE IFNULL(OriginalLink, Link) = @value);" :
                    "SELECT EXISTS(SELECT 1 FROM FileEntries fe INNER JOIN BlogFiles bf ON fe.BlogId = bf.BlogId WHERE IFNULL(OriginalLink, Link) = @value AND IsArchive=0);";
                using (var command = new SQLiteCommand(query, _connection))
                {
                    command.Parameters.AddWithValue("@value", filenameUrl);
                    bool exists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                    if (exists) return exists;
                }
            }
            query = checkArchive ?
                "SELECT EXISTS(SELECT 1 FROM FileEntries WHERE Link = @value);" :
                "SELECT EXISTS(SELECT 1 FROM FileEntries fe INNER JOIN BlogFiles bf ON fe.BlogId=bf.BlogId WHERE Link = @value AND IsArchive=0);";
            using (var command = new SQLiteCommand(query, _connection))
            {
                command.Parameters.AddWithValue("@value", filenameUrl);
                bool exists = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                return exists;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        public async Task RemoveFileEntriesAsync(string name, int blogType, bool isArchive)
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

        public async Task ClearFileEntries(bool isArchive)
        {
            await RemoveFileEntriesAsync(null, -1, isArchive);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
        public async Task UpdateOriginalLink(string blogName, int blogType, string filenameUrl, string filenameOriginalUrl)
        {
            string link;
            var sql = $"SELECT Link FROM FileEntries WHERE Link = @link AND BlogId IN (SELECT BlogId FROM BlogFiles WHERE Name = @name AND BlogType = {blogType}) LIMIT 1";
            using (var cmd = new SQLiteCommand(sql, _connection))
            {
                cmd.Parameters.AddWithValue("@link", filenameUrl);
                cmd.Parameters.AddWithValue("@name", blogName);
                link = (string)await cmd.ExecuteScalarAsync();
            }
            if (link is null)
            {
                await AddFileToDb(blogName, (BlogTypes)blogType, filenameUrl, filenameOriginalUrl, null);
            }
            else
            {
                sql = $"UPDATE FileEntries SET OriginalLink = @originalLink WHERE Link = @link AND BlogId IN (SELECT BlogId FROM BlogFiles WHERE Name = @name AND BlogType = {blogType})";
                using (var cmd = new SQLiteCommand(sql, _connection))
                {
                    cmd.Parameters.AddWithValue("@link", filenameUrl);
                    cmd.Parameters.AddWithValue("@originalLink", filenameOriginalUrl);
                    cmd.Parameters.AddWithValue("@name", blogName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
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
