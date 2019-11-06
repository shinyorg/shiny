﻿using System;
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
        /// <returns></returns>
        public static bool UseMotionActivity(this IServiceCollection services)
        {
#if __ANDROID__
            services.AddSingleton<AndroidSqliteDatabase>();
            services.AddSingleton<IMotionActivityManager, MotionActivityManagerImpl>();
            return true;
#elif __IOS__
            services.AddSingleton<IMotionActivityManager, MotionActivityManagerImpl>();
            return true;
#else
            return false;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="services"></param>
        /// <param name="regions"></param>
        /// <returns></returns>
        public static bool UseGeofencing(this IServiceCollection services, Type geofenceDelegateType, params GeofenceRegion[] regions)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GeofenceModule(geofenceDelegateType, regions));
            return true;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="regions"></param>
        /// <returns></returns>
        public static bool UseGeofencing<T>(this IServiceCollection services, params GeofenceRegion[] regions) where T : class, IGeofenceDelegate
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GeofenceModule(typeof(T), regions));
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
            services.RegisterModule(new GpsModule(null, null));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
        /// </summary>
        /// <param name="delegateType">The IGpsDelegate to call</param>
        /// <param name="services">The servicecollection to configure</param>
        /// <param name="requestIfPermissionGranted">This will be called when permission is given to use GPS functionality (background permission is assumed when calling this - setting your GPS request to not use background is ignored)</param>
        /// <returns></returns>

        public static bool UseGps(this IServiceCollection services, Type delegateType, Action<GpsRequest> requestIfPermissionGranted = null)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new GpsModule(delegateType, requestIfPermissionGranted));
            return true;
#endif
        }


        /// <summary>
        /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
        /// </summary>
        /// <typeparam name="T">The IGpsDelegate to call</typeparam>
        /// <param name="services">The servicecollection to configure</param>
        /// <param name="requestIfPermissionGranted">This will be called when permission is given to use GPS functionality (background permission is assumed when calling this - setting your GPS request to not use background is ignored)</param>
        /// <returns></returns>
        public static bool UseGps<T>(this IServiceCollection services, Action<GpsRequest> requestIfPermissionGranted = null) where T : class, IGpsDelegate
            => services.UseGps(typeof(T), requestIfPermissionGranted);
    }
}
