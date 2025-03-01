using System.Threading;
using System.Threading.Tasks;
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
    /// <returns>The current configuration builder to allow for chaining</returns>
    /// <param name="configurationFilePath"></param>
    /// <param name="configurationUri"></param>
    /// <param name="getData"></param>
    /// <param name="waitForRemoteLoad"></param>
    /// <param name="autoRefreshTimer"></param>
    /// <param name="services"></param>
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder, 
        string configurationFilePath, 
        string configurationUri,
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null,
        bool waitForRemoteLoad = true,
        TimeSpan? autoRefreshTimer = null,
        IServiceCollection? services = null
    )
    {
        builder.AddJsonFile(configurationFilePath, true, true);
        var configuration = builder.Build();
        builder.Add(new RemoteConfigurationSource(new RemoteConfig(
            configurationUri,
            configuration,
            waitForRemoteLoad,
            autoRefreshTimer,
            configurationFilePath
        ), getData, services));
        
        return builder;
    }
}