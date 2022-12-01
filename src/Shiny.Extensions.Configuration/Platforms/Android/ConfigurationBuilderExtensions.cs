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
    public static IConfigurationBuilder AddJsonAndroidAsset(this IConfigurationBuilder builder, bool optional = true, bool includePlatformSpecific = true)
    {
        if (includePlatformSpecific)
            builder.AddJsonAssetInternal("appsettings.android.json", true);

        return builder.AddJsonAssetInternal("appsettings.json", optional);
    }


    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddAndroidPreferences(this IConfigurationBuilder builder)
        => builder.Add(new SharedPreferencesConfigurationSource());


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
