using Microsoft.Extensions.Configuration;

namespace Shiny.Extensions.Configuration;


public class SharedPreferencesConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new SharedPreferencesConfigurationProvider();
}
