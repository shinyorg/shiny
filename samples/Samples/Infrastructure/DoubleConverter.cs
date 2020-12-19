using System;
using System.Globalization;
using Xamarin.Forms;


namespace Samples.Infrastructure
{
    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            return value;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var str = value as string;
            if (String.IsNullOrWhiteSpace(str))
                return null;

            if (Double.TryParse(str, out var dbl))
                return dbl;

            return null;
        }
    }
}
