using Foundation;
using Shiny.Extensions.Configuration;
using System.IO;

namespace Microsoft.Extensions.Configuration;


public static partial class ConfigurationBuilderExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="fileName"></param>
    /// <param name="optional"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddJsonIosBundle(this IConfigurationBuilder builder, bool optional = true, bool reloadOnChange = true, bool includePlatformSpecific = true)
    {
        if (includePlatformSpecific)
        {
            builder.AddJsonFileInternal("appsettings.apple.json", true, reloadOnChange);
#if IOS
            builder.AddJsonFileInternal("appsettings.ios.json", true, reloadOnChange);
#elif MACCATALYST
            builder.AddJsonFileInternal("appsettings.maccatalyst.json", true, reloadOnChange);
#endif
        }
        return builder.AddJsonFileInternal("appsettings.json", optional, reloadOnChange);
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddIosUserDefaults(this IConfigurationBuilder builder)
        => builder.Add(new NSUserDefaultsConfigurationSource());


    static IConfigurationBuilder AddJsonFileInternal(this IConfigurationBuilder builder, string fileName, bool optional, bool reloadOnChange)
    {
#if MACCATALYST
        var path = Path.Combine(NSBundle.MainBundle.BundlePath, "Contents", "Resources", fileName);
#else
        var path = Path.Combine(NSBundle.MainBundle.BundlePath, fileName);
#endif
        return builder.AddJsonFile(path, optional, reloadOnChange);
    } 
}