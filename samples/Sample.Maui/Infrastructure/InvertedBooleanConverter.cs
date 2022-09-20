using System.Globalization;

namespace Sample.Infrastructure;


public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !((bool)value);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => !((bool)value);
}