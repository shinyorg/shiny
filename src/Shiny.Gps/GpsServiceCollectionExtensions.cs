using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Locations;
#if ANDROID
using Android.App;
using Android.Gms.Common;
#elif !PLATFORM
using Shiny.Locations.Blazor;
#endif

namespace Shiny;


public static class GpsServiceCollectionExtensions
{
    #if APPLE
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, bool forceUseOldCLManager = false) where TDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddGps(forceUseOldCLManager);
        
        return services;
    }
    
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services, bool forceUseOldCLManager = false)
    {
        if (!forceUseOldCLManager && (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18)))
            services.AddSingletonAsImplementedInterfaces<GpsManager>();
        else
            services.AddSingletonAsImplementedInterfaces<CLLocationGpsManager>();

        return services;
    }

    #elif WINDOWS
    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TGpsDelegate>(this IServiceCollection services) where TGpsDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TGpsDelegate>();
        services.AddSingletonAsImplementedInterfaces<GpsManager>();
        return services;
    }

    #elif ANDROID
    public static IServiceCollection AddGps(this IServiceCollection services, bool forceLocationApi = false)
    {
        if (forceLocationApi)
            services.AddSingletonAsImplementedInterfaces<LocationServicesGpsManager>();

        else
        {
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.Success)
                services.AddSingletonAsImplementedInterfaces<GooglePlayServiceGpsManager>();
            
            else
                services.AddSingletonAsImplementedInterfaces<LocationServicesGpsManager>();
        }

        return services;
    }
    
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, bool forceLocationApi = false) where TDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddGps(forceLocationApi);

        return services;
    }
    #else

    /// <summary>
    /// Adds GPS on Blazor WebAssembly through the browser Geolocation API. Does nothing on any other
    /// plain .NET host, which has no location API.
    /// </summary>
    /// <remarks>
    /// The browser does not support true background GPS or geofencing. Listeners only run while the
    /// page/tab is alive and the user has granted permission.
    /// </remarks>
    public static IServiceCollection AddGps(this IServiceCollection services)
    {
        if (OperatingSystem.IsBrowser())
        {
            services.AddSingleton<GpsManager>();
            services.AddSingleton<IGpsManager>(sp => sp.GetRequiredService<GpsManager>());
        }
        return services;
    }


    /// <summary>
    /// Adds GPS with a custom <see cref="IGpsDelegate"/> on Blazor WebAssembly. The delegate is only
    /// invoked while the Blazor app is running in the foreground. Does nothing on any other plain .NET
    /// host.
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGpsDelegate
    {
        if (OperatingSystem.IsBrowser())
        {
            services.AddSingletonAsImplementedInterfaces<TDelegate>();
            services.AddGps();
        }
        return services;
    }
    #endif
}
