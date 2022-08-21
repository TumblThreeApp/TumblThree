using System;
using System.Globalization;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = ConverterHelper.IsParameterSet("invert", parameter);
            bool includeEmpty = ConverterHelper.IsParameterSet("includeEmpty", parameter);
            bool invertIncludeEmpty = ConverterHelper.IsParameterSet("invertIncludeEmpty", parameter);
            if (invertIncludeEmpty) { invert = includeEmpty = true; }

            return invert
                ? value == null || (includeEmpty && value is string s2 && s2.Length == 0)
                : value != null && (!includeEmpty || !(value is string s1) || s1.Length != 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
