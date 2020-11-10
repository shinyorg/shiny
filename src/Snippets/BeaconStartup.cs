using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class BeaconStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseBeaconRanging();

        services.UseBeaconMonitoring<BeaconMonitorDelegate>();
    }
}
