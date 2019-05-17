using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || __ANDROID__
using Shiny.BluetoothLE;
#endif

namespace Shiny.Beacons
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
#if WINDOWS_UWP || __ANDROID__
            builder.UseBleCentral();
            builder.AddSingleton<IBeaconManager, BeaconManager>();
            return true;
#elif __IOS__
            builder.AddSingleton<IBeaconManager, BeaconManager>();
            return true;
#else
            return false;
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
            if (!builder.UseBeacons())
                return false;

            builder.AddSingleton<IBeaconDelegate, T>();

            if (regionsToMonitorWhenPermissionAvailable.Any())
            {
                builder.RegisterPostBuildAction(sp =>
                {
                    var mgr = sp.GetService<IBeaconManager>();
                    mgr
                        .WhenAccessStatusChanged(true)
                        .Where(x => x == AccessState.Available)
                        .Take(1)
                        .SubscribeAsync(async () =>
                        {
                            foreach (var region in regionsToMonitorWhenPermissionAvailable)
                                await mgr.StartMonitoring(region);
                        });
                });
            }
            return true;
        }
    }
}
