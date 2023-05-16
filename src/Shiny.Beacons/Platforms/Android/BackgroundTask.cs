using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE;
using Shiny.Support.Repositories;

namespace Shiny.Beacons;


public class BackgroundTask : IDisposable
{
    readonly Dictionary<string, BeaconRegionStatus> states = new();
    IDisposable? repoSub;
    IDisposable? scanSub;

    readonly IRepository repository;
    readonly IBleManager bleManager;
    readonly ILogger logger;
    readonly IEnumerable<IBeaconMonitorDelegate> delegates;


    public BackgroundTask(
        IRepository repository,
        IBleManager centralManager,
        IEnumerable<IBeaconMonitorDelegate> delegates,
        ILogger<IBeaconMonitorDelegate> logger
    )
    {
        this.repository = repository;
        this.bleManager = centralManager;
        this.logger = logger;
        this.delegates = delegates;
    }


    public void Run()
    {
        this.logger.LogInformation("Starting Beacon Monitoring");

        this.repoSub = this.repository
            .WhenActionOccurs()
            .Where(x => x.Entity is BeaconRegion)
            .Subscribe(x =>
            {
                var e = (BeaconRegion)x.Entity;
                switch (x.Action)
                {
                    case RepositoryAction.Add:
                        if (this.states.Count == 0)
                        {
                            this.StartScan();
                        }
                        else
                        {
                            lock (this.states)
                            {
                                this.states.Add(e.Identifier, new BeaconRegionStatus(e));
                            }
                        }
                        break;

                    case RepositoryAction.Update:
                        // this actually shouldn't be allowed
                        break;

                    case RepositoryAction.Remove:
                        lock (this.states)
                        {
                            this.states.Remove(e.Identifier);
                            if (this.states.Count == 0)
                                this.StopScan();
                        }
                        break;

                    case RepositoryAction.Clear:
                        this.StopScan();
                        break;
                }
            });

        this.StartScan();
        this.logger.LogInformation("Beacon Monitoring Started Successfully");
    }


    public void StartScan()
    {
        if (this.scanSub != null)
            return;

        this.logger.LogInformation("Beacon Monitoring Scan Starting");
        var regions = this.repository.GetList<BeaconRegion>();
        if (!regions.Any())
            return;

        foreach (var region in regions)
            this.states.Add(region.Identifier, new BeaconRegionStatus(region));

        try
        {
            this.scanSub = this.bleManager
                .ScanForBeacons(true)
                .Buffer(TimeSpan.FromSeconds(5))
                .SubscribeAsyncConcurrent(this.CheckStates);

            this.logger.LogInformation("Beacon Monitoring Scan Started Successfully");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Beacon Monitoring Scan Starting");
        }
    }


    IList<BeaconRegionStatus> GetCopy()
    {
        lock (this.states)
        {
            return this.states
                .Select(x => x.Value)
                .ToList();
        }
    }


    public void StopScan()
    {
        if (this.scanSub == null)
            return;

        this.scanSub?.Dispose();
        this.scanSub = null;
        lock (this.states)
            this.states.Clear();

        this.logger.LogInformation("Beacon Monitoring Scan Stopped");
    }


    async Task CheckStates(IList<Beacon> beacons)
    {
        var copy = this.GetCopy();

        foreach (var state in copy)
        {
            foreach (var beacon in beacons)
            {
                if (state.Region.IsBeaconInRegion(beacon))
                {
                    state.LastPing = DateTime.UtcNow;
                    state.IsInRange ??= true;

                    if (!state.IsInRange.Value)
                    {
                        state.IsInRange = true;
                        if (state.Region.NotifyOnEntry)
                        {
                            await this.delegates
                                .RunDelegates(
                                    x => x.OnStatusChanged(BeaconRegionState.Entered, state.Region),
                                    this.logger
                                )
                                .ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        var cutoffTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(20));
        foreach (var state in copy)
        {
            if ((state.IsInRange ?? false) && state.LastPing < cutoffTime)
            {
                state.IsInRange = false;
                if (state.Region.NotifyOnExit)
                {
                    await this.delegates
                        .RunDelegates(
                            x => x.OnStatusChanged(
                                BeaconRegionState.Exited,
                                state.Region
                            ),
                            this.logger
                        )
                        .ConfigureAwait(false);
                }
            }
        }
    }


    public void Dispose()
    {
        this.repoSub?.Dispose();
        this.StopScan();
    }
}
