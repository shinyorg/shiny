//using System;
//namespace Shiny.SourceGenerators;

//[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
//public class ShinyJobAttribute {
//    string Identifier,
//    Type JobType,
//    bool RunOnForeground = false,
//    Dictionary<string, string>? Parameters = null,
//    InternetAccess RequiredInternetAccess = InternetAccess.None,
//    bool DeviceCharging = false,
//    bool BatteryNotLow = false,
//    bool IsSystemJob = false
//) : Attribute
//{
//}

//public enum InternetAccess
//{
//    None = 0,
//    Any = 1,
//    Unmetered = 2
//}

//[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
//public class ShinyServiceAttribute(ServiceLifetime Lifetime = ServiceLifetime.Singleton) : Attribute
//{
//    // INPC
//    // IShinyStartupTask
//}

//public enum ServiceLifetime
//{
//    Singleton,
//    Transient,
//    Scoped
//}

//[AttributeUsage(AttributeTargets.Interface)]
//public class StoreGenerateAttribute(string StoreName) : Attribute
//{
//}