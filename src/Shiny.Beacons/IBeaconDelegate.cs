using System;


namespace Shiny.Beacons
{
    public interface IBeaconDelegate
    {
        void OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region);
    }
}
