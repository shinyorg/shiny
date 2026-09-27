using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;

namespace Shiny;


public static class GeocodingServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGeocoder"/> for reverse geocoding - supported on iOS, Mac Catalyst and Android.
    /// On other targets nothing is registered.
    /// </summary>
    public static IServiceCollection AddGeocoding(this IServiceCollection services)
    {
#if ANDROID || APPLE
        if (!services.HasService<IGeocoder>())
            services.AddSingleton<IGeocoder, Geocoder>();
#endif
        return services;
    }
}
