using Android.App;
using Java.Nio.FileNio.Attributes;
using Shiny.Extensions.Configuration;
using System;
using System.Linq;

namespace Microsoft.Extensions.Configuration;


public static partial class ConfigurationBuilderExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="optional"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddJsonAndroidAsset(this IConfigurationBuilder builder, string? environment = null, bool optional = true, bool includePlatformSpecific = true)
    {
        builder.AddJsonAssetInternal("appsettings.json", environment, optional);
        if (includePlatformSpecific)
            builder.AddJsonAssetInternal("appsettings.android.json", environment, true);

        return builder;
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddAndroidPreferences(this IConfigurationBuilder builder)
        => builder.Add(new SharedPreferencesConfigurationSource());


    static IConfigurationBuilder AddJsonAssetInternal(this IConfigurationBuilder builder, string fileName, string? environment, bool optional)
    {
        if (!String.IsNullOrWhiteSpace(environment))
        {
            var newFileName = GetEnvFileName(fileName, environment);
            builder.AddJsonAssetInternal(newFileName, true);
        }
        return builder.AddJsonAssetInternal(fileName, optional);
    }


    static IConfigurationBuilder AddJsonAssetInternal(this IConfigurationBuilder builder, string fileName, bool optional)
    {
        var assets = Application.Context.Assets;
        var exists = assets!
            .List("")!
            .Any(x => x.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));

        if (exists)
            builder.AddJsonStream(assets.Open(fileName));

        else if (!optional)
            throw new InvalidOperationException($"Android Asset file '{fileName}' not found");

        return builder;
    }
}
