using System.Text.Json.Nodes;
using System.IO;
using System.Linq;
using Shiny.Extensions.Configuration;
using Shiny.Extensions.Configuration.Infrastructure;

namespace Microsoft.Extensions.Configuration;


/// <summary>
/// Extensions that add Shiny configuration sources (platform bundles, platform preferences, remote)
/// to an <see cref="IConfigurationBuilder"/>.
/// </summary>
public static partial class ConfigurationBuilderExtensions
{
    #if APPLE || ANDROID

    /// <summary>
    /// Adds a JSON configuration file from the platform's app bundle (iOS) or asset folder (Android).
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="environment">Optional environment name appended to the file name (e.g. <c>appsettings.Production.json</c>).</param>
    /// <param name="optional">When true, the configuration file is optional.</param>
    /// <param name="addPlatformSpecific">Also include a platform-specific override file when found.</param>
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
    /// Adds the platform's native preferences store (iOS NSUserDefaults or Android SharedPreferences)
    /// as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
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
    /// Adds a remote configuration source whose payload is cached to the platform's local app data folder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configurationUri">The remote endpoint URI.</param>
    /// <param name="configurationFileName">Local file name used for the cached payload.</param>
    /// <param name="getData">Optional override that produces the configuration payload (e.g. for custom auth) as a JSON object - use <c>JsonNode.Parse</c> on a response body, or <c>JsonSerializer.SerializeToNode</c> with a source-generated context for a typed object.</param>
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder,
        string configurationUri,
        string configurationFileName = "remotesettings.json",
        Func<RemoteConfig, CancellationToken, Task<JsonNode>>? getData = null
    ) => builder.AddRemoteWithCacheFile(
        configurationUri,
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            configurationFileName
        ),
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
    /// Returns the registered <see cref="IRemoteConfigurationProvider"/> from the configuration root.
    /// </summary>
    /// <param name="configuration">The configuration root to inspect.</param>
    /// <exception cref="InvalidOperationException">Thrown if no remote configuration provider is registered.</exception>
    public static IRemoteConfigurationProvider GetRemoteConfigurationProvider(this IConfigurationRoot configuration)
        => configuration
            .Providers
            .OfType<IRemoteConfigurationProvider>()
            .FirstOrDefault() 
            ?? throw new InvalidOperationException("No remote configuration provider found in the configuration root.");
    
    
    /// <summary>
    /// Adds a remote configuration source whose payload is cached at the given file path.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configurationUri">The remote endpoint URI to load from.</param>
    /// <param name="configurationFilePath">Local file path where the remote settings are persisted.</param>
    /// <param name="getData">Optional override that produces the configuration payload (e.g. for custom auth) as a JSON object - use <c>JsonNode.Parse</c> on a response body, or <c>JsonSerializer.SerializeToNode</c> with a source-generated context for a typed object.</param>
    /// <param name="waitForRemoteLoad">When true, the configuration build waits for the remote call to complete.</param>
    /// <returns>The same builder, for fluent chaining.</returns>
    public static IConfigurationBuilder AddRemoteWithCacheFile(
        this IConfigurationBuilder builder,
        string configurationUri,
        string configurationFilePath,
        Func<RemoteConfig, CancellationToken, Task<JsonNode>>? getData = null,
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

    #if !APPLE && !ANDROID

    // not on iOS/Android: there AddRemote(string, string) also matched AddRemote(configurationUri, configurationFileName),
    // which takes the same two strings with a different meaning, so the call did not compile
    /// <summary>
    /// Adds a remote configuration source whose payload is cached at the given file path.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configurationFilePath">Local file path where the remote settings are persisted.</param>
    /// <param name="configurationUri">The remote endpoint URI to load from.</param>
    /// <param name="getData">Optional override that produces the configuration payload as a JSON object.</param>
    /// <param name="waitForRemoteLoad">When true, the configuration build waits for the remote call to complete.</param>
    [Obsolete("Use AddRemoteWithCacheFile(configurationUri, configurationFilePath, ...) - note that it takes the URI first")]
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder, 
        string configurationFilePath, 
        string configurationUri,
        Func<RemoteConfig, CancellationToken, Task<JsonNode>>? getData = null,
        bool waitForRemoteLoad = true
    ) => builder.AddRemoteWithCacheFile(configurationUri, configurationFilePath, getData, waitForRemoteLoad);

    #endif

    // public static MauiAppBuilder AddRemoteConfigurationMaui(
    //     this MauiAppBuilder builder, 
    //     string configurationUri,
    //     Func<RemoteConfig, CancellationToken, Task<JsonNode>>? getData = null, 
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