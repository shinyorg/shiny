using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class BeaconAdsStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseBeaconAdvertising();
    }
}