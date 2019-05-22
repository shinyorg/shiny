#if WINDOWS_UWP || __ANDROID__
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.Infrastructure;
using Shiny.BluetoothLE.Central;
using Shiny.Logging;


namespace Shiny.Beacons
{
    public class BackgroundTask
    {
        readonly IDictionary<string, BeaconRegionStatus> regionStates;

        readonly ICentralManager centralManager;
        readonly IBeaconManager beaconManager;
        readonly IRepository repository;
        IDisposable scanSub;

        //this.regionStates = new Dictionary<string, BeaconRegionStatus>();


        public BackgroundTask(ICentralManager centralManager,
                              IBeaconManager beaconManager,
                              IRepository repository)
        {
            this.centralManager = centralManager;
            this.beaconManager = beaconManager;
            this.repository = repository;
        }


        public void Run()
        {
            this.repository.WhenEvent().Subscribe(async ev =>
            {
                switch (ev.Type)
                {
                    case RepositoryEventType.Add:
                        this.StartScan();
                        break;

                    case RepositoryEventType.Remove:
                        this.ToggleIfApplicable();
                        break;

                    case RepositoryEventType.Clear:
                        this.StopScan();
                        break;
                }
            });
            // TODO: build the states
            this.ToggleIfApplicable();
        }


        async void ToggleIfApplicable()
        {
            var regions = await this.beaconManager.GetMonitoredRegions();
            if (regions.Any())
                this.StartScan();
            else
                this.StopScan();
        }


        void StartScan()
        {
            if (this.scanSub != null)
                return;

            List<Beacon> lastScan = null;
            this.scanSub = this.centralManager
                .ScanForBeacons(true)
                .Buffer(TimeSpan.FromSeconds(4))
                .Subscribe(
                    x =>
                    {
                        // TODO: get distinct beacons
                        //if (lastScan == null)
                            // don't do anything - just store

                        // TODO: compare wave against last wave
                            // TODO: new found beacons becomes an entry - if not already in region
                        // TODO: non-found beacons become an exit - if there aren't others in the region
                    },
                    ex => Log.Write(ex)
                );
        }


        void StopScan()
        {
            this.scanSub?.Dispose();
            this.scanSub = null;
        }
    }
}
#endif

/*
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


//protected virtual BeaconRegionStatus SetRegion(BeaconRegion region)
//{
//    var key = region.ToString();
//    BeaconRegionStatus status = null;

//    lock (this.regionStates)
//    {
//        if (this.regionStates.ContainsKey(key))
//        {
//            status = this.regionStates[key];
//        }
//        else
//        {
//            status = new BeaconRegionStatus(region);
//            this.regionStates.Add(key, status);
//        }
//    }
//    //this.TryStartMonitorScanner();
//    return status;
//}


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
     */
