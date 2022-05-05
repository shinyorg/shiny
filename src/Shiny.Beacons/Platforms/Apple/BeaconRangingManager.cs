using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using UIKit;
using Shiny.Beacons;
using Shiny.Infrastructure;
using Shiny.Locations;


namespace Shiny.Beacons
{
    public class BeaconRangingManager : IBeaconRangingManager
    {
        readonly CLLocationManager manager;
        readonly BeaconLocationManagerDelegate gdelegate;


        public BeaconRangingManager(IServiceProvider services)
        {
            this.gdelegate = new BeaconLocationManagerDelegate(services);
            this.manager = new CLLocationManager
            {
                Delegate = this.gdelegate
            };
        }


        public Task<AccessState> RequestAccess() => this.manager.RequestAccess(false);
        public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
            ? this.WhenRanged(region)
            : this.WhenRangedClassic(region);


        IObservable<Beacon> WhenRangedClassic(BeaconRegion region)
        {
            var native = region.ToNative();
            this.manager.StartRangingBeacons(native);

            return this.gdelegate
                .WhenBeaconRanged()
                .Where(region.IsBeaconInRegion)
                .Finally(() =>
                    this.manager.StopRangingBeacons(native)
                );
        }


        IObservable<Beacon> WhenRanged(BeaconRegion region)
        {
            var native = region.ToNativeIos13();
            this.manager.StartRangingBeacons(native);

            return this.gdelegate
                .WhenBeaconRanged()
                .Where(region.IsBeaconInRegion)
                .Finally(() =>
                    this.manager.StopRangingBeacons(native)
                );
        }
    }
}