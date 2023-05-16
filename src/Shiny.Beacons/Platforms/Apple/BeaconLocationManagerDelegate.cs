using System;
using System.Reactive.Subjects;
using CoreLocation;
using Microsoft.Extensions.Logging;

namespace Shiny.Beacons;


public class BeaconLocationManagerDelegate : ShinyLocationDelegate
{
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly Subject<Beacon> rangeSubject = new();
    


    public BeaconLocationManagerDelegate(IServiceProvider services, ILogger<BeaconLocationManagerDelegate> logger)
    {
        this.services = services;
        this.logger = logger;
    }


    public IObservable<Beacon> WhenBeaconRanged() => this.rangeSubject;
    public override void DidRangeBeacons(CLLocationManager manager, CLBeacon[] beacons, CLBeaconRegion region)
    {
        foreach (var native in beacons)
        {
            this.rangeSubject.OnNext(new Beacon
            (
                native.ProximityUuid!.ToGuid(),
                native.Major.UInt16Value,
                native.Minor.UInt16Value,
                native.Proximity.FromNative(),
                (int)native.Rssi,
                native.Accuracy
            ));
        }
    }


    public override void RegionEntered(CLLocationManager manager, CLRegion region) => this.Invoke(region, BeaconRegionState.Entered);
    public override void RegionLeft(CLLocationManager manager, CLRegion region) => this.Invoke(region, BeaconRegionState.Exited);


    async void Invoke(CLRegion region, BeaconRegionState status)
    {
        if (region is CLBeaconRegion native)
        {
            var beaconRegion = new BeaconRegion(
                native.Identifier,
                native.ProximityUuid!.ToGuid(),
                native.Major?.UInt16Value,
                native.Minor?.UInt16Value
            );
            await this.services.RunDelegates<IBeaconMonitorDelegate>(
                x => x.OnStatusChanged(status, beaconRegion),
                this.logger
            );
        }
    }
}
