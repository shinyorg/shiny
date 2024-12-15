using Microsoft.Extensions.Configuration;
using Shiny.Extensions.Configuration;

namespace Shiny;


public static class MauiAppBuilderExtensions
{
    public static IConfigurationBuilder AddRemoteMaui(
        this IConfigurationBuilder builder,
        string configurationUri,
        string configurationFileName = "remotesettings.json",
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null)
    {
        return builder.AddRemote(
            Path.Combine(FileSystem.AppDataDirectory, configurationFileName),
            configurationUri,
            getData,
            false,
            null
       );
    }
    
    public static MauiAppBuilder AddRemoteConfigurationMaui(
        this MauiAppBuilder builder, 
        string configurationUri,
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null, 
        string configurationFileName = "remotesettings.json"
    )
    {
        builder.Configuration.AddRemote(
            Path.Combine(FileSystem.AppDataDirectory, configurationFileName),
            configurationUri,
            getData,
            false,
            builder.Services
        );
        return builder;
    }   
}