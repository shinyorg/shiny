using System;
using System.Globalization;
using Xamarin.Forms;


namespace Samples.Infrastructure
{
    public class PercentToDecimalValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var r = Math.Round((decimal)value / 100, 2);
            return r;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
