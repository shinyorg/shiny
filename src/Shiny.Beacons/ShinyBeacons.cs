using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny
{
    public static class ShinyBeacons
    {
        static IBeaconManager Current { get; } = ShinyHost.Resolve<IBeaconManager>();

        public static AccessState GetCurrentStatus(bool forMonitoring) => Current.GetCurrentStatus(forMonitoring);
        public static Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => Current.GetMonitoredRegions();
        public static Task<AccessState> RequestAccess(bool monitoring) => Current.RequestAccess(monitoring);
        public static Task StartMonitoring(BeaconRegion region) => Current.StartMonitoring(region);
        public static Task StopAllMonitoring() => Current.StopAllMonitoring();
        public static Task StopMonitoring(string identifier) => Current.StopMonitoring(identifier);
        public static IObservable<AccessState> WhenAccessStatusChanged(bool monitoring) => Current.WhenAccessStatusChanged(monitoring);
        public static IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => Current.WhenBeaconRanged(region);
    }
}
