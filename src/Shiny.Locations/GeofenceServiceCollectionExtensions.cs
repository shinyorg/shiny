using System;
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
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing(this IServiceCollection services, Type delegateType)
    {
        services.AddSingleton(delegateType);
        services.AddSingleton(typeof(IGeofenceDelegate), sp => sp.GetRequiredService(delegateType));
        services.AddDefaultRepository();
        services.AddJsonContext(ShinyLocationsJsonContext.Default);

#if ANDROID
        if (!services.HasService<IGeofenceManager>())
        {
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.ServiceMissing)
                return services.AddGpsDirectGeofencing(delegateType);

            services.AddSingleton<GeofenceManager>();
            services.AddSingleton<IGeofenceManager>(sp => sp.GetRequiredService<GeofenceManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<GeofenceManager>());
        }
#elif APPLE
        if (!services.HasService<IGeofenceManager>())
        {
            if (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18))
            {
                services.AddSingleton<GeofenceManager>();
                services.AddSingleton<IGeofenceManager>(sp => sp.GetRequiredService<GeofenceManager>());
                services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<GeofenceManager>());
            }
            else
            {
                services.AddSingleton<CLLocationGeofenceManager>();
                services.AddSingleton<IGeofenceManager>(sp => sp.GetRequiredService<CLLocationGeofenceManager>());
            }
        }
#elif WINDOWS
        if (!services.HasService<IGeofenceManager>())
        {
            services.AddSingleton<GeofenceManager>();
            services.AddSingleton<IGeofenceManager>(sp => sp.GetRequiredService<GeofenceManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<GeofenceManager>());
        }
#endif
        return services;
    }


    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing<T>(this IServiceCollection services) where T : class, IGeofenceDelegate
        => services.AddGeofencing(typeof(T));


    /// <summary>
    /// This uses background GPS in realtime broadcasts to monitor geofences - DO NOT USE THIS IF YOU DON"T KNOW WHAT YOU ARE DOING
    /// It is potentially hostile to battery life
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddGpsDirectGeofencing<T>(this IServiceCollection services) where T : class, IGeofenceDelegate
        => services.AddGpsDirectGeofencing(typeof(T));


    /// <summary>
    /// This uses background GPS in realtime broadcasts to monitor geofences - DO NOT USE THIS IF YOU DON"T KNOW WHAT YOU ARE DOING
    /// It is potentially hostile to battery life
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static IServiceCollection AddGpsDirectGeofencing(this IServiceCollection services, Type delegateType)
    {
        services.AddSingleton(delegateType);
        services.AddSingleton(typeof(IGeofenceDelegate), sp => sp.GetRequiredService(delegateType));
        services.AddSingleton<GpsGeofenceManagerImpl>();
        services.AddSingleton<IGeofenceManager>(sp => sp.GetRequiredService<GpsGeofenceManagerImpl>());
        services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<GpsGeofenceManagerImpl>());
        return services;
    }
#else
    /// <summary>
    /// This is a blank AddGeofencing - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing(this IServiceCollection services, Type delegateType)
        => services;

    /// <summary>
    /// This is a blank AddGeofencing - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddGeofencing<T>(this IServiceCollection services) where T : class, IGeofenceDelegate
        => services;

#endif
}
