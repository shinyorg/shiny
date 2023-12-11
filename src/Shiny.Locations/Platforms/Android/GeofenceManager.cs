using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;
using P = Android.Manifest.Permission;
using Shiny.Support.Repositories;

namespace Shiny.Locations;


public class GeofenceManager : IGeofenceManager, IShinyStartupTask
{
    public const string ReceiverName = "com.shiny.locations." + nameof(GeofenceBroadcastReceiver);
    public const string IntentAction = ReceiverName + ".INTENT_ACTION";

    readonly AndroidPlatform platform;
    readonly IServiceProvider services;
    readonly IRepository repository;
    readonly ILogger logger;

    readonly IGeofencingClient client;
    PendingIntent? geofencePendingIntent;

    public GeofenceManager(
        AndroidPlatform platform,
        IRepository repository,
        IServiceProvider services,
        ILogger<GeofenceManager> logger
    )
    {
        this.platform = platform;
        this.repository = repository;
        this.logger = logger;
        this.services = services;
        this.client = LocationServices.GetGeofencingClient(this.platform.AppContext);
    }


    public async void Start()
    {
        try
        {
            GeofenceBroadcastReceiver.Process = async e =>
            {
                if (e.HasError)
                {
                    var err = GeofenceStatusCodes.GetStatusCodeString(e.ErrorCode);
                    this.logger.LogWarning("Geofence OS error - " + err);
                }
                else if (e.TriggeringGeofences != null)
                {
                    foreach (var triggeringGeofence in e.TriggeringGeofences)
                    {
                        var state = (GeofenceState)e.GeofenceTransition;
                        var region = this.repository.Get<GeofenceRegion>(triggeringGeofence.RequestId);

                        if (region == null)
                        {
                            this.logger.LogWarning("Geofence reported by OS not found in Shiny Repository - RequestID: " + triggeringGeofence.RequestId);
                        }
                        else
                        {
                            await this.services
                                .RunDelegates<IGeofenceDelegate>(
                                    x => x.OnStatusChanged(state, region),
                                    this.logger
                                )
                                .ConfigureAwait(false);
                        }
                    }
                }
            };
            var regions = this.repository.GetList<GeofenceRegion>();
            foreach (var region in regions)
                await this.Create(region);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to restart geofencing");
        }
    }


    public AccessState CurrentStatus
    {
        get
        {
            var coarse = this.platform.GetCurrentPermissionStatus(P.AccessCoarseLocation);
            if (coarse == AccessState.Denied)
                return AccessState.Denied;

            var fine = this.platform.GetCurrentPermissionStatus(P.AccessFineLocation);
            if (fine == AccessState.Denied)
                return AccessState.Denied;

            var bg = AccessState.Available;
            if (OperatingSystemShim.IsAndroidVersionAtLeast(29))
            {
                bg = this.platform.GetCurrentPermissionStatus(P.AccessBackgroundLocation);
                if (bg == AccessState.Denied)
                    return AccessState.Denied;
            }
            if (new[] { coarse, fine, bg }.Any(x => x == AccessState.Unknown))
                return AccessState.Unknown;

            return AccessState.Available;
        }
    }


    public async Task<AccessState> RequestAccess()
    {
        var status = AccessState.Denied;
        var result = await this.platform.RequestPermissions(P.AccessCoarseLocation, P.AccessFineLocation).ToTask();
        if (result.IsSuccess())
        {
            status = AccessState.Available;
            if (OperatingSystemShim.IsAndroidVersionAtLeast(29))
                status = await this.platform.RequestAccess(P.AccessBackgroundLocation);
        }

        return status;
    }


    public IList<GeofenceRegion> GetMonitorRegions()
        => this.repository.GetList<GeofenceRegion>();


    public async Task StartMonitoring(GeofenceRegion region)
    {
        (await this.RequestAccess().ConfigureAwait(false)).Assert();
        await this.Create(region).ConfigureAwait(false);
        this.repository.Set(region);
    }


    public Task StopMonitoring(string identifier)
    {
        this.repository.Remove<GeofenceRegion>(identifier);
        return this.client.RemoveGeofencesAsync(new List<string> { identifier });
    }


    public async Task StopAllMonitoring()
    {
        var regions = this.repository.GetList<GeofenceRegion>();
        var regionIds = regions.Select(x => x.Identifier).ToArray();
        if (regionIds.Any())
            await this.client.RemoveGeofencesAsync(regionIds).ConfigureAwait(false);

        this.repository.Clear<GeofenceRegion>();
    }


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken)
    {
        var location = await LocationServices
            .GetFusedLocationProviderClient(this.platform.AppContext)
            .GetLastLocationAsync()
            .ConfigureAwait(false);

        if (location == null)
            return GeofenceState.Unknown;

        var inside = region.IsPositionInside(new Position(location.Latitude, location.Longitude));
        var state = inside ? GeofenceState.Entered : GeofenceState.Exited;
        return state;
    }


    protected virtual Task Create(GeofenceRegion region)
    {
        var transitions = this.GetTransitions(region);

        var geofence = new GeofenceBuilder()
            .SetRequestId(region.Identifier)
            .SetExpirationDuration(Geofence.NeverExpire)
            .SetCircularRegion(
                region.Center.Latitude,
                region.Center.Longitude,
                Convert.ToSingle(region.Radius.TotalMeters)
            )
            .SetTransitionTypes(transitions)
            .Build();

        var request = new GeofencingRequest.Builder()
            .SetInitialTrigger(0)
            .AddGeofence(geofence)
            .Build();

        return this.client.AddGeofencesAsync(
            request,
            this.GetPendingIntent()
        );
    }


    protected virtual int GetTransitions(GeofenceRegion region)
    {
        var i = 0;
        if (region.NotifyOnEntry)
            i += Geofence.GeofenceTransitionEnter;

        if (region.NotifyOnExit)
            i += Geofence.GeofenceTransitionExit;

        return i;
    }


    protected virtual PendingIntent GetPendingIntent()
        => this.geofencePendingIntent ??= this.platform.GetBroadcastPendingIntent<GeofenceBroadcastReceiver>(IntentAction, PendingIntentFlags.UpdateCurrent);
}
