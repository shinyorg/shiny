using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Locations;


namespace Shiny;

public static class ServiceCollectionExtensions
{
    public static void UseShinyWasmGps(this IServiceCollection services)
    {
        // geofencing can use GPS direct module
        services.TryAddSingleton<IGpsManager, Shiny.Locations.Web.GpsManager>();
    }
}
