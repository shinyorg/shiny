#if WINDOWS_UWP || __ANDROID__
using System;


namespace Shiny.Beacons
{
    public class BeaconRegionStatus
    {
        public BeaconRegionStatus(BeaconRegion region)
        {
            this.Region = region;
        }


        public BeaconRegion Region { get; }
        public bool? IsInRange { get; set; }
        public DateTimeOffset LastPing { get; set; }
    }
}
#endif