using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class GeofenceStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGeofencing<GeofenceDelegate>();
    }
}
