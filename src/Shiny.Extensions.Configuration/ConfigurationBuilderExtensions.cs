using System.IO;
using System.Linq;
using Shiny.Extensions.Configuration;
using Shiny.Extensions.Configuration.Infrastructure;

namespace Microsoft.Extensions.Configuration;


public static partial class ConfigurationBuilderExtensions
{
    #if APPLE || ANDROID
    
    /// <summary>
    /// Adds a JSON configuration file from the platform-specific bundle or assets.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="environment"></param>
    /// <param name="optional"></param>
    /// <param name="addPlatformSpecific"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddJsonPlatformBundle(this IConfigurationBuilder builder, string? environment = null, bool optional = true, bool addPlatformSpecific = true)
    {
#if APPLE
        builder.AddJsonIosBundle(environment, optional, addPlatformSpecific);
#elif ANDROID
        builder.AddJsonAndroidAsset(environment, optional, addPlatformSpecific);
#endif
        return builder;
    }


    /// <summary>
    /// Adds platform-specific preferences to the configuration pipeline.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddPlatformPreferences(this IConfigurationBuilder builder)
    {
#if APPLE
        builder.AddIosUserDefaults();
#elif ANDROID
        builder.AddAndroidPreferences();
#endif
        return builder;
    }

    
    /// <summary>
    /// Adds remote configuration to the configuration pipeline
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configurationUri"></param>
    /// <param name="configurationFileName"></param>
    /// <param name="getData"></param>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder,
        string configurationUri,
        string configurationFileName = "remotesettings.json",
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null
    ) => builder.AddRemote(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            configurationFileName
        ),
        configurationUri,
        getData,
        false
    );

    internal static string GetEnvFileName(string fileName, string environment)
    {
        var ext = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var newFileName = $"{name}.{environment}{ext}";
        return newFileName;
    }

    #endif

    /// <summary>
    /// Gets the remote configuration provider from the configuration root.
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IRemoteConfigurationProvider GetRemoteConfigurationProvider(this IConfigurationRoot configuration)
        => configuration
            .Providers
            .OfType<IRemoteConfigurationProvider>()
            .FirstOrDefault() 
            ?? throw new InvalidOperationException("No remote configuration provider found in the configuration root.");
    
    
    /// <summary>
    /// Adds remote configuration to the configuration pipeline
    /// </summary>
    /// <param name="builder">The configuration builder</param>
    /// <param name="configurationFilePath">The location of where the remote settings should be persisted</param>
    /// <param name="configurationUri">This allows you to get the configuration URI from the previous remote call & allows you to update the ConfigurationUri if needed</param>
    /// <param name="getData">If you wish to control how/what data is returned, pass this function</param>
    /// <param name="waitForRemoteLoad">If you want the network call to be waited until completion before returning</param>
    /// <param name="services">If presented to the extension method, IRemoteConfigurationProvider is installed to the service container</param>
    /// <returns>The current configuration builder to allow for chaining</returns>
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder, 
        string configurationFilePath, 
        string configurationUri,
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null,
        bool waitForRemoteLoad = true
    )
    {
        builder.AddJsonFile(configurationFilePath, true, true);
        var configuration = builder.Build();
        builder.Add(new RemoteConfigurationSource(new RemoteConfig(
            configurationUri,
            configuration,
            waitForRemoteLoad,
            configurationFilePath
        ), getData));
        
        return builder;
    }

    // public static MauiAppBuilder AddRemoteConfigurationMaui(
    //     this MauiAppBuilder builder, 
    //     string configurationUri,
    //     Func<RemoteConfig, CancellationToken, Task<object>>? getData = null, 
    //     string configurationFileName = "remotesettings.json"
    // )
    // {
    //     builder.Configuration.AddRemote(
    //         Path.Combine(FileSystem.AppDataDirectory, configurationFileName),
    //         configurationUri,
    //         getData,
    //         false,
    //         builder.Services
    //     );
    //     return builder;
    // } 
}