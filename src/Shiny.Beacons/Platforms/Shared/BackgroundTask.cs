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
        readonly ICentralManager centralManager;
        readonly IBeaconManager beaconManager;
        readonly IRepository repository;
        readonly IBeaconDelegate beaconDelegate;
        readonly IDictionary<string, BeaconRegionStatus> states;
        IDisposable scanSub;
        IDisposable cleanSub;


        public BackgroundTask(ICentralManager centralManager,
                              IBeaconManager beaconManager,
                              IRepository repository,
                              IBeaconDelegate beaconDelegate)
        {
            this.centralManager = centralManager;
            this.beaconManager = beaconManager;
            this.repository = repository;
            this.beaconDelegate = beaconDelegate;
            this.states = new Dictionary<string, BeaconRegionStatus>();
        }


        public void Run()
        {
            this.repository.WhenEvent().Subscribe(async ev =>
            {
                switch (ev.Type)
                {
                    case RepositoryEventType.Add:
                        lock (this.states)
                        {
                            var region = (BeaconRegion)ev.Entity;
                            this.states.Add(ev.Key, new BeaconRegionStatus(region));
                        }
                        this.StartScan();
                        break;

                    case RepositoryEventType.Remove:
                        lock (this.states)
                            this.states.Remove(ev.Key);
                        break;

                    case RepositoryEventType.Clear:
                        this.StopScan();
                        break;
                }
            });
            this.StartScan();
        }


        async void StartScan()
        {
            if (this.scanSub != null)
                return;

            var regions = await this.beaconManager.GetMonitoredRegions();
            if (!regions.Any())
                return;

            foreach (var region in regions)
                this.states.Add(region.Identifier, new BeaconRegionStatus(region));

            this.scanSub = this.centralManager
                .ScanForBeacons(true)
                .Buffer(TimeSpan.FromSeconds(4))
                .Subscribe(
                    this.CheckStates,
                    ex => Log.Write(ex)
                );

            this.cleanSub = Observable
                .Interval(TimeSpan.FromSeconds(5)) // TODO: configurable
                .Subscribe(() => this.TimeOutRegions());
        }


        IList<BeaconRegionStatus> GetCopy()
        {
            lock (this.states)
                return this.states
                    .Select(x => x.Value)
                    .ToList();
        }

        void StopScan()
        {
            this.scanSub?.Dispose();
            this.cleanSub?.Dispose();
            this.states.Clear();
            this.scanSub = null;
        }


        void CheckStates(IList<Beacon> beacons)
        {
            foreach (var beacon in beacons)
            {
                var copy = this.GetCopy();

                foreach (var state in copy)
                {
                    var ranged = state.Region.IsBeaconInRegion(beacon);
                    var fireChange = state.IsInRange != null && state.IsInRange != ranged;

                    if (ranged)
                    {
                        state.IsInRange = true;
                        state.LastPing = DateTime.UtcNow;
                    }
                    if (fireChange)
                        this.FireDelegate(ranged, state.Region);
                }
            }
        }


        void TimeOutRegions()
        {
            var maxAge = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(5)); //TODO: configurable
            var copy = this.GetCopy();

            foreach (var state in copy)
            {
                if (state.IsInRange == true && state.LastPing > maxAge)
                {
                    state.IsInRange = false;
                    this.FireDelegate(false, state.Region);
                }
            }
        }

        void FireDelegate(bool ranged, BeaconRegion region)
        {
            var newState = ranged ? BeaconRegionState.Entered : BeaconRegionState.Exited;
            try
            {
                this.beaconDelegate.OnStatusChanged(newState, region);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
#endif
