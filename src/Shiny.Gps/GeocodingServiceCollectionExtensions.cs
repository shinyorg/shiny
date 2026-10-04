using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;

namespace Shiny;


public static class GeocodingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGeocoder"/> for reverse geocoding. iOS and Mac Catalyst use MapKit / CoreLocation and
    /// Android uses <c>android.location.Geocoder</c>; every other platform (Windows, Linux, macOS, Blazor WebAssembly),
    /// and Android devices without a geocoding backend, use the OpenStreetMap Nominatim API (see <see cref="NominatimOptions"/>).
    /// An <see cref="IGeocoder"/> you register first is kept.
    /// </summary>
    public static IServiceCollection AddGeocoding(this IServiceCollection services)
        => services.AddGeocoding(null);


    /// <summary>
    /// Registers <see cref="IGeocoder"/> for reverse geocoding, configuring the OpenStreetMap Nominatim fallback used on
    /// Windows, Linux, macOS, Blazor WebAssembly and Android devices without a geocoding backend.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureNominatim">Configures the Nominatim fallback - its server, User-Agent, contact email and language.</param>
    public static IServiceCollection AddGeocoding(this IServiceCollection services, Action<NominatimOptions>? configureNominatim)
    {
        if (services.HasService<IGeocoder>())
            return services;

        var options = new NominatimOptions();
        configureNominatim?.Invoke(options);

#if APPLE
        services.AddSingleton<IGeocoder, Geocoder>();
#elif ANDROID
        // devices without Google Play Services have no geocoding backend - fall back to OpenStreetMap there
        services.AddSingleton<IGeocoder>(sp => Android.Locations.Geocoder.IsPresent
            ? ActivatorUtilities.CreateInstance<Geocoder>(sp)
            : new NominatimGeocoder(options)
        );
#else
        services.AddSingleton<IGeocoder>(_ => new NominatimGeocoder(options));
#endif
        return services;
    }
}
