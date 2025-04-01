using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.Collections;

namespace Shiny.Beacons.Managed;


public class ManagedScan(IBeaconRangingManager beaconManager) : IDisposable
{
    IScheduler? scheduler;
    IDisposable? clearSub;
    IDisposable? scanSub;


    readonly BindingList<ManagedBeacon> beacons = new();
    public INotifyReadOnlyCollection<ManagedBeacon> Beacons => this.beacons;
    
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
                        if (tmp.Any())
                            this.beacons.RemoveRange(tmp);
                    });
            }
        }
    }

    public async Task Start(BeaconRegion scanRegion, IScheduler? scheduler = null)
    {
        if (this.IsScanning)
            throw new ArgumentException("A beacon scan is already running");

        (await beaconManager.RequestAccess()).Assert();

        this.scheduler = scheduler;
        this.ScanningRegion = scanRegion;
        this.beacons.Clear();

        // restart clear if applicable
        this.ClearTime = this.ClearTime;

        this.scanSub = beaconManager
            .WhenBeaconRanged(scanRegion)
            .Buffer(TimeSpan.FromSeconds(2))
            .ObserveOnIf(this.scheduler)
            .Subscribe(scanList =>
            {
                var add = new List<ManagedBeacon>();
                foreach (var beacon in scanList)
                {
                    var managed = this.beacons.FirstOrDefault(x => x.Beacon.Equals(beacon));
                    if (managed == null)
                    {
                        managed = new ManagedBeacon(beacon, scanRegion.Identifier);
                        add.Add(managed);
                    }
                    managed.Proximity = beacon.Proximity;
                    managed.LastSeen = DateTimeOffset.UtcNow;
                }
                if (add.Count > 0)
                    this.beacons.AddRange(add);
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
        this.beacons.Clear();
    }
}
