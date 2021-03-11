using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="requestPermissionOnStart"></param>
        /// <returns></returns>
        public static bool UseMotionActivity(this IServiceCollection services, bool requestPermissionOnStart = false)
        {
#if __ANDROID__ || __IOS__
            services.RegisterModule(new MotionActivityModule(requestPermissionOnStart));
            return true;
#else
            return false;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="requestPermissionOnStart"></param>
        /// <returns></returns>
        public static bool UseGeofencing(this IServiceCollection services, Type geofenceDelegateType)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GeofenceModule(geofenceDelegateType));
            return true;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="requestPermissionOnStart"></param>
        /// <returns></returns>
        public static bool UseGeofencing<T>(this IServiceCollection services) where T : class, IGeofenceDelegate
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GeofenceModule(typeof(T)));
            return true;
#endif
        }


        /// <summary>
        /// This uses background GPS in realtime broadcasts to monitor geofences - DO NOT USE THIS IF YOU DON"T KNOW WHAT YOU ARE DOING
        /// It is potentially hostile to battery life
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="requestPermissionOnStart"></param>
        /// <returns></returns>
        public static bool UseGpsDirectGeofencing<T>(this IServiceCollection services) where T : class, IGeofenceDelegate
            => services.UseGpsDirectGeofencing(typeof(T));


        /// <summary>
        /// This uses background GPS in realtime broadcasts to monitor geofences - DO NOT USE THIS IF YOU DON"T KNOW WHAT YOU ARE DOING
        /// It is potentially hostile to battery life
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <param name="requestPermissionOnStart"></param>
        /// <returns></returns>
        public static bool UseGpsDirectGeofencing(this IServiceCollection services, Type delegateType)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GeofenceGpsDirectModule(delegateType));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container - foreground only
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool UseGps(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GpsModule(null));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
        /// </summary>
        /// <param name="delegateType">The IGpsDelegate to call</param>
        /// <param name="services">The servicecollection to configure</param>
        /// <returns></returns>

        public static bool UseGps(this IServiceCollection services, Type? delegateType)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GpsModule(delegateType));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
        /// </summary>
        /// <typeparam name="T">The IGpsDelegate to call</typeparam>
        /// <param name="services">The servicecollection to configure</param>
        /// <param name="requestIfPermissionGranted">This will be used when permission is given to use GPS functionality (background permission is assumed when calling this - setting your GPS request to not use background is ignored)</param>
        /// <returns></returns>
        public static bool UseGps<T>(this IServiceCollection services) where T : class, IGpsDelegate
            => services.UseGps(typeof(T));
    }
}
