using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Beacons.Infrastructure;
using Shiny.BluetoothLE;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Beacons;


/// <summary>
/// Monitors beacon regions by keeping a low-power BLE scan running and inferring transitions from
/// what it hears.
/// </summary>
/// <remarks>
/// Used on Android, Windows, Linux and any other host without an OS-level beacon region API. On
/// Android a foreground service keeps this alive while the app is backgrounded; on desktop the
/// process simply has to stay running.
/// </remarks>
public class BleScanBeaconMonitoringManager : IBeaconMonitoringManager, IDisposable
{
    readonly IBleManager bleManager;
    readonly IRepository repository;
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly BeaconRangingOptions options;
    readonly BeaconRegionMonitor monitor;
    readonly SemaphoreSlim scanLock = new(1, 1);

    CompositeDisposable? scanSubscription;


    /// <summary>
    /// Creates the manager.
    /// </summary>
    public BleScanBeaconMonitoringManager(
        IBleManager bleManager,
        IRepository repository,
        IServiceProvider services,
        BeaconRangingOptions options,
        ILogger<IBeaconMonitoringManager> logger
    )
    {
        this.bleManager = bleManager;
        this.repository = repository;
        this.services = services;
        this.options = options;
        this.logger = logger;
        this.monitor = new BeaconRegionMonitor(options);
        this.monitor.SetRegions(this.repository.GetAll<BeaconRegion>());
    }


    /// <inheritdoc />
    public AccessState CurrentStatus => this.bleManager.CurrentAccess;

    /// <inheritdoc />
    public virtual Task<AccessState> RequestAccess() => this.bleManager.RequestAccess().ToTask();

    /// <inheritdoc />
    public IList<BeaconRegion> GetMonitoredRegions() => this.repository.GetAll<BeaconRegion>().ToList();


    /// <inheritdoc />
    public virtual async Task StartMonitoring(BeaconRegion region)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        this.repository.Set(region);
        this.monitor.AddRegion(region);
        await this.StartScan().ConfigureAwait(false);
    }


    /// <inheritdoc />
    public virtual async Task StopMonitoring(string identifier)
    {
        this.repository.Remove<BeaconRegion>(identifier);
        this.monitor.RemoveRegion(identifier);

        if (this.monitor.Count == 0)
            await this.StopScan().ConfigureAwait(false);
    }


    /// <inheritdoc />
    public virtual async Task StopAllMonitoring()
    {
        this.repository.Clear<BeaconRegion>();
        this.monitor.Clear();
        await this.StopScan().ConfigureAwait(false);
    }


    /// <inheritdoc />
    public Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken cancelToken = default)
        => Task.FromResult(this.monitor.GetState(region.Identifier));


    /// <summary>
    /// Brings the scan up when there is at least one region to watch. Safe to call repeatedly.
    /// </summary>
    protected async Task StartScan()
    {
        await this.scanLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (this.scanSubscription != null || this.monitor.Count == 0)
                return;

            this.logger.LogInformation("Starting beacon region monitoring scan");

            var scanner = new BleBeaconScanner(this.bleManager, this.options, BeaconScanConfigFactory.Create(true));

            this.scanSubscription =
            [
                scanner
                    .WhenBeaconSeen()
                    .Subscribe(
                        beacon => this.Fire(this.monitor.Report(beacon.Identity)),
                        ex => this.logger.LogError(ex, "Beacon monitoring scan failed")
                    ),

                // BLE never reports a departure, so exits are found by re-checking on a timer.
                // This runs off TimeProvider rather than an Rx interval so tests can drive it -
                // Rx has no TimeProvider-based scheduler.
                this.options.TimeProvider.CreateTimer(
                    _ => this.Fire(this.monitor.Evaluate()),
                    null,
                    this.options.RegionEvaluationInterval,
                    this.options.RegionEvaluationInterval
                )
            ];
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to start beacon region monitoring scan");
        }
        finally
        {
            this.scanLock.Release();
        }
    }


    /// <summary>
    /// Tears the scan down.
    /// </summary>
    protected async Task StopScan()
    {
        await this.scanLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (this.scanSubscription == null)
                return;

            this.scanSubscription.Dispose();
            this.scanSubscription = null;
            this.logger.LogInformation("Stopped beacon region monitoring scan");
        }
        finally
        {
            this.scanLock.Release();
        }
    }


    void Fire(IReadOnlyList<BeaconRegionTransition> transitions)
    {
        if (transitions.Count == 0)
            return;

        // fire-and-forget: this runs from a scan callback, and a delegate that throws or blocks
        // must not be able to take the scan down with it
        _ = Task.Run(async () =>
        {
            foreach (var transition in transitions)
            {
                this.logger.LogInformation(
                    "Beacon region {Identifier} {State}",
                    transition.Region.Identifier,
                    transition.State
                );

                await this.services
                    .RunDelegates<IBeaconMonitorDelegate>(
                        x => x.OnStatusChanged(transition.State, transition.Region),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
        });
    }


    /// <inheritdoc />
    public void Dispose()
    {
        this.scanSubscription?.Dispose();
        this.scanSubscription = null;
        this.scanLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
