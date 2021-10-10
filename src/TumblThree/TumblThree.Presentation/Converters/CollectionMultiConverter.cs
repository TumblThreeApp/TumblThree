using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using TumblThree.Domain.Models;

namespace TumblThree.Presentation.Converters
{
    public class CollectionMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values[0] == null || values[1] == null) return null;

            var CollectionId = (int)values[0];
            var collections = (List<Collection>)values[1];

            return collections.Where(x => x.Id == CollectionId).Select(s => s.Name).FirstOrDefault();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
