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
    // ⚠️ Every target has the parameterless AddGps() / AddGps<TDelegate>(). A library or shared project built
    // against net11.0 compiles its call to those, and the platform assembly the app loads must have the same
    // signature - an optional parameter is a different method, so it would throw MissingMethodException.

    #if APPLE
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGpsDelegate
        => services.AddGps<TDelegate>(false);

    /// <summary>
    /// This registers GPS services with the Shiny container - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services)
        => services.AddGps(false);

    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, bool forceUseOldCLManager) where TDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddGps(forceUseOldCLManager);
        
        return services;
    }
    
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services, bool forceUseOldCLManager)
    {
#if MACOS
        // macOS (AppKit) has no CLServiceSession / CLLocationUpdater session pair, so it is always CLLocationManager
        services.AddSingletonAsImplementedInterfaces<CLLocationGpsManager>();
#else
        if (!forceUseOldCLManager && (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18)))
            services.AddSingletonAsImplementedInterfaces<GpsManager>();
        else
            services.AddSingletonAsImplementedInterfaces<CLLocationGpsManager>();
#endif

        return services;
    }

    #elif WINDOWS
    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TGpsDelegate>(this IServiceCollection services) where TGpsDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TGpsDelegate>();
        services.AddGps();
        return services;
    }

    /// <summary>
    /// This registers GPS services with the Shiny container - Windows supports foreground GPS only (no background)
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services)
    {
        services.AddSingletonAsImplementedInterfaces<GpsManager>();
        return services;
    }

    #elif ANDROID
    /// <summary>
    /// This registers GPS services with the Shiny container - Google Play Services when available, the platform location API otherwise
    /// </summary>
    public static IServiceCollection AddGps(this IServiceCollection services)
        => services.AddGps(false);

    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGpsDelegate
        => services.AddGps<TDelegate>(false);

    public static IServiceCollection AddGps(this IServiceCollection services, bool forceLocationApi)
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
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, bool forceLocationApi) where TDelegate : class, IGpsDelegate
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
