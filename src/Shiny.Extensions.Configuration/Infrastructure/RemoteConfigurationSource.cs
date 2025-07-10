namespace Shiny.Extensions.Configuration.Infrastructure;


public class RemoteConfigurationSource(RemoteConfig config, Func<RemoteConfig, CancellationToken, Task<object>>? getData) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new RemoteConfigurationProvider(config, getData);
        return provider;
    }
}