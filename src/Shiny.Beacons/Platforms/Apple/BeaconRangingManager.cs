using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreLocation;

namespace Shiny.Beacons;


/// <summary>
/// Ranges iBeacons through CoreLocation.
/// </summary>
/// <remarks>
/// CoreBluetooth strips Apple's own iBeacon manufacturer data out of scan results, so a BLE scan can
/// never see an iBeacon on an Apple platform no matter how it is configured. CoreLocation is the
/// only route, which is why ranging here costs a location permission rather than a Bluetooth one.
/// CoreLocation also smooths the distance itself, so the RSSI filter and path loss model the other
/// platforms use are deliberately not applied on top.
/// </remarks>
public class BeaconRangingManager : IBeaconRangingManager
{
    readonly BeaconRangingOptions options;
    readonly CLLocationManager manager;
    readonly BeaconRangingDelegate locationDelegate;
    readonly object syncLock = new();
    readonly Dictionary<string, int> rangedConstraints = new();


    /// <summary>
    /// Creates the manager.
    /// </summary>
    /// <param name="options">Supplies the proximity thresholds and the clock.</param>
    public BeaconRangingManager(BeaconRangingOptions options)
    {
        this.options = options;
        this.locationDelegate = new BeaconRangingDelegate(options);
        this.manager = new CLLocationManager { Delegate = this.locationDelegate };
    }


    /// <inheritdoc />
    public AccessState CurrentStatus => CLLocationManager.IsRangingAvailable
        ? this.manager.GetCurrentStatus(false)
        : AccessState.NotSupported;


    /// <inheritdoc />
    public Task<AccessState> RequestAccess()
        // Ranging is a foreground activity, so when-in-use is enough - monitoring is what needs
        // always-on, and it asks for that itself.
        => CLLocationManager.IsRangingAvailable
            ? this.manager.RequestAccess(false)
            : Task.FromResult(AccessState.NotSupported);


    /// <inheritdoc />
    public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
        => Observable
            .FromAsync(this.RequestAccess)
            .Do(access => access.Assert())
            .SelectMany(_ => this.Range(region));


    IObservable<Beacon> Range(BeaconRegion region) => Observable.Create<Beacon>(ob =>
    {
        var constraint = region.ToConstraint();
        var key = region.Uuid + "/" + region.Major + "/" + region.Minor;

        var sub = this.locationDelegate
            .WhenBeaconRanged()
            .Where(region.IsBeaconInRegion)
            .Subscribe(ob.OnNext, ob.OnError);

        this.StartRanging(key, constraint);

        return new CompositeDisposable(
            sub,
            Disposable.Create(() => this.StopRanging(key, constraint))
        );
    });


    // CoreLocation ranges per constraint, not per subscriber. Two subscriptions to the same region
    // must not have the first disposal stop ranging for the second, so constraints are refcounted.
    void StartRanging(string key, CLBeaconIdentityConstraint constraint)
    {
        lock (this.syncLock)
        {
            if (this.rangedConstraints.TryGetValue(key, out var count))
            {
                this.rangedConstraints[key] = count + 1;
                return;
            }

            this.rangedConstraints[key] = 1;
            this.manager.StartRangingBeacons(constraint);
        }
    }


    void StopRanging(string key, CLBeaconIdentityConstraint constraint)
    {
        lock (this.syncLock)
        {
            if (!this.rangedConstraints.TryGetValue(key, out var count))
                return;

            if (count > 1)
            {
                this.rangedConstraints[key] = count - 1;
                return;
            }

            this.rangedConstraints.Remove(key);
            this.manager.StopRangingBeacons(constraint);
        }
    }
}


/// <summary>
/// Bridges CoreLocation's ranging callbacks onto an observable.
/// </summary>
class BeaconRangingDelegate(BeaconRangingOptions options) : ShinyBeaconLocationDelegate
{
    readonly Subject<Beacon> rangeSubject = new();

    public IObservable<Beacon> WhenBeaconRanged() => this.rangeSubject;


    public override void DidRangeBeaconsSatisfyingConstraint(CLLocationManager manager, CLBeacon[] beacons, CLBeaconIdentityConstraint constraint)
    {
        foreach (var native in beacons)
            this.rangeSubject.OnNext(native.FromNative(options));
    }


#if !MACOS
    // The pre-iOS 13 callback - a device on an older OS still routes through it
    public override void DidRangeBeacons(CLLocationManager manager, CLBeacon[] beacons, CLBeaconRegion region)
    {
        foreach (var native in beacons)
            this.rangeSubject.OnNext(native.FromNative(options));
    }
#endif
}
