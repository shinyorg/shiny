#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Locations;
#if ANDROID
using Android.App;
using Android.Gms.Common;
#endif

namespace Shiny;


public static class GpsServiceCollectionExtensions
{
    #if APPLE
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <typeparam name="T">The IGpsDelegate to call</typeparam>
    /// <param name="services">The servicecollection to configure</param>
    /// <param name="forceUseOldCLManager">Force Shiny to use the old CLLocationManager implementation - features are not guaranteed to work on iOS 18+</param>
    /// <returns></returns>
    public static IServiceCollection AddGps<T>(this IServiceCollection services, bool forceUseOldCLManager = false) where T : class, IGpsDelegate
        => services.AddGps(typeof(T));
    
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <param name="delegateType">The IGpsDelegate to call</param>
    /// <param name="services">The servicecollection to configure</param>
    /// <param name="forceUseOldCLManager">Force Shiny to use the old CLLocationManager implementation - features are not guaranteed to work on iOS 18+</param>
    /// <returns></returns>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null, bool forceUseOldCLManager = false)
    {
        if (delegateType != null)
            services.AddShinyService(delegateType);
        
        if (!forceUseOldCLManager && (OperatingSystemShim.IsIOSVersionAtLeast(18) || OperatingSystemShim.IsMacCatalystVersionAtLeast(18)))
        {
            services.AddShinyService<GpsManager>();    
        }
        else
        {
            services.AddShinyService<CLLocationGpsManager>();
        }

        return services;
    }

    #elif WINDOWS
    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    /// <typeparam name="T">The IGpsDelegate to call</typeparam>
    /// <param name="services">The servicecollection to configure</param>
    /// <returns></returns>
    public static IServiceCollection AddGps<T>(this IServiceCollection services) where T : class, IGpsDelegate
        => services.AddGps(typeof(T));

    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    /// <param name="services">The servicecollection to configure</param>
    /// <param name="delegateType">The IGpsDelegate to call</param>
    /// <returns></returns>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null)
    {
        if (delegateType != null)
            services.AddShinyService(delegateType);

        services.AddShinyService<GpsManager>();
        return services;
    }

    #elif ANDROID
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <typeparam name="T">The IGpsDelegate to call</typeparam>
    /// <param name="services">The servicecollection to configure</param>
    /// <param name="forceLocationApi">This bypasses fusedlocationprovider and goes to the deprecated (and fallback) location API - features are not guaranteed to work</param>
    /// <returns></returns>
    public static IServiceCollection AddGps<T>(this IServiceCollection services, bool forceLocationApi = false) where T : class, IGpsDelegate
        => services.AddGps(typeof(T), forceLocationApi);

    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <param name="delegateType">The IGpsDelegate to call</param>
    /// <param name="services">The servicecollection to configure</param>
    /// <param name="forceLocationApi">This bypasses fusedlocationprovider and goes to the deprecated (and fallback) location API - features are not guaranteed to work</param>
    /// <returns></returns>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null, bool forceLocationApi = false)
    {
        if (delegateType != null)
            services.AddShinyService(delegateType);

        if (forceLocationApi)
        {
            services.AddShinyService<LocationServicesGpsManager>();
        }
        else
        {
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.Success)
                services.AddShinyService<GooglePlayServiceGpsManager>();
            else
                services.AddShinyService<LocationServicesGpsManager>();
        }

        return services;
    }
    #endif
}
#endif