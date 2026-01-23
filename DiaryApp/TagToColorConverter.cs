using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiaryApp
{
    public class TagToColorConverter : IValueConverter
    {
        private static readonly List<string> _colors = new List<string>
        {
            "#6C5CE7", // Purple
            "#0984E3", // Blue
            "#00B894", // Green
            "#E17055", // Orange
            "#D63031", // Red
            "#FD79A8", // Pink
            "#00CEC9", // Teal
            "#FDCB6E", // Yellow
            "#636E72", // Grey
            "#2D3436", // Dark Grey
            "#e84393", // Prunus Avium
            "#2d98da", // Boyzone
            "#20bf6b", // Emerald
            "#f7b731", // NYC Taxi
            "#fa8231", // Tangerine
            "#eb3b5a", // Desire
            "#4b7bec", // Royal Blue
            "#a55eea", // Royal Purple
            "#778ca3", // Metal Blue
            "#4b6584"  // Blue Horizon
        };

        private static readonly Dictionary<string, SolidColorBrush> _cache = new Dictionary<string, SolidColorBrush>();

        public static SolidColorBrush GetColorBrush(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return Brushes.Transparent;

            if (_cache.TryGetValue(tag, out var brush))
            {
                return brush;
            }

            // Use simple hashing to pick a color
            int index = Math.Abs(tag.GetHashCode()) % _colors.Count;
            var colorCode = _colors[index];
            
            try 
            {
                var color = (Color)ColorConverter.ConvertFromString(colorCode);
                var newBrush = new SolidColorBrush(color);
                newBrush.Freeze(); // Freeze for performance
                _cache[tag] = newBrush;
                return newBrush;
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tag)
            {
                return GetColorBrush(tag);
            }
            
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
