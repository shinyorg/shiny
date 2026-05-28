using System;
using System.Diagnostics.CodeAnalysis;
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
    public static IServiceCollection AddGps<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services, bool forceUseOldCLManager = false) where TDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddGps();
        
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
    public static IServiceCollection AddGps<TGpsDelegate>(this IServiceCollection services) where TGpsDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
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
    public static IServiceCollection AddGps<TDelegate>(this IServiceCollection services, bool forceLocationApi = false) where TDelegate : class, IGpsDelegate
    {
        services.AddSingletonAsImplementedInterfaces<TDelegate>();
        services.AddGps();

        return services;
    }
    #else

    /// <summary>
    /// This is a blank AddGps - you won't see this documentation if you've got a proper target that is supported
    /// </summary>
    public static IServiceCollection AddGps<TDelegate>(this IServiceCollection services) where TDelegate : class, IGpsDelegate
        => services;
    #endif
}
