using System;
using CoreLocation;


namespace Shiny.Beacons
{
    public class BeaconLocationManagerDelegate : CLLocationManagerDelegate
    {
        //public static IBeaconManager
        public override void RegionEntered(CLLocationManager manager, CLRegion region)
        {
        }


        public override void RegionLeft(CLLocationManager manager, CLRegion region)
        {

        }


        public event EventHandler<CLAuthorizationStatus> AuthStatusChanged;
        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            => this.AuthStatusChanged?.Invoke(this, status);
    }
}
