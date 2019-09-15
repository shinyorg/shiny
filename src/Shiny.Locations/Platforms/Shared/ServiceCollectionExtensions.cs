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
        /// <param name="builder"></param>
        /// <returns></returns>
        public static bool UseMotionActivity(this IServiceCollection builder)
        {
#if __ANDROID__
            builder.AddSingleton<AndroidSqliteDatabase>();
            builder.AddSingleton<IMotionActivity, MotionActivityImpl>();
            return true;
#elif __IOS__
            builder.AddSingleton<IMotionActivity, MotionActivityImpl>();
            return true;
#else
            return false;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="regions"></param>
        /// <returns></returns>
        public static bool UseGeofencing<T>(this IServiceCollection builder, params GeofenceRegion[] regions) where T : class, IGeofenceDelegate
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new GeofenceModule<T>(regions));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container - foreground only
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static bool UseGps(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new GpsModule(null));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
        /// </summary>
        /// <typeparam name="T">The IGpsDelegate to call</typeparam>
        /// <param name="builder">The servicecollection to configure</param>
        /// <param name="requestIfPermissionGranted">This will be called when permission is given to use GPS functionality (background permission is assumed when calling this - setting your GPS request to not use background is ignored)</param>
        /// <returns></returns>
        public static bool UseGps<T>(this IServiceCollection builder, Action<GpsRequest> requestIfPermissionGranted = null) where T : class, IGpsDelegate
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new GpsModule(requestIfPermissionGranted));
            builder.AddSingleton<IGpsDelegate, T>();
            return true;
#endif
        }
    }
}
