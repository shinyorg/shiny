using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using CoreLocation;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Locations;

public class GeofenceManager(
    ILogger<IGeofenceManager> logger,
    IServiceProvider services,
    IRepository repository
) : IGeofenceManager, IShinyStartupTask
{
    CLBackgroundActivitySession? session;
    CLMonitor? monitor;
    
    public async void Start()
    {
        var items = repository.GetList<GeofenceRegion>();
        if (items.Count > 0)
        {
            try
            {
                var status = await this.RequestAccess();
                if (status != AccessState.Available)
                {
                    logger.LogWarning("User has changed permissions which block geofences from running");
                }
                else
                {
                    var mon = await this.Init(true);
                    foreach (var item in items)
                        this.AddToMonitor(mon, item);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-start geofencing");
            }
        }
    }
    
    
    async Task<CLMonitor?> Init(bool createIfNeeded)
    {
        if (this.monitor == null && createIfNeeded)
        {
            this.session = CLBackgroundActivitySession.Create();
            this.monitor = await CLMonitor.RequestMonitorAsync(CLMonitorConfiguration.Create(
                "shiny_geofences",
                new DispatchQueue("geofences"),
                async (mon, evt) =>
                {
                    var region = repository.Get<GeofenceRegion>(evt.Identifier);
                    if (region != null)
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
                    }
                }
            ));
        }
        return this.monitor;
    }


    // may need a subject or an await somewhere
    public AccessState CurrentStatus { get; private set; } = AccessState.Unknown;

    
    // CLServiceSession? psession = null!;
    public Task<AccessState> RequestAccess()
    {
        return LocationExtensions.RequestAccess(true);
        // var tcs = new TaskCompletionSource<AccessState>();
        // CLServiceSession ps = null!;
        //
        // try
        // {
        //     ps = CLServiceSession.CreateSession(
        //         CLServiceSessionAuthorizationRequirement.Always,
        //         "Geofencing",
        //         new DispatchQueue("shiny_geofence"),
        //         diag =>
        //         {
        //             if (diag.AuthorizationRequestInProgress)
        //                 return; 
        //             
        //             // diag.FullAccuracyDenied
        //             if (diag.AuthorizationDenied || diag.AlwaysAuthorizationDenied)
        //                 this.CurrentStatus = AccessState.Denied;
        //             else
        //                 this.CurrentStatus = AccessState.Available;
        //
        //             tcs.TrySetResult(this.CurrentStatus);
        //         }
        //     );
        // }
        // finally
        // {
        //     ps.Dispose();
        // }
        //
        // return tcs.Task;
    }

    
    public IList<GeofenceRegion> GetMonitorRegions() => repository.GetList<GeofenceRegion>();

    
    public async Task StartMonitoring(GeofenceRegion region)
    {
        var mon = await this.Init(true);
        this.AddToMonitor(mon, region);
        repository.Insert(region);
    }

    
    public async Task StopMonitoring(string identifier)
    {
        var mon = await this.Init(false);
        if (mon != null)
        {
            repository.Remove<GeofenceRegion>(identifier);
            mon.RemoveCondition(identifier);
            if (repository.GetList<GeofenceRegion>().Count == 0)
                this.DestroyMonitor();
        }
    }
    

    public Task StopAllMonitoring()
    {
        this.DestroyMonitor();
        repository.Clear<GeofenceRegion>();
        return Task.CompletedTask;
    }

    
    public Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        var tcs = new TaskCompletionSource<GeofenceState>();
        var updater = CLLocationUpdater.CreateLiveUpdates(DispatchQueue.CurrentQueue, update =>
        {
            if (update.AuthorizationDenied)
                throw new PermissionException("GPS", AccessState.Denied);
            
            if (update.LocationUnavailable || update.Location == null)
                throw new PermissionException("GPS", AccessState.Disabled);

            var reading = update.Location.FromNative();
            var available = region.IsPositionInside(reading.Position);
            var status = available ? GeofenceState.Entered : GeofenceState.Exited;
            tcs.TrySetResult(status);
        });
        using var _ = cancelToken.Register(() => tcs.TrySetCanceled());
        try
        {
            return tcs.Task;
        }
        finally
        {
            updater?.Dispose();
        }
    }


    void AddToMonitor(CLMonitor mon, GeofenceRegion region)
    {
        var condition = new CLCircularGeographicCondition(
            new CLLocationCoordinate2D(region.Center.Latitude, region.Center.Longitude),
            region.Radius.TotalMeters
        );
        mon.AddCondition(condition, region.Identifier);
    }


    void DestroyMonitor()
    {
        this.monitor?.Dispose();
        this.monitor = null;
        
        this.session?.Dispose();
        this.session = null;
    }
}