using Microsoft.Extensions.Configuration;

namespace Shiny.Extensions.Configuration;


public class NSUserDefaultsConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new NSUserDefaultsConfigurationProvider();
}
