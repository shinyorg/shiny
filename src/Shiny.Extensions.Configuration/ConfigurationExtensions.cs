using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Configuration;
using Shiny.Extensions.Configuration.Infrastructure;

namespace Shiny;


public static class ConfigurationExtensions
{
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
        bool waitForRemoteLoad = true,
        IServiceCollection? services = null
    )
    {
        builder.AddJsonFile(configurationFilePath, true, true);
        var configuration = builder.Build();
        builder.Add(new RemoteConfigurationSource(new RemoteConfig(
            configurationUri,
            configuration,
            waitForRemoteLoad,
            configurationFilePath
        ), getData, services));
        
        return builder;
    }
    
   
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder,
        string configurationUri,
        string configurationFileName = "remotesettings.json",
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null,
        IServiceCollection? services = null
    ) => builder.AddRemote(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                configurationFileName
            ),
            configurationUri,
            getData,
            false,
            services
        );

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