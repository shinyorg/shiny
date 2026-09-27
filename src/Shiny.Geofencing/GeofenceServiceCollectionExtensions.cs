using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Locations;

namespace Shiny;

public static class GeofenceServiceCollectionExtensions
{
#if PLATFORM
    /// <summary>
    ///
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TDelegate"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGeofenceDelegate
    {
        services.AddDefaultRepository();

#if ANDROID
        if (!services.HasService<IGeofenceManager>())
        {
            // the GPS-direct fallback for devices without Google Play Services is parked (commented out)
            // in Shiny.Gps - Shiny.Geofencing does not reference it
            // var resultCode = GoogleApiAvailability
            //     .Instance
            //     .IsGooglePlayServicesAvailable(Application.Context);
            //
            // if (resultCode == ConnectionResult.ServiceMissing)
            //     return services.AddGpsDirectGeofencing<TDelegate>();

            services.AddSingletonAsImplementedInterfaces<GeofenceManager>();
        }
#elif APPLE
        if (!services.HasService<IGeofenceManager>())
        {
            if (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18))
                services.AddSingletonAsImplementedInterfaces<GeofenceManager>();
            else
                services.AddSingletonAsImplementedInterfaces<CLLocationGeofenceManager>();
        }
#elif WINDOWS
        if (!services.HasService<IGeofenceManager>())
            services.AddSingletonAsImplementedInterfaces<GeofenceManager>();
#endif
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        
        return services;
    }

#else
    /// <summary>
    /// This is a blank AddGeofencing - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TDelegate"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGeofenceDelegate
        => services;

#endif
}
