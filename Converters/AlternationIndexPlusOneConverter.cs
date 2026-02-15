using System;
using System.Globalization;
using System.Windows.Data;

namespace FacturationMercuriale.Converters
{
    public sealed class AlternationIndexPlusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
                return (i + 1).ToString(culture);

            return "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
