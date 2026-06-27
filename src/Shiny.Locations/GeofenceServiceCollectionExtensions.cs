using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Locations;
#if ANDROID
using Android.App;
using Android.Gms.Common;
#endif



public static class GeofenceServiceCollectionExtensions
{
#if PLATFORM
    /// <summary>
    ///
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TDelegate"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGeofenceDelegate
    {
        services.AddDefaultRepository();

#if ANDROID
        if (!services.HasService<IGeofenceManager>())
        {
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.ServiceMissing)
                return services.AddGpsDirectGeofencing<TDelegate>();

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
    
    
    /// <summary>
    /// This uses background GPS in realtime broadcasts to monitor geofences - DO NOT USE THIS IF YOU DON'T KNOW WHAT YOU ARE DOING
    /// It is potentially hostile to battery life
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TDelegate"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGpsDirectGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGeofenceDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        if (!services.HasService<IGeofenceManager>())
            services.AddSingletonAsImplementedInterfaces<GpsGeofenceManagerImpl>();
        
        return services;
    }
#else
    /// <summary>
    /// This is a blank AddGeofencing - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TDelegate"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGeofenceDelegate
        => services;

#endif
}
