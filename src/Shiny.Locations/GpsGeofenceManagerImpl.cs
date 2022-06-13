using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Locations;


public class GpsGeofenceManagerImpl : IGeofenceManager, IShinyStartupTask
{
    readonly ILogger logger;
    readonly IRepository<GeofenceRegion> repository;
    readonly IGpsManager gpsManager;


    static readonly GpsRequest defaultRequest = new GpsRequest
    {
        BackgroundMode = GpsBackgroundMode.Realtime,
        Accuracy = GpsAccuracy.Normal
    };


    public GpsGeofenceManagerImpl(
        ILogger<GpsGeofenceManagerImpl> logger,
        IRepository<GeofenceRegion> repository, 
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
            var restore = await this.repository.GetList().ConfigureAwait(false);
            if (restore.Any())
                await this.TryStartGps();
        }
        catch (Exception ex) 
        {
            this.logger.LogWarning(ex, "Failed to start gps geofencing");
        }
    }


    public Task<AccessState> RequestAccess()
        => this.gpsManager.RequestAccess(defaultRequest);


    public Task<IList<GeofenceRegion>> GetMonitorRegions()
        => this.repository.GetList();


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
        await this.repository.Set(region).ConfigureAwait(false);
    }


    public async Task StopAllMonitoring()
    {
        await this.repository.Clear().ConfigureAwait(false);
        await this.gpsManager.StopListener().ConfigureAwait(false);
    }


    public async Task StopMonitoring(string identifier)
    {
        await this.repository.Remove(identifier).ConfigureAwait(false);
        var geofences = await this.repository.GetList().ConfigureAwait(false);

        if (geofences.Count == 0)
            await this.gpsManager!.StopListener();
    }


    protected async Task TryStartGps()
    {
        if (this.gpsManager.CurrentListener == null)
            await this.gpsManager.StartListener(defaultRequest).ConfigureAwait(false);
    }
}