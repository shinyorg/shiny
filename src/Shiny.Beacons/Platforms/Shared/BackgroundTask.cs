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
            this.repository
                .WhenEvent()
                .Where(x => x.EntityType == typeof(BeaconRegion))
                .Subscribe(ev =>
                {
                    switch (ev.Type)
                    {
                        case RepositoryEventType.Add:
                            if (this.states.Count == 0)
                                this.StartScan();
                            else
                            {
                                lock (this.states)
                                {
                                    var region = (BeaconRegion)ev.Entity;
                                    this.states.Add(ev.Key, new BeaconRegionStatus(region));
                                }
                            }
                            break;

                        case RepositoryEventType.Update:
                            // TODO: this actually shouldn't be allowed
                            break;

                        case RepositoryEventType.Remove:
                            lock (this.states)
                            {
                                this.states.Remove(ev.Key);
                                if (this.states.Count == 0)
                                    this.StopScan();
                            }
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

            try
            {
                this.scanSub = this.centralManager
                    .ScanForBeacons(true)
                    .Buffer(TimeSpan.FromSeconds(4))
                    .Subscribe(
                        this.CheckStates,
                        ex => Log.Write(ex)
                    );
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
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
            this.states.Clear();
            this.scanSub = null;
            lock (this.states)
                this.states.Clear();
        }


        void CheckStates(IList<Beacon> beacons)
        {
            var copy = this.GetCopy();
            var maxAge = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10)); //TODO: configurable

            foreach (var state in copy)
            {
                foreach (var beacon in beacons)
                {
                    if (state.Region.IsBeaconInRegion(beacon))
                    {
                        state.LastPing = DateTime.UtcNow;
                        if (state.IsInRange == null)
                        {
                            state.IsInRange = true;
                        }
                        else if (!state.IsInRange.Value)
                        {
                            state.IsInRange = true;
                            if (state.Region.NotifyOnEntry)
                                this.FireDelegate(BeaconRegionState.Entered, state.Region);
                        }
                    }
                }
            }
            foreach (var state in copy)
            {
                if (state.IsInRange != null && state.IsInRange.Value && state.LastPing < maxAge)
                {
                    state.IsInRange = false;
                    if (state.Region.NotifyOnExit)
                        this.FireDelegate(BeaconRegionState.Exited, state.Region);
                }
            }
        }


        void FireDelegate(BeaconRegionState newState, BeaconRegion region)
        {
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
