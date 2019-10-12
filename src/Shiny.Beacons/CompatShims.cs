using System;


namespace Shiny.Beacons
{
    public static class CrossBeacons
    {
        public static IBeaconManager Current => ShinyHost.Resolve<IBeaconManager>();
    }
}
