#if !MACOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Beacons;


/// <summary>
/// Monitors beacon regions with the classic CLLocationManager region API.
/// </summary>
/// <remarks>
/// <para>
/// Used on iOS and Mac Catalyst below 18, where CLMonitor does not exist. macOS never gets here -
/// CoreLocation has no beacon region monitoring on the Mac at all, only ranging.
/// </para>
/// <para>
/// iOS caps an app at 20 monitored regions across every kind - beacons, geofences, everything. The
/// limit is enforced here with a clear error rather than being left to CoreLocation, which silently
/// drops the excess.
/// </para>
/// </remarks>
public class CLLocationBeaconMonitoringManager : IBeaconMonitoringManager, IShinyStartupTask
{
    /// <summary>The number of regions iOS will monitor for an app, across all region types.</summary>
    public const int MaxMonitoredRegions = 20;

    readonly IRepository repository;
    readonly ILogger logger;
    readonly CLLocationManager manager;
    readonly BeaconMonitoringDelegate locationDelegate;


    /// <summary>
    /// Creates the manager.
    /// </summary>
    public CLLocationBeaconMonitoringManager(
        IRepository repository,
        IServiceProvider services,
        ILogger<IBeaconMonitoringManager> logger
    )
    {
        this.repository = repository;
        this.logger = logger;
        this.locationDelegate = new BeaconMonitoringDelegate(repository, services, logger);
        this.manager = new CLLocationManager { Delegate = this.locationDelegate };
    }


    /// <inheritdoc />
    public void Start()
    {
        try
        {
            // CoreLocation keeps monitored regions across launches, but the app can be reinstalled
            // or the stored set edited while it was not running, so the two are reconciled here.
            var stored = this.repository.GetAll<BeaconRegion>();
            if (stored.Count == 0)
                return;

            var native = this.manager.MonitoredRegions
                .OfType<CLBeaconRegion>()
                .Select(x => x.Identifier)
                .ToHashSet();

            foreach (var region in stored.Where(x => !native.Contains(x.Identifier)))
                this.manager.StartMonitoring(region.ToNative());
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error starting beacon monitoring");
        }
    }


    /// <inheritdoc />
    public AccessState CurrentStatus => CLLocationManager.IsMonitoringAvailable(typeof(CLBeaconRegion))
        ? this.manager.GetCurrentStatus(true)
        : AccessState.NotSupported;


    /// <inheritdoc />
    public Task<AccessState> RequestAccess()
        // background monitoring needs always-on authorization, unlike ranging
        => CLLocationManager.IsMonitoringAvailable(typeof(CLBeaconRegion))
            ? this.manager.RequestAccess(true)
            : Task.FromResult(AccessState.NotSupported);


    /// <inheritdoc />
    public IList<BeaconRegion> GetMonitoredRegions() => this.repository.GetAll<BeaconRegion>().ToList();


    /// <inheritdoc />
    public async Task StartMonitoring(BeaconRegion region)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        if (!this.repository.Exists<BeaconRegion>(region.Identifier) &&
            this.manager.MonitoredRegions.Count >= MaxMonitoredRegions)
        {
            throw new InvalidOperationException($"iOS monitors at most {MaxMonitoredRegions} regions per app, across beacons and geofences together. Stop monitoring one before adding another.");
        }

        this.repository.Set(region);
        this.manager.StartMonitoring(region.ToNative());
    }


    /// <inheritdoc />
    public Task StopMonitoring(string identifier)
    {
        var region = this.repository.Get<BeaconRegion>(identifier);
        if (region == null)
            return Task.CompletedTask;

        this.repository.Remove<BeaconRegion>(identifier);
        this.manager.StopMonitoring(region.ToNative());

        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task StopAllMonitoring()
    {
        this.repository.Clear<BeaconRegion>();

        foreach (var native in this.manager.MonitoredRegions.OfType<CLBeaconRegion>())
            this.manager.StopMonitoring(native);

        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken cancelToken = default)
    {
        var tcs = new TaskCompletionSource<BeaconRegionState>();
        var native = region.ToNative();

        EventHandler<CLRegionStateDeterminedEventArgs>? handler = null;
        handler = (_, args) =>
        {
            if (args.Region.Identifier != region.Identifier)
                return;

            this.locationDelegate.StateDetermined -= handler;
            tcs.TrySetResult(args.State switch
            {
                CLRegionState.Inside => BeaconRegionState.Entered,
                CLRegionState.Outside => BeaconRegionState.Exited,
                _ => BeaconRegionState.Unknown
            });
        };

        this.locationDelegate.StateDetermined += handler;
        cancelToken.Register(() =>
        {
            this.locationDelegate.StateDetermined -= handler;
            tcs.TrySetCanceled();
        });

        this.manager.RequestState(native);
        return tcs.Task;
    }
}


/// <summary>
/// Routes CoreLocation region transitions to the registered monitor delegates.
/// </summary>
class BeaconMonitoringDelegate(
    IRepository repository,
    IServiceProvider services,
    ILogger logger
) : ShinyBeaconLocationDelegate
{
    public event EventHandler<CLRegionStateDeterminedEventArgs>? StateDetermined;


    public override void RegionEntered(CLLocationManager manager, CLRegion region)
        => this.Fire(region, BeaconRegionState.Entered);

    public override void RegionLeft(CLLocationManager manager, CLRegion region)
        => this.Fire(region, BeaconRegionState.Exited);

    public override void DidDetermineState(CLLocationManager manager, CLRegionState state, CLRegion region)
        => this.StateDetermined?.Invoke(this, new CLRegionStateDeterminedEventArgs(state, region));

    public override void MonitoringFailed(CLLocationManager manager, CLRegion region, Foundation.NSError error)
        => logger.LogError("Beacon region monitoring failed for {Identifier} - {Error}", region?.Identifier, error?.LocalizedDescription);


    async void Fire(CLRegion native, BeaconRegionState state)
    {
        // async void: CoreLocation calls this from a native callback with nowhere to return a task,
        // so nothing may escape
        try
        {
            if (native is not CLBeaconRegion)
                return;

            // The stored region is preferred over one rebuilt from CoreLocation, because the
            // notify flags and the caller's own identifier only exist on this side.
            var region = repository.Get<BeaconRegion>(native.Identifier);
            if (region == null)
            {
                logger.LogWarning("Received a transition for unknown beacon region {Identifier}", native.Identifier);
                return;
            }

            if (state == BeaconRegionState.Entered && !region.NotifyOnEntry)
                return;

            if (state == BeaconRegionState.Exited && !region.NotifyOnExit)
                return;

            logger.LogInformation("Beacon region {Identifier} {State}", region.Identifier, state);

            await services
                .RunDelegates<IBeaconMonitorDelegate>(x => x.OnStatusChanged(state, region), logger)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching beacon region transition for {Identifier}", native.Identifier);
        }
    }
}
#endif
