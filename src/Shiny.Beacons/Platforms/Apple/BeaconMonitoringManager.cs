#if !MACOS
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Beacons;


/// <summary>
/// Monitors beacon regions with CoreLocation's CLMonitor.
/// </summary>
/// <remarks>
/// <para>
/// This is the modern path - iOS 18, Mac Catalyst 18 and macOS 15 - and it mirrors what
/// Shiny.Locations already does for geofences, right down to needing a CLServiceSession before
/// CoreLocation will deliver anything to a backgrounded app.
/// </para>
/// <para>
/// CLMonitor persists its conditions at the OS level, so it re-attaches and replays their current
/// state on a cold start. Those first fires are suppressed; only genuine changes reach the delegate.
/// </para>
/// </remarks>
[SupportedOSPlatform("ios18.0")]
[SupportedOSPlatform("maccatalyst18.0")]
[SupportedOSPlatform("macos15.0")]
public class BeaconMonitoringManager(
    IRepository repository,
    IServiceProvider services,
    IPlatform platform,
    ILogger<IBeaconMonitoringManager> logger
) : IBeaconMonitoringManager, IShinyStartupTask
{
    const string MonitorName = "shinybeacons";

    volatile CLMonitor? monitor;
    readonly SemaphoreSlim monitorLock = new(1, 1);
    readonly ConcurrentDictionary<string, byte> initialFires = new();
    CLServiceSession? session;
    TaskCompletionSource<AccessState>? authTcs;


    /// <inheritdoc />
    public async void Start()
    {
        try
        {
            var regions = repository.GetAll<BeaconRegion>();
            if (regions.Count == 0)
                return;

            this.EnsureSession();

            var mon = await this.GetMonitor().ConfigureAwait(false);
            await platform.InvokeOnMainThreadAsync(() => this.Reconcile(mon, regions)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting beacon monitoring");
        }
    }


    // Must run on the queue CLMonitor was configured with - touching MonitoredIdentifiers from
    // anywhere else native-crashes the process.
    void Reconcile(CLMonitor mon, IReadOnlyList<BeaconRegion> regions)
    {
        var native = new HashSet<string>(mon.MonitoredIdentifiers ?? []);
        var stored = new HashSet<string>(regions.Select(x => x.Identifier));

        foreach (var orphan in native.Except(stored))
        {
            logger.LogInformation("Removing orphaned beacon condition {Identifier}", orphan);
            mon.RemoveCondition(orphan);
        }

        foreach (var region in regions.Where(x => !native.Contains(x.Identifier)))
            this.AddToMonitor(mon, region);
    }


    /// <inheritdoc />
    public AccessState CurrentStatus { get; private set; } = AccessState.Unknown;


    /// <inheritdoc />
    public async Task<AccessState> RequestAccess()
    {
        if (this.CurrentStatus != AccessState.Unknown)
            return this.CurrentStatus;

        this.authTcs ??= new TaskCompletionSource<AccessState>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var registration = cts.Token.Register(
            () => this.authTcs.TrySetException(new TimeoutException("Beacon monitoring authorization request timed out"))
        );

        this.EnsureSession();
        return await this.authTcs.Task.ConfigureAwait(false);
    }


    /// <inheritdoc />
    public IList<BeaconRegion> GetMonitoredRegions() => repository.GetAll<BeaconRegion>().ToList();


    /// <inheritdoc />
    public async Task StartMonitoring(BeaconRegion region)
    {
        this.EnsureSession();

        var mon = await this.GetMonitor().ConfigureAwait(false);
        await platform.InvokeOnMainThreadAsync(() => this.AddToMonitor(mon, region)).ConfigureAwait(false);
        repository.Set(region);
    }


    /// <inheritdoc />
    public async Task StopMonitoring(string identifier)
    {
        repository.Remove<BeaconRegion>(identifier);

        var mon = await this.GetMonitor().ConfigureAwait(false);
        await platform.InvokeOnMainThreadAsync(() => mon.RemoveCondition(identifier)).ConfigureAwait(false);

        if (repository.GetAll<BeaconRegion>().Count == 0)
            await this.DestroyMonitor().ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task StopAllMonitoring()
    {
        await this.DestroyMonitor().ConfigureAwait(false);
        repository.Clear<BeaconRegion>();
    }


    /// <inheritdoc />
    public Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken cancelToken = default)
        => platform.InvokeTaskOnMainThread(async () =>
        {
            var mon = await this.GetMonitor().ConfigureAwait(false);
            var record = mon.GetMonitoringRecord(region.Identifier);

            return record?.LastEvent.State switch
            {
                CLMonitoringState.Satisfied => BeaconRegionState.Entered,
                CLMonitoringState.Unsatisfied => BeaconRegionState.Exited,
                _ => BeaconRegionState.Unknown
            };
        }, cancelToken);


    void AddToMonitor(CLMonitor mon, BeaconRegion region)
    {
        if (region is { NotifyOnEntry: false, NotifyOnExit: false })
            throw new InvalidOperationException("The region is not set to notify on entry or exit");

        // conditions are persisted by the OS, so re-registering an identifier has to replace
        if (mon.GetMonitoringRecord(region.Identifier) != null)
        {
            logger.LogInformation("Replacing existing beacon condition {Identifier}", region.Identifier);
            mon.RemoveCondition(region.Identifier);
        }

        this.initialFires.TryAdd(region.Identifier, 0);
        mon.AddCondition(region.ToCondition(), region.Identifier);
    }


    void EnsureSession()
    {
        if (this.session != null)
            return;

        // Background beacon monitoring is an "always" capability; a session is what tells
        // CoreLocation this app intends to keep monitoring while it is not in front of the user.
        this.session = CLServiceSession.CreateSession(
            CLServiceSessionAuthorizationRequirement.Always,
            String.Empty,
            DispatchQueue.MainQueue,
            diagnostic =>
            {
                // runs on MainQueue as an Action - an unhandled throw here terminates the app
                try
                {
                    if (diagnostic is null || diagnostic.AuthorizationRequestInProgress)
                        return;

                    this.CurrentStatus = diagnostic.AuthorizationDenied || diagnostic.AlwaysAuthorizationDenied
                        ? AccessState.Denied
                        : AccessState.Available;

                    this.authTcs?.TrySetResult(this.CurrentStatus);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling CLServiceSession diagnostic");
                }
            }
        );
    }


    async ValueTask<CLMonitor> GetMonitor()
    {
        if (this.monitor != null)
            return this.monitor;

        await this.monitorLock.WaitAsync().ConfigureAwait(false);
        try
        {
            this.monitor ??= await this.RequestMonitorWithRetry().ConfigureAwait(false);
            return this.monitor;
        }
        finally
        {
            this.monitorLock.Release();
        }
    }


    async Task<CLMonitor> RequestMonitorWithRetry()
    {
        // Only one open CLMonitor per name per process is allowed. After iOS background-launches
        // the app for a beacon event the previous process's instance can still count as open, and
        // RequestMonitor throws NSInternalInconsistencyException until it is reaped.
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var mon = await CLMonitor.RequestMonitorAsync(this.BuildConfiguration()).ConfigureAwait(false);

                await platform.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var id in mon.MonitoredIdentifiers ?? [])
                        this.initialFires.TryAdd(id, 0);
                }).ConfigureAwait(false);

                return mon;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "CLMonitor.RequestMonitor failed (attempt {Attempt}/{Max}) - retrying", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt)).ConfigureAwait(false);
            }
        }
        throw new InvalidOperationException($"Failed to acquire CLMonitor after {maxAttempts} attempts");
    }


    CLMonitorConfiguration BuildConfiguration() => CLMonitorConfiguration.Create(
        MonitorName,
        DispatchQueue.MainQueue,
        async (mon, evt) =>
        {
            // runs on MainQueue as a void-returning delegate - an unhandled throw terminates the app
            try
            {
                if (evt == null)
                    return;

                if (evt.ConditionUnsupported)
                {
                    // macOS in particular may refuse beacon conditions outright
                    logger.LogWarning("CoreLocation reports beacon condition {Identifier} is unsupported on this platform", evt.Identifier);
                    return;
                }

                if (this.initialFires.TryRemove(evt.Identifier, out _))
                {
                    logger.LogDebug("Suppressed initial state fire for beacon region {Identifier}", evt.Identifier);
                    return;
                }

                var region = repository.Get<BeaconRegion>(evt.Identifier);
                if (region == null)
                    return;

                switch (evt.State)
                {
                    case CLMonitoringState.Satisfied when region.NotifyOnEntry:
                        await this.Fire(region, BeaconRegionState.Entered).ConfigureAwait(false);
                        break;

                    case CLMonitoringState.Unsatisfied when region.NotifyOnExit:
                        await this.Fire(region, BeaconRegionState.Exited).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling beacon monitoring event for {Identifier}", evt?.Identifier);
            }
        }
    );


    Task Fire(BeaconRegion region, BeaconRegionState state)
    {
        logger.LogInformation("Beacon region {Identifier} {State}", region.Identifier, state);

        return services.RunDelegates<IBeaconMonitorDelegate>(
            x => x.OnStatusChanged(state, region),
            logger
        );
    }


    async ValueTask DestroyMonitor()
    {
        await this.monitorLock.WaitAsync().ConfigureAwait(false);
        try
        {
            this.monitor?.Dispose();
            this.monitor = null;
            this.initialFires.Clear();

            this.session?.Invalidate();
            this.session = null;
            this.authTcs = null;
            this.CurrentStatus = AccessState.Unknown;
        }
        finally
        {
            this.monitorLock.Release();
        }
    }
}
#endif
