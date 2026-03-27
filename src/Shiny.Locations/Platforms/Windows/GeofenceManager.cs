using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using WinGeofenceState = Windows.Devices.Geolocation.Geofencing.GeofenceState;

namespace Shiny.Locations;


public class GeofenceManager : IGeofenceManager, IShinyStartupTask
{
    readonly IServiceProvider services;
    readonly IRepository repository;
    readonly ILogger logger;


    public GeofenceManager(
        IServiceProvider services,
        IRepository repository,
        ILogger<IGeofenceManager> logger
    )
    {
        this.services = services;
        this.repository = repository;
        this.logger = logger;
    }


    public void Start()
    {
        try
        {
            var regions = this.repository.GetList<GeofenceRegion>();
            if (regions.Count == 0)
                return;

            GeofenceMonitor.Current.GeofenceStateChanged += this.OnGeofenceStateChanged;

            foreach (var region in regions)
            {
                try
                {
                    this.AddNativeGeofence(region);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to restore geofence: {Identifier}", region.Identifier);
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to start geofence manager");
        }
    }


    public AccessState CurrentStatus
    {
        get
        {
            var status = Geolocator.RequestAccessAsync().AsTask().GetAwaiter().GetResult();
            return FromNative(status);
        }
    }


    public async Task<AccessState> RequestAccess()
    {
        var status = await Geolocator.RequestAccessAsync();
        return FromNative(status);
    }


    public IList<GeofenceRegion> GetMonitorRegions()
        => this.repository.GetList<GeofenceRegion>();


    public async Task StartMonitoring(GeofenceRegion region)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        this.AddNativeGeofence(region);
        this.repository.Set(region);

        var regions = this.repository.GetList<GeofenceRegion>();
        if (regions.Count == 1)
            GeofenceMonitor.Current.GeofenceStateChanged += this.OnGeofenceStateChanged;
    }


    public Task StopMonitoring(string identifier)
    {
        this.repository.Remove<GeofenceRegion>(identifier);
        this.RemoveNativeGeofence(identifier);

        var regions = this.repository.GetList<GeofenceRegion>();
        if (regions.Count == 0)
            GeofenceMonitor.Current.GeofenceStateChanged -= this.OnGeofenceStateChanged;

        return Task.CompletedTask;
    }


    public Task StopAllMonitoring()
    {
        GeofenceMonitor.Current.GeofenceStateChanged -= this.OnGeofenceStateChanged;

        var regions = this.repository.GetList<GeofenceRegion>();
        foreach (var region in regions)
            this.RemoveNativeGeofence(region.Identifier);

        this.repository.Clear<GeofenceRegion>();
        return Task.CompletedTask;
    }


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();

        var loc = new Geolocator();
        var position = await loc.GetGeopositionAsync().AsTask(cancelToken).ConfigureAwait(false);
        if (position?.Coordinate == null)
            return GeofenceState.Unknown;

        var currentPosition = new Position(
            position.Coordinate.Point.Position.Latitude,
            position.Coordinate.Point.Position.Longitude
        );

        return region.IsPositionInside(currentPosition)
            ? GeofenceState.Entered
            : GeofenceState.Exited;
    }


    void AddNativeGeofence(GeofenceRegion region)
    {
        this.RemoveNativeGeofence(region.Identifier);

        var position = new BasicGeoposition
        {
            Latitude = region.Center.Latitude,
            Longitude = region.Center.Longitude
        };

        var geocircle = new Geocircle(position, region.Radius.TotalMeters);

        var states = (MonitoredGeofenceStates)0;
        if (region.NotifyOnEntry)
            states |= MonitoredGeofenceStates.Entered;
        if (region.NotifyOnExit)
            states |= MonitoredGeofenceStates.Exited;

        var geofence = new Geofence(
            region.Identifier,
            geocircle,
            states,
            region.SingleUse
        );

        GeofenceMonitor.Current.Geofences.Add(geofence);
    }


    void RemoveNativeGeofence(string identifier)
    {
        var existing = GeofenceMonitor.Current.Geofences
            .FirstOrDefault(x => x.Id == identifier);

        if (existing != null)
            GeofenceMonitor.Current.Geofences.Remove(existing);
    }


    async void OnGeofenceStateChanged(GeofenceMonitor sender, object args)
    {
        try
        {
            var reports = sender.ReadReports();
            foreach (var report in reports)
            {
                var region = this.repository.Get<GeofenceRegion>(report.Geofence.Id);
                if (region == null)
                    continue;

                var state = report.NewState switch
                {
                    WinGeofenceState.Entered => GeofenceState.Entered,
                    WinGeofenceState.Exited => GeofenceState.Exited,
                    _ => GeofenceState.Unknown
                };

                if (state == GeofenceState.Unknown)
                    continue;

                await this.services
                    .RunDelegates<IGeofenceDelegate>(
                        x => x.OnStatusChanged(state, region),
                        this.logger
                    )
                    .ConfigureAwait(false);

                if (region.SingleUse)
                    await this.StopMonitoring(region.Identifier).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing geofence state change");
        }
    }


    static AccessState FromNative(GeolocationAccessStatus status) => status switch
    {
        GeolocationAccessStatus.Allowed => AccessState.Available,
        GeolocationAccessStatus.Denied => AccessState.Denied,
        _ => AccessState.Unknown
    };
}
