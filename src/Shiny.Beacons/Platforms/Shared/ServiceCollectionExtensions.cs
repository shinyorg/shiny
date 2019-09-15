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
        /// <param name="builder"></param>
        /// <returns></returns>
        public static bool UseBeacons(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.RegisterModule(new BeaconModule(null));
            return true;
#endif
        }


        /// <summary>
        /// Use this method if you plan to use background monitoring (works for ranging as well)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="registerBeaconsIfPermissionAvailable"></param>
        /// <returns></returns>
        public static bool UseBeacons<T>(this IServiceCollection builder, params BeaconRegion[] regionsToMonitorWhenPermissionAvailable) where T : class, IBeaconDelegate
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IBeaconDelegate, T>();
            builder.RegisterModule(new BeaconModule(regionsToMonitorWhenPermissionAvailable));
            return false;
#endif
        }
    }
}
