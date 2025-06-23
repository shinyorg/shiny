#if APPLE || ANDROID
using System;
using System.IO;

namespace Microsoft.Extensions.Configuration;


public static partial class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddJsonPlatformBundle(this IConfigurationBuilder builder, string? environment = null, bool optional = true, bool addPlatformSpecific = true)
    {
#if APPLE
        builder.AddJsonIosBundle(environment, optional, addPlatformSpecific);
#elif ANDROID
        builder.AddJsonAndroidAsset(environment, optional, addPlatformSpecific);
#endif
        return builder;
    }


    public static IConfigurationBuilder AddPlatformPreferences(this IConfigurationBuilder builder)
    {
#if APPLE
        builder.AddIosUserDefaults();
#elif ANDROID
        builder.AddAndroidPreferences();
#endif
        return builder;
    }


    internal static string GetEnvFileName(string fileName, string environment)
    {
        var ext = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var newFileName = $"{name}.{environment}{ext}";
        return newFileName;
    }

    //public static T BindTwoWay<T>(this IConfiguration configuration, T obj) where T : INotifyPropertyChanged
    //{
    //    // TODO: sub-binding if deep binding set?
    //    obj.PropertyChanged += (sender, args) =>
    //    {

    //    };
    //    return obj;
    //}
}
#endif