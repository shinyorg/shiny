using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.Logging;
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
            var regions = repository.GetList<GeofenceRegion>();
            if (regions.Any())
            {
                var mon = await this.GetMonitor();

                foreach (var region in regions)
                    this.AddToMonitor(mon, region);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting GeofenceManager");
        }
    }
    
    
    public AccessState CurrentStatus { get; private set; } = AccessState.Unknown;
    

    CLServiceSession? session;
    public async Task<AccessState> RequestAccess()
    {
        if (this.CurrentStatus != AccessState.Unknown)
            return this.CurrentStatus;
        
        var tcs = new TaskCompletionSource<AccessState>();
        this.session ??= CLServiceSession.CreateSession(
            CLServiceSessionAuthorizationRequirement.Always,
            String.Empty,
            DispatchQueue.MainQueue, 
            diag =>
            {
                if (diag.AuthorizationRequestInProgress)
                    return;

                if (diag.AuthorizationDenied || diag.AlwaysAuthorizationDenied)
                    this.CurrentStatus = AccessState.Denied;
                else
                    this.CurrentStatus = AccessState.Available;
                
                tcs.TrySetResult(this.CurrentStatus);
            }
        );

        return await tcs.Task.ConfigureAwait(false);
    }

    
    public IList<GeofenceRegion> GetMonitorRegions() => repository.GetList<GeofenceRegion>();

    
    public async Task StartMonitoring(GeofenceRegion region)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();
        
        var mon = await this.GetMonitor();
        this.AddToMonitor(mon, region);
        repository.Insert(region);
    }

    
    public async Task StopMonitoring(string identifier)
    {
        repository.Remove<GeofenceRegion>(identifier);
        var mon = await this.GetMonitor();
        mon.RemoveCondition(identifier);
        
        if (repository.GetList<GeofenceRegion>().Count == 0)
            this.DestroyMonitor();
    }
    

    public Task StopAllMonitoring()
    {
        this.DestroyMonitor();
        repository.Clear<GeofenceRegion>();
        return Task.CompletedTask;
    }


    public Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default) =>
        platform.InvokeTaskOnMainThread(async () =>
        {
            (await this.RequestAccess()).Assert();
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
        mon.AddCondition(condition, region.Identifier);
    }

    
    CLMonitor? monitor;
    async ValueTask<CLMonitor> GetMonitor() => this.monitor ??= await CLMonitor.RequestMonitorAsync(
        CLMonitorConfiguration.Create(
            "shinygeofences",
            DispatchQueue.MainQueue,
            async (mon, evt) =>
            {
                // TODO: prevent initial state firing?
                var lastEvent = mon.GetMonitoringRecord(evt.Identifier)!.LastEvent;
                if (lastEvent.State == evt.State)
                {
                    logger.LogDebug("Geofence State Matches");
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
        )
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

    
    void DestroyMonitor()
    {
        this.monitor?.Dispose();
        this.monitor = null;
        
        this.session?.Invalidate();
        this.session = null;
    }
}