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
using ObjCRuntime;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Hosting;

namespace Shiny.Locations;


[SupportedOSPlatform("ios18.0")]
[SupportedOSPlatform("maccatalyst18.0")]
public class GeofenceManager(
    ILogger<IGeofenceManager> logger,
    IServiceProvider services,
    IPlatform platform,
    IRepository repository
) : IGeofenceManager, IShinyStartupTask, IIosLifecycle.IApplicationLifecycle
{
    public async void Start()
    {
        try
        {
            var regions = repository.GetAll<GeofenceRegion>();
            if (regions.Count > 0)
            {
                // Session is required for CoreLocation to deliver background monitor events on iOS 18
                this.EnsureSession();

                var mon = await this.GetMonitor().ConfigureAwait(false);
                if (mon != null)
                    await platform.InvokeOnMainThreadAsync(() => this.Reconcile(mon, regions)).ConfigureAwait(false);

                this.Dwell.Restore();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting GeofenceManager");
        }
    }


    // Must be invoked on the dispatch queue the CLMonitor was configured with (MainQueue).
    void Reconcile(CLMonitor mon, IReadOnlyList<GeofenceRegion> regions)
    {
        var iosIds = new HashSet<string>(mon.MonitoredIdentifiers ?? []);
        var repoIds = new HashSet<string>(regions.Select(r => r.Identifier));

        // Drop iOS conditions that no longer have a repo entry (renamed IDs, stale installs)
        foreach (var orphan in iosIds.Except(repoIds))
        {
            logger.LogInformation("Removing orphan geofence condition {Identifier}", orphan);
            mon.RemoveCondition(orphan);
        }

        // Add repo entries that aren't already on iOS - skip those iOS already tracks
        foreach (var region in regions)
        {
            if (iosIds.Contains(region.Identifier))
                logger.LogDebug("Geofence {Identifier} already monitored by iOS - skipping re-add", region.Identifier);
            else
                this.AddToMonitor(mon, region);
        }
    }
    
    
    public AccessState CurrentStatus { get; private set; } = AccessState.Unknown;


    public void OnForeground()
    {
        try
        {
            // dwell timers don't advance while iOS has the app suspended - catch up on any that came due
            this.Dwell.Reevaluate();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to re-evaluate geofence dwells");
        }
    }


    public void OnBackground() { }


    GeofenceDwellTracker Dwell
    {
        get
        {
            field ??= new GeofenceDwellTracker(
                repository,
                logger,
                this.RequestState,
                r => this.FireDelegate(r, GeofenceState.Dwelling)
            );
            return field;
        }
    }
    

    CLServiceSession? session;
    TaskCompletionSource<AccessState>? authTcs;

    void EnsureSession()
    {
        if (this.session != null)
            return;

        this.session = CLServiceSession.CreateSession(
            CLServiceSessionAuthorizationRequirement.Always,
            String.Empty,
            DispatchQueue.MainQueue,
            diag =>
            {
                // Callback runs on MainQueue as an Action; an unhandled throw here would terminate the app.
                try
                {
                    if (diag is null || diag.AuthorizationRequestInProgress)
                        return;

                    this.CurrentStatus = (diag.AuthorizationDenied || diag.AlwaysAuthorizationDenied)
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


    public async Task<AccessState> RequestAccess()
    {
        if (this.CurrentStatus != AccessState.Unknown)
            return this.CurrentStatus;

        this.authTcs ??= new TaskCompletionSource<AccessState>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cts.Token.Register(() => this.authTcs.TrySetException(new TimeoutException("Geofence authorization request timed out")));

        this.EnsureSession();
        return await this.authTcs.Task.ConfigureAwait(false);
    }

    
    public IList<GeofenceRegion> GetMonitorRegions() => repository.GetAll<GeofenceRegion>().ToList();

    
    public async Task StartMonitoring(GeofenceRegion region)
    {
        // CLMonitor needs an active CLServiceSession to deliver events when the app is backgrounded.
        this.EnsureSession();
        var mon = await this.GetMonitor().ConfigureAwait(false);

        // stored first - the condition's initial event can arrive as soon as it is added, and it needs the region
        repository.Insert(region);
        try
        {
            await platform.InvokeOnMainThreadAsync(() => this.AddToMonitor(mon, region)).ConfigureAwait(false);
        }
        catch
        {
            repository.Remove<GeofenceRegion>(region.Identifier);
            throw;
        }
    }


    public async Task StopMonitoring(string identifier)
    {
        this.Dwell.Remove(identifier);
        repository.Remove<GeofenceRegion>(identifier);
        var mon = await this.GetMonitor().ConfigureAwait(false);
        await platform.InvokeOnMainThreadAsync(() => mon.RemoveCondition(identifier)).ConfigureAwait(false);

        if (repository.GetAll<GeofenceRegion>().Count == 0)
            await this.DestroyMonitor().ConfigureAwait(false);
    }
    

    public async Task StopAllMonitoring()
    {
        this.Dwell.Clear();
        await this.DestroyMonitor().ConfigureAwait(false);
        repository.Clear<GeofenceRegion>();
    }


    public Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default) =>
        platform.InvokeTaskOnMainThread(async () =>
        {
            var mon = await this.GetMonitor();
            var rec = mon.GetMonitoringRecord(region.Identifier);
            if (rec == null)
                return GeofenceState.Unknown; // throw?

            return rec.LastEvent.State switch
            {
                CLMonitoringState.Satisfied => GeofenceState.Entered,
                CLMonitoringState.Unsatisfied => GeofenceState.Exited,
                _ => GeofenceState.Unknown
            };
        }, cancelToken);


    void AddToMonitor(CLMonitor mon, GeofenceRegion region)
    {
        if (region is { NotifyOnEntry: false, NotifyOnExit: false, DwellTime: null })
            throw new InvalidOperationException("Region is not set to notify on entry, exit or dwell");

        // CLMonitor persists conditions at the OS level keyed by monitor name, so an existing
        // record here means a prior install/session left an orphan (or the caller is re-adding
        // with updated parameters). Remove and re-add so the new condition takes effect.
        if (mon.GetMonitoringRecord(region.Identifier) != null)
        {
            logger.LogInformation("Replacing existing geofence condition {Identifier}", region.Identifier);
            mon.RemoveCondition(region.Identifier);
        }

        var condition = new CLCircularGeographicCondition(
            new CLLocationCoordinate2D(region.Center.Latitude, region.Center.Longitude),
            region.Radius.TotalMeters
        );

        // we monitor ALL state changes for RequestState and dwell, but we only fire delegates according to flags
        this.initialFires.TryAdd(region.Identifier, 0);
        mon.AddCondition(condition, region.Identifier);
    }

    
    volatile CLMonitor? monitor;
    SemaphoreSlim monitorLock = new(1, 1);
    ConcurrentDictionary<string, byte> initialFires = new();

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
        // CLMonitor only allows one open instance per name in the process. After a background-launch
        // by iOS for a geofence event, the prior process's instance can still be considered "open"
        // and RequestMonitor throws NSInternalInconsistencyException. A short retry usually clears it.
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var mon = await CLMonitor.RequestMonitorAsync(this.BuildConfiguration()).ConfigureAwait(false);

                // CLMonitor re-attaches to OS-persisted conditions and fires their current state
                // immediately on cold start. Prime initialFires so the event handler suppresses those
                // first fires - only state CHANGES after this point should reach delegates.
                // MonitoredIdentifiers must be touched on the queue CLMonitor was configured with (MainQueue)
                // or CoreLocation will native-crash the process.
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
        "shinygeofences",
        DispatchQueue.MainQueue,
        async (mon, evt) =>
        {
            // Handler runs on MainQueue as a void-returning delegate; unhandled throws would terminate the app.
            try
            {
                // CLMonitor fires an initial event when a condition is first added (and after cold-start
                // re-attach) - those never reach the delegate. Dwell still uses them: an initial "inside" starts
                // the clock for a region the device was already in, and a replayed exit still ends a stay.
                var initial = this.initialFires.TryRemove(evt.Identifier, out _);
                if (initial)
                    logger.LogDebug("Geofence initial state fire suppressed for {Identifier}", evt.Identifier);

                var region = repository.Get<GeofenceRegion>(evt.Identifier);
                if (region != null)
                {
                    // when CoreLocation saw the crossing, not when the app got to handle it
                    DateTimeOffset? at = evt.Date is { } date ? new DateTimeOffset((DateTime)date) : null;

                    switch (evt.State)
                    {
                        case CLMonitoringState.Satisfied:
                            this.Dwell.Entered(region, at);
                            if (!initial && region.NotifyOnEntry)
                                await this.FireDelegate(region, GeofenceState.Entered).ConfigureAwait(false);

                            break;

                        case CLMonitoringState.Unsatisfied:
                            // reports Dwelling first if the stay lasted the dwell time
                            await this.Dwell.Exited(region, at).ConfigureAwait(false);

                            // a single-use region is gone once its dwell fired
                            if (!initial && region.NotifyOnExit && repository.Exists<GeofenceRegion>(region.Identifier))
                                await this.FireDelegate(region, GeofenceState.Exited).ConfigureAwait(false);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling geofence event for {Identifier}", evt?.Identifier);
            }
        }
    );


    async Task FireDelegate(GeofenceRegion region, GeofenceState status)
    {
        await services
            .RunDelegates<IGeofenceDelegate>(
                x => x.OnStatusChanged(status, region),
                logger
            )
            .ConfigureAwait(false);

        if (region.IsSingleUseComplete(status))
            await this.StopMonitoring(region.Identifier).ConfigureAwait(false);
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