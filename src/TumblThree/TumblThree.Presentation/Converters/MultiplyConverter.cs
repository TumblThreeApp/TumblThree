using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return DependencyProperty.UnsetValue;

            if (value is double d && double.TryParse(parameter.ToString(), out double factor))
                return d * factor;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
