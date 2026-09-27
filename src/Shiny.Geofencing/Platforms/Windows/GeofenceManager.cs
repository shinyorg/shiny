using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using WinGeofenceState = Windows.Devices.Geolocation.Geofencing.GeofenceState;

namespace Shiny.Locations;


public class GeofenceManager : IGeofenceManager, IShinyStartupTask
{
    readonly IServiceProvider services;
    readonly IRepository repository;
    readonly ILogger logger;


    readonly GeofenceDwellTracker dwell;


    public GeofenceManager(
        IServiceProvider services,
        IRepository repository,
        ILogger<IGeofenceManager> logger
    )
    {
        this.services = services;
        this.repository = repository;
        this.logger = logger;
        this.dwell = new GeofenceDwellTracker(
            repository,
            logger,
            this.RequestState,
            r => this.FireDelegate(r, GeofenceState.Dwelling)
        );
    }


    public void Start()
    {
        try
        {
            var regions = this.repository.GetAll<GeofenceRegion>();
            if (regions.Count == 0)
                return;

            this.EnsureEventSubscription();

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
            this.dwell.Restore();
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
        => this.repository.GetAll<GeofenceRegion>().ToList();


    public async Task StartMonitoring(GeofenceRegion region)
    {
        this.AddNativeGeofence(region);
        this.repository.Set(region);
        this.EnsureEventSubscription();
    }


    public Task StopMonitoring(string identifier)
    {
        this.dwell.Remove(identifier);
        this.repository.Remove<GeofenceRegion>(identifier);
        this.RemoveNativeGeofence(identifier);

        var regions = this.repository.GetAll<GeofenceRegion>();
        if (regions.Count == 0)
        {
            GeofenceMonitor.Current.GeofenceStateChanged -= this.OnGeofenceStateChanged;
            this.eventSubscribed = false;
        }

        return Task.CompletedTask;
    }


    public Task StopAllMonitoring()
    {
        GeofenceMonitor.Current.GeofenceStateChanged -= this.OnGeofenceStateChanged;
        this.eventSubscribed = false;
        this.dwell.Clear();

        var regions = this.repository.GetAll<GeofenceRegion>();
        foreach (var region in regions)
            this.RemoveNativeGeofence(region.Identifier);

        this.repository.Clear<GeofenceRegion>();
        return Task.CompletedTask;
    }


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
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


    bool eventSubscribed;
    void EnsureEventSubscription()
    {
        if (!this.eventSubscribed)
        {
            GeofenceMonitor.Current.GeofenceStateChanged += this.OnGeofenceStateChanged;
            this.eventSubscribed = true;
        }
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

        // dwell needs the entry to start its timer and the exit to cancel it
        var hasDwell = region.DwellTime != null;
        var states = (MonitoredGeofenceStates)0;
        if (region.NotifyOnEntry || hasDwell)
            states |= MonitoredGeofenceStates.Entered;
        if (region.NotifyOnExit || hasDwell)
            states |= MonitoredGeofenceStates.Exited;

        // Windows' own dwellTime only delays the Entered report - Shiny's dwell is a separate transition,
        // and a native single-use fence would be removed on entry before the dwell could fire
        var geofence = new Geofence(
            region.Identifier,
            geocircle,
            states,
            region.SingleUse && !hasDwell
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
                if (region != null)
                {
                    // when the position that crossed the boundary was taken, not when the report was read
                    DateTimeOffset? at = report.Geoposition?.Coordinate?.Timestamp;

                    switch (report.NewState)
                    {
                        case WinGeofenceState.Entered:
                            this.dwell.Entered(region, at);
                            if (region.NotifyOnEntry)
                                await this.FireDelegate(region, GeofenceState.Entered).ConfigureAwait(false);
                            break;

                        case WinGeofenceState.Exited:
                            // reports Dwelling first if the stay lasted the dwell time and the timer didn't already
                            await this.dwell.Exited(region, at).ConfigureAwait(false);

                            // a single-use region is gone once its dwell fired
                            if (region.NotifyOnExit && this.repository.Exists<GeofenceRegion>(region.Identifier))
                                await this.FireDelegate(region, GeofenceState.Exited).ConfigureAwait(false);
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing geofence state change");
        }
    }


    async Task FireDelegate(GeofenceRegion region, GeofenceState state)
    {
        await this.services
            .RunDelegates<IGeofenceDelegate>(
                x => x.OnStatusChanged(state, region),
                this.logger
            )
            .ConfigureAwait(false);

        if (region.IsSingleUseComplete(state))
            await this.StopMonitoring(region.Identifier).ConfigureAwait(false);
    }


    static AccessState FromNative(GeolocationAccessStatus status) => status switch
    {
        GeolocationAccessStatus.Allowed => AccessState.Available,
        GeolocationAccessStatus.Denied => AccessState.Denied,
        _ => AccessState.Unknown
    };
}
