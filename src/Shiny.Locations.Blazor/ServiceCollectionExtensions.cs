using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Locations;
using Shiny.Locations.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    // TODO: delegate
    public static void AddGps(this IServiceCollection services)
    {
        // geofencing can use GPS direct module
        services.AddShinyService<GpsManager>();
    }
}
