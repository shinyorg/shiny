using System.Text.Json.Nodes;
namespace Shiny.Extensions.Configuration.Infrastructure;


public class RemoteConfigurationSource(RemoteConfig config, Func<RemoteConfig, CancellationToken, Task<JsonNode>>? getData) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new RemoteConfigurationProvider(config, getData);
        return provider;
    }
}