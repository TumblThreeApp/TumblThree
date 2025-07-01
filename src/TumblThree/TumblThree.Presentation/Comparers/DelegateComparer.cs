using System;
using System.Collections;
using System.ComponentModel;

namespace TumblThree.Presentation.Comparers
{
    public class DelegateComparer : IComparer
    {
        private readonly Func<object, object> _extractor;
        private readonly ListSortDirection _direction;

        public DelegateComparer(Func<object, object> extractor, ListSortDirection direction)
        {
            _extractor = extractor;
            _direction = direction;
        }

        public int Compare(object x, object y)
        {
            var valueX = _extractor(x);
            var valueY = _extractor(y);
            int result;
            if (valueX is string)
            {
                result = string.Compare((string)valueX, (string)valueY, StringComparison.OrdinalIgnoreCase);
            }
            else if (valueX is int)
            {
                result = ((int)valueX).CompareTo((int)valueY);
            }
            else
            {
                throw new ArgumentException("Unsupported data type.");
            }
            
            return _direction == ListSortDirection.Ascending ? result : -result;
        }
    }
}
