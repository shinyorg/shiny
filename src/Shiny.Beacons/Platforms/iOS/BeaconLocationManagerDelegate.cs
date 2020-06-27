using System;
using System.Reactive.Subjects;
using CoreLocation;


namespace Shiny.Beacons
{
    public class BeaconLocationManagerDelegate : ShinyLocationDelegate
    {
        readonly IBeaconMonitorDelegate bdelegate;
        readonly Subject<Beacon> rangeSubject;


        public BeaconLocationManagerDelegate()
        {
            this.bdelegate = ShinyHost.Resolve<IBeaconMonitorDelegate>();
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


        void Invoke(CLRegion region, BeaconRegionState status) => Dispatcher.ExecuteBackgroundTask(async () =>
        {
            if (region is CLBeaconRegion native)
            {
                var beaconRegion = new BeaconRegion(
                    native.Identifier,
                    native.ProximityUuid.ToGuid(),
                    native.Major?.UInt16Value,
                    native.Minor?.UInt16Value
                );
                await this.bdelegate?.OnStatusChanged(status, beaconRegion);
            }
        });
    }
}
