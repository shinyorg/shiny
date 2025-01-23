using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Extensions.Configuration.Infrastructure;


public class RemoteConfigurationSource(RemoteConfig config, Func<RemoteConfig, CancellationToken, Task<object>>? getData, IServiceCollection? services) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new RemoteConfigurationProvider(config, getData);
        services?.AddSingleton<IRemoteConfigurationProvider>(provider);
        return provider;
    }
}