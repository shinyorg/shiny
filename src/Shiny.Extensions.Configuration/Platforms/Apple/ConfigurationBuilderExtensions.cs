using Foundation;
using Shiny.Extensions.Configuration;
using System;
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
    public static IConfigurationBuilder AddJsonIosBundle(this IConfigurationBuilder builder, string? environment = null, bool optional = true, bool reloadOnChange = true, bool includePlatformSpecific = true)
    {
        builder.AddJsonFileInternal("appsettings.json", environment, optional, reloadOnChange);
        if (includePlatformSpecific)
        {
            builder.AddJsonFileInternal("appsettings.apple.json", environment, true, reloadOnChange);
#if IOS
            builder.AddJsonFileInternal("appsettings.ios.json", environment, true, reloadOnChange);
#elif MACCATALYST
            builder.AddJsonFileInternal("appsettings.maccatalyst.json", environment, true, reloadOnChange);
#endif
        }
        return builder;
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddIosUserDefaults(this IConfigurationBuilder builder)
        => builder.Add(new NSUserDefaultsConfigurationSource());


    static IConfigurationBuilder AddJsonFileInternal(this IConfigurationBuilder builder, string fileName, string? environment, bool optional, bool reloadOnChange)
    {
        builder.AddJsonFileInternal(fileName, true, reloadOnChange);
        if (!String.IsNullOrWhiteSpace(environment))
        {
            var newFileName = GetEnvFileName(fileName, environment);
            builder.AddJsonFileInternal(newFileName, true, reloadOnChange);
        }
        return builder;
    }


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