//using System;
//using Shiny.Locations;

//namespace Shiny.Stores.Infrastructure;


//public interface ITypeConverter
//{
//    Type Type { get; }
//    Type StoreType { get; }

//    object ConvertToStore(object value);
//    object ConvertFromStore(object value);
//}


//public class TimeSpanTypeConverter : ITypeConverter
//{
//    public Type Type => typeof(TimeSpan);
//    public Type StoreType => typeof(double);

//    public object ConvertFromStore(object value) => TimeSpan.FromMilliseconds((long)value);
//    public object ConvertToStore(object value) => ((TimeSpan)value).TotalMilliseconds;

//}


//public class DateTimeOffsetTypeConverter : ITypeConverter
//{
//    public Type Type => typeof(DateTimeOffset);
//    public Type StoreType => typeof(long);

//    public object ConvertFromStore(object value) => ((DateTimeOffset)value).ToUnixTimeMilliseconds();
//    public object ConvertToStore(object value) => DateTimeOffset.FromUnixTimeMilliseconds((long)value);
//}


//public class DistanceConverter : ITypeConverter
//{
//    public Type Type => typeof(Distance);
//    public Type StoreType => typeof(double);

//    public object ConvertFromStore(object value) => Distance.FromKilometers((double)value);
//    public object ConvertToStore(object value) => ((Distance)value).TotalKilometers;
//}


//public class PositionConverter : ITypeConverter
//{
//    public Type Type => typeof(Position);
//    public Type StoreType => typeof(string);

//    public object ConvertFromStore(object value)
//    {
//        var s = (string)value;
//        var coords = s.Split('/');
//        var lat = double.Parse(coords[0]);
//        var lng = double.Parse(coords[1]);
//        return new Position(lat, lng);
//    }


//    public object ConvertToStore(object value)
//    {
//        var p = (Position)value;
//        return $"{p.Latitude}/{p.Longitude}";
//    }
//}


//public class TypeTypeConverter : ITypeConverter
//{
//    public Type Type => typeof(Type);
//    public Type StoreType => typeof(string);

//    public object ConvertFromStore(object value) => Type.GetType((string)value);
//    public object ConvertToStore(object value) => ((Type)value).AssemblyQualifiedName;
//}