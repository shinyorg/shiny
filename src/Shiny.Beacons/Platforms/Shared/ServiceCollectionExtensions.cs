using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Beacons;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register the beacon service with this if you only plan to use ranging
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool UseBeaconRanging(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
#if MONOANDROID || WINDOWS_UWP
            services.UseBleClient();
#endif
            services.TryAddSingleton<IBeaconRangingManager, BeaconRangingManager>();
            return true;
#endif
        }


        /// <summary>
        /// Use this method if you plan to use background monitoring (works for ranging as well)
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        public static bool UseBeaconMonitoring(this IServiceCollection services, Type delegateType)
        {
#if NETSTANDARD
            return false;
#else
            if (delegateType == null)
                throw new ArgumentException("You can't register monitoring regions without a delegate type");

#if __ANDROID__ || WINDOWS_UWP
            services.TryAddSingleton<BackgroundTask>();
            services.UseBleClient();
            services.UseNotifications();
#endif
            services.AddSingleton(typeof(IBeaconMonitorDelegate), delegateType);
            services.TryAddSingleton<IBeaconMonitoringManager, BeaconMonitoringManager>();
            return true;
#endif
        }


        /// <summary>
        /// Use this method if you plan to use background monitoring (works for ranging as well)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static bool UseBeaconMonitoring<T>(this IServiceCollection services) where T : class, IBeaconMonitorDelegate
            => services.UseBeaconMonitoring(typeof(T));
    }
}
