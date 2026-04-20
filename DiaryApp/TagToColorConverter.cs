using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiaryApp
{
    public class TagToColorConverter : IValueConverter
    {
        private static readonly List<string> _colors = new()
        {
            "#6C5CE7",
            "#0984E3",
            "#00B894",
            "#E17055",
            "#D63031",
            "#FD79A8",
            "#00CEC9",
            "#FDCB6E",
            "#636E72",
            "#2D3436",
            "#E84393",
            "#2D98DA",
            "#20BF6B",
            "#F7B731",
            "#FA8231",
            "#EB3B5A",
            "#4B7BEC",
            "#A55EEA",
            "#778CA3",
            "#4B6584"
        };

        private static readonly Dictionary<string, SolidColorBrush> _cache = new(StringComparer.OrdinalIgnoreCase);

        public static SolidColorBrush GetColorBrush(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return Brushes.Transparent;
            }

            tag = tag.Trim();
            if (_cache.TryGetValue(tag, out var brush))
            {
                return brush;
            }

            try
            {
                var colorCode = _colors[(int)(ComputeStableHash(tag) % (uint)_colors.Count)];
                var color = (Color)ColorConverter.ConvertFromString(colorCode);
                var newBrush = new SolidColorBrush(color);
                newBrush.Freeze();
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

        private static uint ComputeStableHash(string text)
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            uint hash = offsetBasis;
            foreach (var ch in text.ToLowerInvariant())
            {
                hash ^= ch;
                hash *= prime;
            }

            return hash;
        }
    }
}
