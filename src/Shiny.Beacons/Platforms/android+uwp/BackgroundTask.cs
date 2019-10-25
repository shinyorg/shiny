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
        readonly IMessageBus messageBus;
        readonly IBeaconDelegate beaconDelegate;
        readonly IDictionary<string, BeaconRegionStatus> states;
        IDisposable scanSub;


        public BackgroundTask(ICentralManager centralManager,
                              IBeaconManager beaconManager,
                              IMessageBus messageBus,
                              IRepository repository,
                              IBeaconDelegate beaconDelegate)
        {
            this.centralManager = centralManager;
            this.beaconManager = beaconManager;
            this.messageBus = messageBus;
            this.repository = repository;
            this.beaconDelegate = beaconDelegate;
            this.states = new Dictionary<string, BeaconRegionStatus>();
        }


        public void Run()
        {
            Log.Write(BeaconLogCategory.Task, "Starting");

            // TODO: I should record state of the beacon region so I can fire stuff without going into initial state from unknown
            this.messageBus
                .Listener<BeaconRegisterEvent>()
                .Subscribe(ev =>
                {
                    switch (ev.Type)
                    {
                        case BeaconRegisterEventType.Add:
                            if (this.states.Count == 0)
                                this.StartScan();
                            else
                            {
                                lock (this.states)
                                {
                                    this.states.Add(ev.Region.Identifier, new BeaconRegionStatus(ev.Region));
                                }
                            }
                            break;

                        case BeaconRegisterEventType.Update:
                            // TODO: this actually shouldn't be allowed
                            break;

                        case BeaconRegisterEventType.Remove:
                            lock (this.states)
                            {
                                this.states.Remove(ev.Region.Identifier);
                                if (this.states.Count == 0)
                                    this.StopScan();
                            }
                            break;

                        case BeaconRegisterEventType.Clear:
                            this.StopScan();
                            break;
                    }
                });

            this.StartScan();
            Log.Write(BeaconLogCategory.Task, "Started");
        }


        public async void StartScan()
        {
            if (this.scanSub != null)
                return;

            Log.Write(BeaconLogCategory.Task, "Scan Starting");
            var regions = await this.beaconManager.GetMonitoredRegions();
            if (!regions.Any())
                return;

            foreach (var region in regions)
                this.states.Add(region.Identifier, new BeaconRegionStatus(region));

            try
            {
                this.scanSub = this.centralManager
                    .ScanForBeacons(true)
                    .Buffer(TimeSpan.FromSeconds(5))
                    .Subscribe(
                        this.CheckStates,
                        ex => Log.Write(ex)
                    );

                Log.Write(BeaconLogCategory.Task, "Scan Started");
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


        public void StopScan()
        {
            if (this.scanSub == null)
                return;

            Log.Write(BeaconLogCategory.Task, "Scan Stopping");
            this.scanSub?.Dispose();
            this.states.Clear();
            this.scanSub = null;
            lock (this.states)
                this.states.Clear();

            Log.Write(BeaconLogCategory.Task, "Scan Stopped");
        }


        void CheckStates(IList<Beacon> beacons)
        {
            var copy = this.GetCopy();
            var maxAge = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(20)); //TODO: configurable

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
