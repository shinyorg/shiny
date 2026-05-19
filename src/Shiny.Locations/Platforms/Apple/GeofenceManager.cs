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
using Shiny.Support.Repositories;

namespace Shiny.Locations;


[SupportedOSPlatform("ios18.0")]
[SupportedOSPlatform("maccatalyst18.0")]
public class GeofenceManager(
    ILogger<IGeofenceManager> logger,
    IServiceProvider services,
    IPlatform platform,
    IRepository repository
) : IGeofenceManager, IShinyStartupTask
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
                    this.Reconcile(mon, regions);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting GeofenceManager");
        }
    }


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
                if (diag.AuthorizationRequestInProgress)
                    return;

                this.CurrentStatus = (diag.AuthorizationDenied || diag.AlwaysAuthorizationDenied)
                    ? AccessState.Denied
                    : AccessState.Available;

                this.authTcs?.TrySetResult(this.CurrentStatus);
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
        var mon = await this.GetMonitor();
        this.AddToMonitor(mon, region);
        repository.Insert(region);
    }

    
    public async Task StopMonitoring(string identifier)
    {
        repository.Remove<GeofenceRegion>(identifier);
        var mon = await this.GetMonitor();
        mon.RemoveCondition(identifier);
        
        if (repository.GetAll<GeofenceRegion>().Count == 0)
            this.DestroyMonitor();
    }
    

    public async Task StopAllMonitoring()
    {
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
        if (region is { NotifyOnEntry: false, NotifyOnExit: false })
            throw new InvalidOperationException("Region is not set to notify on entry or exit");

        var rec = mon.GetMonitoringRecord(region.Identifier);
        if (rec != null)
            throw new InvalidOperationException($"A region with the identifier '{region.Identifier}' already exists");

        var condition = new CLCircularGeographicCondition(
            new CLLocationCoordinate2D(region.Center.Latitude, region.Center.Longitude),
            region.Radius.TotalMeters
        );

        // we monitor ALL state changes for RequestState, but we only fire delegates according to flags
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
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CLMonitor.RequestMonitorAsync(this.BuildConfiguration()).ConfigureAwait(false);
            }
            catch (ObjCException ex) when (attempt < maxAttempts && ex.Reason?.Contains("already in use") == true)
            {
                logger.LogWarning(ex, "CLMonitor \"shinygeofences\" reported already-in-use (attempt {Attempt}/{Max}) - retrying", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt)).ConfigureAwait(false);
            }
        }
    }


    CLMonitorConfiguration BuildConfiguration() => CLMonitorConfiguration.Create(
        "shinygeofences",
        DispatchQueue.MainQueue,
        async (mon, evt) =>
        {
            // CLMonitor fires an initial event when a condition is first added - suppress it
            if (this.initialFires.TryRemove(evt.Identifier, out _))
            {
                logger.LogDebug("Geofence initial state fire suppressed for {Identifier}", evt.Identifier);
                return;
            }

            var region = repository.Get<GeofenceRegion>(evt.Identifier);
            if (region != null)
            {
                switch (evt.State)
                {
                    case CLMonitoringState.Satisfied:
                        if (region.NotifyOnEntry)
                            await this.FireDelegate(region, evt).ConfigureAwait(false);

                        break;

                    case CLMonitoringState.Unsatisfied:
                        if (region.NotifyOnExit)
                            await this.FireDelegate(region, evt).ConfigureAwait(false);
                        break;
                }
            }
        }
    );


    async Task FireDelegate(GeofenceRegion region, CLMonitoringEvent evt)
    {
        var status = evt.State == CLMonitoringState.Satisfied
            ? GeofenceState.Entered
            : GeofenceState.Exited;
        
        await services
            .RunDelegates<IGeofenceDelegate>(
                x => x.OnStatusChanged(status, region),
                logger
            )
            .ConfigureAwait(false);

        if (region.SingleUse)
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