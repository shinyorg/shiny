using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class LocationSyncStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        //services.UseGps<>();
        //services.UseGeofencingSync<>();
    }
}
