#if APPLE || ANDROID
namespace Microsoft.Extensions.Configuration;


public static partial class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddJsonPlatformBundle(this IConfigurationBuilder builder, string fileName = "appsettings.json", bool optional = true)
    {
#if APPLE
        builder.AddJsonIosBundle(fileName, optional);
#elif ANDROID
        builder.AddJsonAndroidAsset(fileName, optional);
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