using System;
using System.Globalization;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class BlogQueueIndicatorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values is null || values.Length < 2 ? "" : $"{((bool)values[0] || (ulong)values[1] == 0 ? "[F]" : "")}";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
