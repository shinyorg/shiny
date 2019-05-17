#if WINDOWS_UWP || __ANDROID__
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE.Central;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
    public class BeaconManager : AbstractBeaconManager
    {
        readonly ICentralManager centralManager;
        readonly IDictionary<string, BeaconRegionStatus> regionStates;
        readonly Subject<BeaconRegionStatus> monitorSubject;
        IDisposable monitorScan;


        public BeaconManager(ICentralManager centralManager, IRepository repository) : base(repository)
        {
            this.centralManager = centralManager;
            //this.monitorSubject = new Subject<BeaconRegionStatusChanged>();
            this.regionStates = new Dictionary<string, BeaconRegionStatus>();
            repository
                .GetAll<BeaconRegion>()
                .ContinueWith(x =>
                {
                    foreach (var region in x.Result)
                        this.SetRegion(region);
                });
        }


        public override AccessState GetCurrentStatus(bool background) => this.centralManager.Status;
        public override Task<AccessState> RequestAccess(bool monitoring)
            => this.centralManager.RequestAccess().ToTask();


        public override IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => this.Scan().Where(region.IsBeaconInRegion);


        public override async Task StartMonitoring(BeaconRegion region)
        {
            await this.Repository.Set(region.Identifier, region);
            this.SetRegion(region);
        }


        public override async Task StopMonitoring(BeaconRegion region)
        {
            await this.Repository.Remove(region.Identifier);
            lock (this.regionStates)
                this.regionStates.Remove(region.Identifier);

            //if (this.regionStates.Count == 0)
            //    this.CleanupMonitoringScanner();
        }


        public override async Task StopAllMonitoring()
        {
            //this.CleanupMonitoringScanner();

            await this.Repository.Clear();
        }


        //protected void TryStartMonitorScanner()
        //{
        //    if (this.monitorScan != null)
        //        return;

        //    this.monitorScan = Observable.Create<BeaconRegionStatus>(ob =>
        //    {
        //        var internalScan = this.Scan()
        //            .Where(_ => this.regionStates.Count > 0)
        //            .Subscribe(beacon =>
        //            {
        //                var states = this.GetRegionStatesForBeacon(this.MonitoredRegions, beacon);
        //                foreach (var state in states)
        //                {
        //                    if (state.IsInRange != null && !state.IsInRange.Value)
        //                    {
        //                        //ob.OnNext(new BeaconRegionStatusChanged(state.Region, true));
        //                    }

        //                    state.IsInRange = true;
        //                    state.LastPing = DateTimeOffset.UtcNow;
        //                }
        //            });

        //        var cleanup = Observable
        //            .Interval(TimeSpan.FromSeconds(5)) // TODO: configurable
        //            .Subscribe(x =>
        //            {
        //                var maxAge = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(5));

        //                foreach (var state in this.regionStates.Values)
        //                {
        //                    if (state.IsInRange == true && state.LastPing > maxAge)
        //                    {
        //                        state.IsInRange = false;
        //                        ob.OnNext(state);
        //                    }
        //                }
        //            });

        //        return () =>
        //        {
        //            internalScan.Dispose();
        //            cleanup.Dispose();
        //        };
        //    })
        //    .Subscribe(this.monitorSubject.OnNext);
        //}


        //protected void CleanupMonitoringScanner()
        //{
        //    this.monitorScan?.Dispose();
        //    this.monitorScan = null;
        //}


        IObservable<Beacon> beaconScanner;
        protected IObservable<Beacon> Scan()
        {
            // TODO: switch to background scan
            this.beaconScanner = this.beaconScanner ?? this.centralManager
                .Scan()
                .Where(x => x.IsBeacon())
                .Select(x => x.ToBeacon())
                .Publish()
                .RefCount();

            return this.beaconScanner;
        }


        protected virtual BeaconRegionStatus SetRegion(BeaconRegion region)
        {
            var key = region.ToString();
            BeaconRegionStatus status = null;

            lock (this.regionStates)
            {
                if (this.regionStates.ContainsKey(key))
                {
                    status = this.regionStates[key];
                }
                else
                {
                    status = new BeaconRegionStatus(region);
                    this.regionStates.Add(key, status);
                }
            }
            //this.TryStartMonitorScanner();
            return status;
        }


        protected virtual IEnumerable<BeaconRegionStatus> GetRegionStatesForBeacon(IEnumerable<BeaconRegion> regionList, Beacon beacon)
        {
            var copy = this.regionStates.ToDictionary(x => x.Key, x => x.Value);

            foreach (var region in regionList)
            {
                var state = copy[region.ToString()];
                if (state.Region.IsBeaconInRegion(beacon))
                    yield return state;
            }
        }
    }
}
/*
 *  if (this.AdvertisedBeacon != null)
        throw new ArgumentException("You are already advertising a beacon");

    var settings = new AdvertiseSettings.Builder()
        .SetAdvertiseMode(AdvertiseMode.Balanced)
        .SetConnectable(false);

    var adData = new AdvertiseData.Builder()
        .AddManufacturerData(0x004C, beacon.ToIBeaconPacket(10)); // Apple

    this.manager
        .Adapter
        .BluetoothLeAdvertiser
        .StartAdvertising(
            settings.Build(),
            adData.Build(),
            this.adCallbacks
        );

    this.AdvertisedBeacon = beacon;
 */
#endif