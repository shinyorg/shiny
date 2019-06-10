using System;
using System.Reactive.Subjects;
using CoreLocation;
using Shiny.Locations;
using Shiny.Logging;


namespace Shiny.Beacons
{
    public class BeaconLocationManagerDelegate : ShinyLocationDelegate
    {
        readonly IBeaconDelegate bdelegate;
        readonly Subject<Beacon> rangeSubject;


        public BeaconLocationManagerDelegate()
        {
            this.bdelegate = ShinyHost.Resolve<IBeaconDelegate>();
            this.rangeSubject = new Subject<Beacon>();
        }


        public IObservable<Beacon> WhenBeaconRanged() => this.rangeSubject;
        public override void DidRangeBeacons(CLLocationManager manager, CLBeacon[] beacons, CLBeaconRegion region)
        {
            foreach (var native in beacons)
            {
                this.rangeSubject.OnNext(new Beacon
                (
                    native.ProximityUuid.ToGuid(),
                    native.Major.UInt16Value,
                    native.Minor.UInt16Value,
                    //native.Accuracy,
                    native.Proximity.FromNative()
                ));
            }
        }


        public override void RegionEntered(CLLocationManager manager, CLRegion region) => this.Invoke(region, BeaconRegionState.Entered);
        public override void RegionLeft(CLLocationManager manager, CLRegion region) => this.Invoke(region, BeaconRegionState.Exited);


        void Invoke(CLRegion region, BeaconRegionState status)
        {
            try
            {
                var native = region as CLBeaconRegion;
                if (native != null)
                {
                    var beaconRegion = new BeaconRegion(
                        native.Identifier,
                        native.ProximityUuid.ToGuid(),
                        native.Major?.UInt16Value,
                        native.Minor?.UInt16Value
                    );
                    this.bdelegate?.OnStatusChanged(status, beaconRegion);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
