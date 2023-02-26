using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Microsoft.Extensions.Logging;
using Shiny.Locations;

namespace Shiny.Beacons;


public class BeaconRangingManager : IBeaconRangingManager
{
    readonly CLLocationManager manager;
    readonly BeaconLocationManagerDelegate gdelegate;


    public BeaconRangingManager(IServiceProvider services, ILogger<BeaconLocationManagerDelegate> logger)
    {
        this.gdelegate = new BeaconLocationManagerDelegate(services, logger);
        this.manager = new CLLocationManager
        {
            Delegate = this.gdelegate
        };
    }


    public Task<AccessState> RequestAccess() => this.manager.RequestAccess(false);


    public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
    {
        var native = region.ToCLBeaconIdentityConstraint();
        this.manager.StartRangingBeacons(native);

        return this.gdelegate
            .WhenBeaconRanged()
            .Where(beacon => region.IsBeaconInRegion(beacon))
            .Finally(() =>
                this.manager.StopRangingBeacons(native)
            );
    }

    //public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => UIDevice.CurrentDevice.CheckSystemVersion(13, 0)
    //    ? this.WhenRanged(region)
    //    : this.WhenRangedClassic(region);


    //IObservable<Beacon> WhenRangedClassic(BeaconRegion region)
    //{
    //    var native = region.ToNative();
    //    this.manager.StartRangingBeacons(native);

    //    return this.gdelegate
    //        .WhenBeaconRanged()
    //        .Where(region.IsBeaconInRegion)
    //        .Finally(() =>
    //            this.manager.StopRangingBeacons(native)
    //        );
    //}


    //IObservable<Beacon> WhenRanged(BeaconRegion region)
    //{
    //    var native = region.ToCLBeaconIdentityConstraint();
    //    this.manager.StartRangingBeacons(native);

    //    return this.gdelegate
    //        .WhenBeaconRanged()
    //        .Where(region.IsBeaconInRegion)
    //        .Finally(() =>
    //            this.manager.StopRangingBeacons(native)
    //        );
    //}
}