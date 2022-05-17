using System;

namespace Shiny.Stores;


public interface ITypeConverter
{
    Type Type { get; }
    Type StoreType { get; }

    object ConvertToStore(object value);
    object ConvertFromStore(object value);
}


public class TimeSpanTypeConverter : ITypeConverter
{
    public Type Type => typeof(TimeSpan);
    public Type StoreType => typeof(long);

    public object ConvertFromStore(object value) => TimeSpan.FromMilliseconds((long)value);
    public object ConvertToStore(object value) => ((TimeSpan)value).TotalMilliseconds;
    
}


public class DateTimeOffsetTypeConverter : ITypeConverter
{
    public Type Type => typeof(DateTimeOffset);
    public Type StoreType => typeof(long);

    public object ConvertFromStore(object value) => ((DateTimeOffset)value).ToUnixTimeMilliseconds();
    public object ConvertToStore(object value) => DateTimeOffset.FromUnixTimeMilliseconds((long)value);
}