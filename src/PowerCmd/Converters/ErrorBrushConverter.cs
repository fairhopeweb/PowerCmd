using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PowerCmd.Converters
{
    public class ErrorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && (bool)value == true)
                return new SolidColorBrush(Colors.Red);
            return new SolidColorBrush(Colors.Green);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}