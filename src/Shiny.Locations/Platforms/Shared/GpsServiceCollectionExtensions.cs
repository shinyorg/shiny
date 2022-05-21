using System;
using Microsoft.Extensions.DependencyInjection;
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

    public static bool AddGps(this IServiceCollection services, Type? delegateType = null)
    {
        if (delegateType != null)
            services.AddShinyService(typeof(IGpsDelegate), delegateType);

#if ANDROID
        var resultCode = GoogleApiAvailability
            .Instance
            .IsGooglePlayServicesAvailable(Application.Context);

        if (resultCode == ConnectionResult.ServiceMissing)
            services.AddShinyService<IGpsManager, LocationServicesGpsManagerImpl>();
        else
            services.AddShinyService<IGpsManager, GooglePlayServiceGpsManagerImpl>();

        return true;
#elif IOS || MACCATALYST
        services.AddShinyService<IGpsManager, GpsManager>();
        return true;
#else
        return false;

#endif
    }


    /// <summary>
    /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
    /// </summary>
    /// <typeparam name="T">The IGpsDelegate to call</typeparam>
    /// <param name="services">The servicecollection to configure</param>
    /// <returns></returns>
    public static bool AddGps<T>(this IServiceCollection services) where T : class, IGpsDelegate
        => services.AddGps(typeof(T));
}