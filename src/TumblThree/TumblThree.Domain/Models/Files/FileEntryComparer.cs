using System;
using System.Collections.Generic;

namespace TumblThree.Domain.Models.Files
{
    public class FileEntryComparer : IEqualityComparer<FileEntry>
    {
        public bool Equals(FileEntry x, FileEntry y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Link == y.Link;
        }

        public int GetHashCode(FileEntry obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return obj.Link.GetHashCode();
        }
    }
}
