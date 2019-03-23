using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
#if WINDOWS_UWP || __ANDROID__
using Shiny.BluetoothLE;
#endif

namespace Shiny.Beacons
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseBeacons<T>(this IServiceCollection builder, Func<IEnumerable<BeaconRegion>> registerBeacons = null) where T : class, IBeaconDelegate
        {
            builder.AddSingleton<IBeaconDelegate, T>();
            //if (registerBeacons != null)
            //{
            //    builder.OnBuild(c =>
            //    {
            //        // TODO: wait for grant state
            //        var mgr = c.Resolve<IBeaconManager>();
            //        var regions = registerBeacons();
            //        foreach (var region in regions)
            //            mgr.StartMonitoring(region);
            //    });
            //}
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
    }
}
