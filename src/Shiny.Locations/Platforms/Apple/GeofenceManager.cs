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
) : IGeofenceManager
{
    CLBackgroundActivitySession? session;
    CLMonitor? monitor;
    
    async Task<CLMonitor?> Init(bool createIfNeeded)
    {
        if (this.monitor == null && createIfNeeded)
        {
            this.session = CLBackgroundActivitySession.Create(); // catalyst 14 or ios17
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
    
    
    public AccessState CurrentStatus { get; }
    public Task<AccessState> RequestAccess() => throw new System.NotImplementedException();

    
    public IList<GeofenceRegion> GetMonitorRegions() => repository.GetList<GeofenceRegion>();

    public async Task StartMonitoring(GeofenceRegion region)
    {
        var mon = await this.Init(true);
        var condition = new CLCircularGeographicCondition(
            new CLLocationCoordinate2D(region.Center.Latitude, region.Center.Longitude),
            region.Radius.TotalMeters
        );
        mon.AddCondition(condition, region.Identifier);
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

    
    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        // CLLocationUpdater.CreateLiveUpdates(DispatchQueue.CurrentQueue, update =>
        // {
        //     update.
        // });
        return GeofenceState.Entered;
    }


    void DestroyMonitor()
    {
        this.monitor?.Dispose();
        this.monitor = null;
        
        this.session?.Dispose();
        this.session = null;
    }
}