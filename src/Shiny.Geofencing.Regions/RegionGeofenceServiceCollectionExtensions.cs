#if APPLE || ANDROID
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;

namespace Shiny;


/// <summary>
/// Registration for GPS-driven region geofencing. Only available on iOS, Android and Mac Catalyst - the
/// base target ships the types but has no background GPS to drive them.
/// </summary>
public static class RegionGeofenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers region geofencing along with the platform GPS services it rides on.
    /// </summary>
    /// <typeparam name="TDelegate">The app's transition handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Registers the region sets and tunes the reading filters.</param>
    /// <exception cref="ArgumentException">No region set was registered.</exception>
    public static IServiceCollection AddRegionGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        Action<RegionGeofenceConfig> configure
    ) where TDelegate : class, IRegionGeofenceDelegate
    {
        ArgumentNullException.ThrowIfNull(configure);

        var config = new RegionGeofenceConfig();
        configure(config);

        if (config.RegionSets.Count == 0)
            throw new ArgumentException("At least one region set must be registered - call AddRegionSet.", nameof(configure));

        services.AddSingleton(config);
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddSingleton<RegionEvaluator>();
        services.AddSingletonAsImplementedInterfaces<RegionGeofenceManager>();

        if (config.UsesGeocoder && !services.HasService<IGeocoder>())
            services.AddGeocoding();

        // Readings fan out to every IGpsDelegate, so an app that already registered GPS (with its own delegate)
        // keeps its manager - only bring the platform one in when nothing has. Region geofencing still owns
        // the listener while it runs; the app can't start a second one alongside it.
        services.AddSingletonAsImplementedInterfaces<RegionGeofenceGpsDelegate>();
        if (!services.HasService<IGpsManager>())
            services.AddGps();

        return services;
    }
}
#endif
