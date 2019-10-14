using System;
using Microsoft.Extensions.DependencyInjection;
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
        public static bool UseBeacons(this IServiceCollection services)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new BeaconModule(null));
            return true;
#endif
        }


        /// <summary>
        /// Use this method if you plan to use background monitoring (works for ranging as well)
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="delegateType"></param>
        /// <param name="registerBeaconsIfPermissionAvailable"></param>
        /// <returns></returns>
        public static bool UseBeacons(this IServiceCollection services, Type delegateType, params BeaconRegion[] regionsToMonitorWhenPermissionAvailable)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new BeaconModule(delegateType, regionsToMonitorWhenPermissionAvailable));
            return false;
#endif
        }


        /// <summary>
        /// Use this method if you plan to use background monitoring (works for ranging as well)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="registerBeaconsIfPermissionAvailable"></param>
        /// <returns></returns>
        public static bool UseBeacons<T>(this IServiceCollection services, params BeaconRegion[] regionsToMonitorWhenPermissionAvailable) where T : class, IBeaconDelegate
            => services.UseBeacons(typeof(T), regionsToMonitorWhenPermissionAvailable);
    }
}
