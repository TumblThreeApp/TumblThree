using System;
using System.IO;

namespace TumblThree.Applications.Downloader
{
    internal sealed class StreamWriterWithInfo : IDisposable
    {
        private readonly StreamWriter _sw;
        private readonly bool isJson;
        private bool hasElements;

        public StreamWriterWithInfo(string path, bool append, bool isJson)
        {
            this.isJson = isJson;
            if (!File.Exists(path))
            {
                _sw = new StreamWriter(path, append);
                if (isJson) _sw.WriteLine("[");
            }
            else
            {
                var fi = new FileInfo(path);
                if (fi.Length > 6) hasElements = true;
                var sr = new StreamReader(path);
                var line = sr.ReadLine() ?? "";
                sr.Close();
                var hasStartElement = line.TrimStart().StartsWith("[");
                var tmpPath = path + ".temp";
                if (isJson && line.Length > 0 && !hasStartElement)
                {
                    File.Move(path, tmpPath);
                }
                _sw = new StreamWriter(new FileStream(path, FileMode.OpenOrCreate));
                if (append) _sw.BaseStream.Seek(0, SeekOrigin.End);
                if (isJson && !hasStartElement) _sw.WriteLine("[");
                if (isJson && line.Length > 0)
                {
                    if (hasStartElement)
                    {
                        _sw.BaseStream.Seek(-5, SeekOrigin.End);
                        _sw.WriteLine(",");
                    }
                    else
                    {
                        var content = File.ReadAllText(tmpPath);
                        content = content.TrimEnd(',', '\r', '\n') + ",";
                        _sw.WriteLine(content);
                        _sw.Flush();
                        File.Delete(tmpPath);
                    }
                }
            }
        }

        public void WriteLine(string text)
        {
            if (isJson) text = $"{text},";
            hasElements = true;
            _sw.WriteLine(text);
        }

        public void Dispose()
        {
            if (isJson)
            {
                if (hasElements)
                {
                    _sw.Flush();
                    _sw.BaseStream.Seek(-3, SeekOrigin.End);
                }
                _sw.WriteLine(hasElements ? "\n]" : "]");
            }
            _sw.Dispose();
        }
    }
}
