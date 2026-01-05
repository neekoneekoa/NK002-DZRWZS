using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiaryApp
{
    public class PriorityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int priority)
            {
                return priority switch
                {
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF7675")),
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDCB6E")),
                    3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00B894")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF636E72"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF636E72"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
