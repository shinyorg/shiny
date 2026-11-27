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
/// Shiny.Geofencing already does for geofences, right down to needing a CLServiceSession before
/// CoreLocation will deliver anything to a backgrounded app.
/// </para>
/// <para>
/// CLMonitor persists its conditions at the OS level, so it re-attaches and replays their current
/// state on a cold start. Those first fires are suppressed; only genuine changes reach the delegate.
/// A condition added by <see cref="StartMonitoring"/> is different: its first event is the current
/// state, so a device already inside the region reports <see cref="BeaconRegionState.Entered"/>.
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
    readonly ConcurrentDictionary<string, byte> freshConditions = new();
    CLServiceSession? session;


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
    // read live, like CLLocationBeaconMonitoringManager - it used to stay Unknown until RequestAccess or a session
    // callback ran
    public AccessState CurrentStatus
    {
        get
        {
            using var locationManager = new CLLocationManager();
            return locationManager.GetCurrentStatus(true);
        }
    }


    /// <inheritdoc />
    public async Task<AccessState> RequestAccess()
    {
        // CLServiceSession only reports whether always is held - it never prompts - so ask the way
        // CLLocationBeaconMonitoringManager does: when-in-use first, then the upgrade to always
        var status = await platform
            .InvokeTaskOnMainThread(() => this.AuthManager.RequestAccess(true))
            .ConfigureAwait(false);

        return status;
    }


    // only touched on the main thread - CoreLocation delivers a manager's callbacks to the run loop it was created on
    CLLocationManager AuthManager => field ??= new CLLocationManager { Delegate = new AuthorizationDelegate() };

    sealed class AuthorizationDelegate : ShinyBeaconLocationDelegate;


    /// <inheritdoc />
    public IList<BeaconRegion> GetMonitoredRegions() => repository.GetAll<BeaconRegion>().ToList();


    /// <inheritdoc />
    public async Task StartMonitoring(BeaconRegion region)
    {
        AssertNotifies(region);
        this.EnsureSession();

        var mon = await this.GetMonitor().ConfigureAwait(false);

        // stored first - the condition's first event can arrive as soon as it is added, and it needs the region
        var previous = repository.Get<BeaconRegion>(region.Identifier);
        repository.Set(region);
        try
        {
            await platform.InvokeOnMainThreadAsync(() => this.AddToMonitor(mon, region)).ConfigureAwait(false);
        }
        catch
        {
            if (previous == null)
                repository.Remove<BeaconRegion>(region.Identifier);
            else
                repository.Set(previous);

            throw;
        }
    }


    /// <inheritdoc />
    public async Task StopMonitoring(string identifier)
    {
        repository.Remove<BeaconRegion>(identifier);
        repository.Remove<BeaconMonitorState>(identifier);

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
        repository.Clear<BeaconMonitorState>();
    }


    /// <inheritdoc />
    public Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken cancelToken = default)
        => platform.InvokeTaskOnMainThread(async () =>
        {
            // no ConfigureAwait(false): the record has to be read back on the main queue CLMonitor runs on, and a
            // monitor that is not open yet completes this await on the thread pool
            var mon = await this.GetMonitor();
            var record = mon.GetMonitoringRecord(region.Identifier);

            return record?.LastEvent.State switch
            {
                CLMonitoringState.Satisfied => BeaconRegionState.Entered,
                CLMonitoringState.Unsatisfied => BeaconRegionState.Exited,
                _ => BeaconRegionState.Unknown
            };
        }, cancelToken);


    static void AssertNotifies(BeaconRegion region)
    {
        if (region is { NotifyOnEntry: false, NotifyOnExit: false })
            throw new InvalidOperationException("The region is not set to notify on entry or exit");
    }


    void AddToMonitor(CLMonitor mon, BeaconRegion region)
    {
        AssertNotifies(region);

        // conditions are persisted by the OS, so re-registering an identifier has to replace
        if (mon.GetMonitoringRecord(region.Identifier) != null)
        {
            logger.LogInformation("Replacing existing beacon condition {Identifier}", region.Identifier);
            mon.RemoveCondition(region.Identifier);
        }

        // The first event of a condition the app has just added is its current state, not a replay of
        // an older one - it must not be swallowed like the cold-start replays are.
        this.initialFires.TryRemove(region.Identifier, out _);
        this.freshConditions[region.Identifier] = 0;
        repository.Remove<BeaconMonitorState>(region.Identifier);
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
                    if (diagnostic is { AuthorizationRequestInProgress: false } && (diagnostic.AuthorizationDenied || diagnostic.AlwaysAuthorizationDenied))
                        logger.LogWarning("Always location authorization is not held - beacon region events will not be delivered in the background");
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

                if (evt.ConditionUnsupported || evt.State == CLMonitoringState.Unmonitored)
                {
                    // macOS in particular may refuse beacon conditions outright, and a device without region
                    // monitoring reports Unmonitored. Neither may use up the first-event handling below
                    logger.LogWarning("CoreLocation is not monitoring beacon region {Identifier} ({State})", evt.Identifier, evt.State);
                    return;
                }

                var fresh = this.freshConditions.TryRemove(evt.Identifier, out _);
                var initial = !fresh && this.initialFires.TryRemove(evt.Identifier, out _);

                var region = repository.Get<BeaconRegion>(evt.Identifier);
                if (region == null)
                    return;

                var inside = evt.State == CLMonitoringState.Satisfied;
                var last = repository.Get<BeaconMonitorState>(evt.Identifier);
                repository.Set(new BeaconMonitorState(evt.Identifier, inside));

                // Unsatisfied is what the app assumes anyway. Satisfied means the device was already inside when
                // monitoring started - CoreLocation will not report that again, so this is the only chance the
                // delegate gets to hear it.
                if (fresh && !inside)
                    return;

                // The first event after a cold start is only swallowed when it repeats the state last handled.
                // When the OS relaunched the app for a crossing, that event is the crossing - it differs from what
                // was persisted and is reported like any other.
                if (initial && (last == null || last.Inside == inside))
                {
                    logger.LogDebug("Suppressed initial state fire for beacon region {Identifier}", evt.Identifier);
                    return;
                }

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
            this.freshConditions.Clear();

            this.session?.Invalidate();
            this.session = null;
        }
        finally
        {
            this.monitorLock.Release();
        }
    }
}


/// <summary>
/// The last CLMonitor state handled for a beacon region - persisted so the first event after a cold start can be
/// told apart: a repeat of this is a replay, anything else is a transition that happened while the app was not running.
/// </summary>
record BeaconMonitorState(string Identifier, bool Inside) : IRepositoryEntity;
#endif
