using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace TumblThree.Presentation.Comparers
{
    public class StableComparer : IComparer
    {
        private readonly Dictionary<object, int> _currentOrder;
        private readonly List<SortDescription> _sortDescriptions;
        private readonly Func<object, string> _getCollectionName;
        private readonly Func<object, object> _getProgressValue;

        public StableComparer(IEnumerable items, IEnumerable<SortDescription> sortDescriptions,
            Func<object, string> getCollectionName = null, Func<object, object> getProgressValue = null)
        {
            _currentOrder = new Dictionary<object, int>();
            int index = 0;
            foreach (var item in items)
            {
                _currentOrder[item] = index++;
            }
            _sortDescriptions = new List<SortDescription>(sortDescriptions);
            _getCollectionName = getCollectionName;
            _getProgressValue = getProgressValue;
        }

        public int Compare(object x, object y)
        {
            foreach (var sortDescription in _sortDescriptions)
            {
                int result = CompareProperty(x, y, sortDescription);
                if (result != 0)
                    return result;
            }

            var currentX = _currentOrder.TryGetValue(x, out int indexX) ? indexX : int.MaxValue;
            var currentY = _currentOrder.TryGetValue(y, out int indexY) ? indexY : int.MaxValue;
            return currentX.CompareTo(currentY);
        }

        private int CompareProperty(object x, object y, SortDescription sortDescription)
        {
            var valueX = GetPropertyValue(x, sortDescription.PropertyName);
            var valueY = GetPropertyValue(y, sortDescription.PropertyName);

            int result = CompareValues(valueX, valueY);

            return sortDescription.Direction == ListSortDirection.Ascending ? result : -result;
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null) return null;

            if (propertyName == "__collection")
            {
                return _getCollectionName?.Invoke(obj);
            }
            else if (propertyName == "__progress")
            {
                return _getProgressValue?.Invoke(obj);
            }
            else
            {
                var property = obj.GetType().GetProperty(propertyName);
                return property?.GetValue(obj);
            }
        }

        private static int CompareValues(object valueX, object valueY)
        {
            if (valueX == null && valueY == null) return 0;
            if (valueX == null) return -1;
            if (valueY == null) return 1;

            if (valueX is string strX && valueY is string strY)
            {
                return string.Compare(strX, strY, StringComparison.OrdinalIgnoreCase);
            }
            else if (valueX is IComparable comparableX)
            {
                return comparableX.CompareTo(valueY);
            }
            else if (valueX.Equals(valueY))
            {
                return 0;
            }

            return string.Compare(valueX.ToString(), valueY.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}