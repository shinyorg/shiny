// GPS-direct geofencing is parked here, commented out, since Shiny.Gps no longer references
// Shiny.Geofencing - it needs IGeofenceDelegate, IGeofenceManager and GeofenceRegion from there.
// It used to be the Android fallback in AddGeofencing when Google Play Services were missing.
// See GpsGeofenceManagerImpl.cs and GpsGeofenceDelegate.cs.

// using System.Diagnostics.CodeAnalysis;
// using Microsoft.Extensions.DependencyInjection;
// using Shiny.Locations;
//
// namespace Shiny;
//
//
// public static class GpsDirectGeofencingServiceCollectionExtensions
// {
// #if PLATFORM
//     /// <summary>
//     /// This uses background GPS in realtime broadcasts to monitor geofences - DO NOT USE THIS IF YOU DON'T KNOW WHAT YOU ARE DOING
//     /// It is potentially hostile to battery life
//     /// </summary>
//     /// <param name="services"></param>
//     /// <typeparam name="TDelegate"></typeparam>
//     /// <returns></returns>
//     public static IServiceCollection AddGpsDirectGeofencing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(this IServiceCollection services) where TDelegate : class, IGeofenceDelegate
//     {
//         services.AddDefaultRepository();
//         services.AddSingletonAsImplementedInterfaces<TDelegate>();
//         if (!services.HasService<IGeofenceManager>())
//         {
//             services.AddSingletonAsImplementedInterfaces<GpsGeofenceManagerImpl>();
//
//             // readings from the GPS listener are what drive the geofence transitions
//             services.AddSingletonAsImplementedInterfaces<GpsGeofenceDelegate>();
//             if (!services.HasService<IGpsManager>())
//             {
// #if WINDOWS
//                 services.AddSingletonAsImplementedInterfaces<GpsManager>();
// #else
//                 services.AddGps();
// #endif
//             }
//         }
//
//         return services;
//     }
// #endif
// }
