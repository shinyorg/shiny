using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;


namespace Shiny.Beacons.Managed
{
    public class ManagedScan : IDisposable
    {
        readonly IBeaconRangingManager beaconManager;
        IScheduler? scheduler;
        CompositeDisposable? composite;


        public ManagedScan(IBeaconRangingManager beaconManager)
            => this.beaconManager = beaconManager;


        public ObservableCollection<ManagedBeacon> Beacons { get; } = new ObservableCollection<ManagedBeacon>();
        public BeaconRegion? ScanningRegion { get; private set; }
        public bool IsScanning => this.ScanningRegion != null;


        public void Start(BeaconRegion scanRegion, TimeSpan? timeToClear, IScheduler? scheduler = null)
        {
            if (this.IsScanning)
                throw new ArgumentException("A beacon scan is already running");

            this.scheduler = scheduler;
            this.ScanningRegion = scanRegion;
            this.Beacons.Clear();

            this.composite = new CompositeDisposable();
            this.beaconManager
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
                            managed = new ManagedBeacon(beacon);
                            this.Beacons.Add(managed);
                        }
                        managed.Proximity = beacon.Proximity;
                        managed.LastSeen = DateTimeOffset.UtcNow;
                    }
                })
                .DisposedBy(this.composite);

            if (timeToClear != null)
            {
                Observable
                    .Interval(timeToClear.Value)
                    .ObserveOnIf(this.scheduler)
                    .Synchronize(this.Beacons)
                    .Subscribe(_ =>
                    {
                        var maxAge = DateTimeOffset.UtcNow.Subtract(timeToClear.Value);
                        var tmp = this.Beacons.Where(x => x.LastSeen < maxAge).ToList();
                        foreach (var beacon in tmp)
                            this.Beacons.Remove(beacon);
                    })
                    .DisposedBy(this.composite);
            }
        }


        public void Stop()
        {
            this.composite?.Dispose();
            this.scheduler = null;
            this.ScanningRegion = null;
        }


        public void Dispose()
        {
            this.Stop();
            this.Beacons.Clear();
        }
    }
}
