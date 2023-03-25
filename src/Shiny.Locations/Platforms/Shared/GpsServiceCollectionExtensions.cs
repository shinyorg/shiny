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
    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <param name="delegateType">The IGpsDelegate to call</param>
    /// <param name="services">The servicecollection to configure</param>
    /// <returns></returns>

    public static IServiceCollection AddGps(this IServiceCollection services, Type? delegateType = null)
    {
        if (delegateType != null)
            services.AddShinyService(delegateType);

#if ANDROID
        var resultCode = GoogleApiAvailability
            .Instance
            .IsGooglePlayServicesAvailable(Application.Context);

        if (resultCode == ConnectionResult.Success)
            services.AddShinyService<GooglePlayServiceGpsManager>();
        else
            services.AddShinyService<LocationServicesGpsManager>();

#elif APPLE
        services.AddShinyService<GpsManager>();
#endif
        return services;
    }


    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <typeparam name="T">The IGpsDelegate to call</typeparam>
    /// <param name="services">The servicecollection to configure</param>
    /// <returns></returns>
    public static IServiceCollection AddGps<T>(this IServiceCollection services) where T : class, IGpsDelegate
        => services.AddGps(typeof(T));
}
#endif