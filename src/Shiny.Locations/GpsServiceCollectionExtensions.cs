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
    public static IServiceCollection AddGps<T>(this IServiceCollection services, bool forceUseOldCLManager = false) where T : class, IGpsDelegate
        => services.AddGps(typeof(T));

    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null, bool forceUseOldCLManager = false)
    {
        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(IGpsDelegate), sp => sp.GetRequiredService(delegateType));
        }

        if (!forceUseOldCLManager && (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18)))
        {
            services.AddSingleton<GpsManager>();
            services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<GpsManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<GpsManager>());
        }
        else
        {
            services.AddSingleton<CLLocationGpsManager>();
            services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<CLLocationGpsManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<CLLocationGpsManager>());
        }

        return services;
    }

    #elif WINDOWS
    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    public static IServiceCollection AddGps<T>(this IServiceCollection services) where T : class, IGpsDelegate
        => services.AddGps(typeof(T));

    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null)
    {
        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(IGpsDelegate), sp => sp.GetRequiredService(delegateType));
        }

        services.AddSingleton<GpsManager>();
        services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<GpsManager>());
        return services;
    }

    #elif ANDROID
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps<T>(this IServiceCollection services, bool forceLocationApi = false) where T : class, IGpsDelegate
        => services.AddGps(typeof(T), forceLocationApi);

    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null, bool forceLocationApi = false)
    {
        if (delegateType != null)
        {
            services.AddSingleton(delegateType);
            services.AddSingleton(typeof(IGpsDelegate), sp => sp.GetRequiredService(delegateType));
        }

        if (forceLocationApi)
        {
            services.AddSingleton<LocationServicesGpsManager>();
            services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<LocationServicesGpsManager>());
            services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<LocationServicesGpsManager>());
        }
        else
        {
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.Success)
            {
                services.AddSingleton<GooglePlayServiceGpsManager>();
                services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<GooglePlayServiceGpsManager>());
                services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<GooglePlayServiceGpsManager>());
            }
            else
            {
                services.AddSingleton<LocationServicesGpsManager>();
                services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<LocationServicesGpsManager>());
                services.AddSingleton<IShinyStartupTask>(sp => sp.GetRequiredService<LocationServicesGpsManager>());
            }
        }

        return services;
    }
    #else

    /// <summary>
    /// This is a blank AddGps - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    public static IServiceCollection AddGps<T>(this IServiceCollection services) where T : class, IGpsDelegate
        => services;

    /// <summary>
    /// This is a blank AddGps - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null)
        => services;
    #endif
}
