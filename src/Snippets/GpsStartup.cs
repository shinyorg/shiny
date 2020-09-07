using Microsoft.Extensions.DependencyInjection;

using Shiny;

public class GpsStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGps();

        // OR

        services.UseGps<GpsDelegate>();
    }
}
