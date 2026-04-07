using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;
using Shiny.Locations.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds GPS support for Blazor WebAssembly using the browser Geolocation API.
    /// </summary>
    /// <remarks>
    /// The browser does not support true background GPS or geofencing. Listeners
    /// only run while the page/tab is alive and the user has granted permission.
    /// </remarks>
    public static IServiceCollection AddGps(this IServiceCollection services)
    {
        services.AddShinyService<GpsManager>();
        return services;
    }


    /// <summary>
    /// Adds GPS support with a custom <see cref="IGpsDelegate"/>. Note that the delegate
    /// will only be invoked while the Blazor app is running in the foreground.
    /// </summary>
    public static IServiceCollection AddGps<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IGpsDelegate
    {
        services.AddShinyService<TDelegate>();
        return services.AddGps();
    }
}
