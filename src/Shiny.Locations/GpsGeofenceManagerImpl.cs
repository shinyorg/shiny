using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Locations;


public class GpsGeofenceManagerImpl : IGeofenceManager, IShinyStartupTask
{
    readonly ILogger logger;
    readonly IRepository repository;
    readonly IGpsManager gpsManager;


    static readonly GpsRequest defaultRequest = new GpsRequest
    {
        BackgroundMode = GpsBackgroundMode.Realtime,
        Accuracy = GpsAccuracy.Normal
    };

    public GpsGeofenceManagerImpl(
        ILogger<GpsGeofenceManagerImpl> logger,
        IRepository repository, 
        IGpsManager gpsManager
    )
    {
        this.logger = logger;
        this.repository = repository;
        this.gpsManager = gpsManager;
    }


    public async void Start()
    {
        try 
        { 
            var restore = this.repository.GetList<GeofenceRegion>();
            if (restore.Any())
                await this.TryStartGps();
        }
        catch (Exception ex) 
        {
            this.logger.LogWarning(ex, "Failed to start gps geofencing");
        }
    }


    public AccessState CurrentStatus
        => this.gpsManager.GetCurrentStatus(defaultRequest);

    public Task<AccessState> RequestAccess()
        => this.gpsManager.RequestAccess(defaultRequest);


    public IList<GeofenceRegion> GetMonitorRegions()
        => this.repository.GetList<GeofenceRegion>();


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        var reading = await this.gpsManager!
            .GetLastReading()
            .Timeout(TimeSpan.FromSeconds(10))
            .ToTask();

        if (reading == null)
            return GeofenceState.Unknown;

        var state = region.IsPositionInside(reading.Position)
            ? GeofenceState.Entered
            : GeofenceState.Exited;

        return state;
    }


    public async Task StartMonitoring(GeofenceRegion region)
    {
        await this.TryStartGps().ConfigureAwait(false);
        this.repository.Set(region);
    }


    public async Task StopAllMonitoring()
    {
        this.repository.Clear<GeofenceRegion>();
        await this.gpsManager.StopListener().ConfigureAwait(false);
    }


    public async Task StopMonitoring(string identifier)
    {
        this.repository.Remove<GeofenceRegion>(identifier);
        var geofences = this.repository.GetList<GeofenceRegion>();

        if (geofences.Count == 0)
            await this.gpsManager!.StopListener();
    }


    protected async Task TryStartGps()
    {
        if (this.gpsManager.CurrentListener == null)
            await this.gpsManager.StartListener(defaultRequest).ConfigureAwait(false);
    }
}