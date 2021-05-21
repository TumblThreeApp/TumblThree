using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TumblThree.Presentation.Converters
{
    public class BrushResourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (string) value;
            return Application.Current.Resources.MergedDictionaries.Where(r => r.Source.ToString() == "Resources/BrushResources.xaml").FirstOrDefault()[s];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
