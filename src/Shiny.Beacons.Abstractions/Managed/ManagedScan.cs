using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Shiny.Beacons.Managed;


public class ManagedScan : IDisposable
{
    readonly IBeaconRangingManager beaconManager;
    IScheduler? scheduler;
    IDisposable? clearSub;
    IDisposable? scanSub;


    public ManagedScan(IBeaconRangingManager beaconManager)
        => this.beaconManager = beaconManager;


    public ObservableCollection<ManagedBeacon> Beacons { get; } = new();
    public BeaconRegion? ScanningRegion { get; private set; }
    public bool IsScanning => this.ScanningRegion != null;


    TimeSpan? clearTime;
    public TimeSpan? ClearTime
    {
        get => this.clearTime;
        set
        {
            this.clearTime = value;
            this.clearSub?.Dispose();

            if (value != null)
            {
                this.clearSub = Observable
                    .Interval(TimeSpan.FromSeconds(5))
                    .ObserveOnIf(this.scheduler)
                    .Synchronize(this.Beacons)
                    .Subscribe(_ =>
                    {
                        var maxAge = DateTimeOffset.UtcNow.Subtract(value.Value);
                        var tmp = this.Beacons.Where(x => x.LastSeen < maxAge).ToList();
                        foreach (var beacon in tmp)
                            this.Beacons.Remove(beacon);
                    });
            }
        }
    }

    public async Task Start(BeaconRegion scanRegion, IScheduler? scheduler = null)
    {
        if (this.IsScanning)
            throw new ArgumentException("A beacon scan is already running");

        (await this.beaconManager.RequestAccess()).Assert();

        this.scheduler = scheduler;
        this.ScanningRegion = scanRegion;
        this.Beacons.Clear();

        // restart clear if applicable
        this.ClearTime = this.ClearTime;

        this.scanSub = this.beaconManager
            .WhenBeaconRanged(scanRegion)
            .Buffer(TimeSpan.FromSeconds(2))
            .ObserveOnIf(this.scheduler)
            .Synchronize(this.Beacons)
            .Subscribe(beacons =>
            {
                foreach (var beacon in beacons)
                {
                    var managed = this.Beacons.FirstOrDefault(x => x.Beacon.Equals(beacon));
                    if (managed == null)
                    {
                        managed = new ManagedBeacon(beacon, scanRegion.Identifier);
                        this.Beacons.Add(managed);
                    }
                    managed.Proximity = beacon.Proximity;
                    managed.LastSeen = DateTimeOffset.UtcNow;
                }
            });
    }


    public void Stop()
    {
        this.clearSub?.Dispose();
        this.scanSub?.Dispose();
        this.scheduler = null;
        this.ScanningRegion = null;
    }


    public void Dispose()
    {
        this.Stop();
        this.Beacons.Clear();
    }
}
