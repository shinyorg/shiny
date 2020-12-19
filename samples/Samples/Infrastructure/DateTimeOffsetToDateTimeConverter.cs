using System;
using System.Globalization;
using Xamarin.Forms;


namespace Samples.Infrastructure
{
    public class DateTimeOffsetToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTimeOffset dto)
                return dto.LocalDateTime;

            return default(DateTime);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return (DateTimeOffset)dt;

            throw new ArgumentException("Invalid Type");
        }
    }
}