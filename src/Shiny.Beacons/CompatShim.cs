using System;
using Shiny.Beacons;

namespace Shiny
{
    public static class CrossBeacons
    {
        public static IBeaconManager Current => ShinyHost.Resolve<IBeaconManager>();
    }
}
