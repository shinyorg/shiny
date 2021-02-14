using System;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;


namespace Shiny.Beacons
{
    public class ManagedScan
    {
        readonly IBeaconRangingManager beaconManager;
        IDisposable? scanSub;


        public ManagedScan(IBeaconRangingManager beaconManager)
            => this.beaconManager = beaconManager;


        public ObservableCollection<Beacon> Beacons { get; } = new ObservableCollection<Beacon>();
        public BeaconRegion? ScanningRegion { get; private set; }
        public bool IsScanning => this.scanSub != null;


        // TODO: timespan to remove from list
        public void Start(BeaconRegion scanRegion, IScheduler? scheduler = null)
        {
            if (this.IsScanning)
                return;

            this.ScanningRegion = scanRegion;
            var ob = this.beaconManager
                .WhenBeaconRanged(scanRegion)
                .ObserveOnIf(scheduler)
                .Subscribe(beacon =>
                {
                    // TODO: update proximity, add if not already in list
                });
        }


        public void Stop()
        {
            this.scanSub?.Dispose();
            this.scanSub = null;
            this.ScanningRegion = null;
        }
    }


    public static class Extensions_ManagedScan
    {
        public static ManagedScan CreateManagedScan(this IBeaconRangingManager manager)
            => new ManagedScan(manager);
    }
}
