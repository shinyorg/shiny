using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;
using P = Android.Manifest.Permission;
using Shiny.Extensions.Stores.Repositories;

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
                    var state = FromNativeTransition(e.GeofenceTransition);
                    foreach (var triggeringGeofence in e.TriggeringGeofences)
                    {
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

                            if (region.IsSingleUseComplete(state))
                                await this.StopMonitoring(region.Identifier).ConfigureAwait(false);
                        }
                    }
                }
            };
            var regions = this.repository.GetAll<GeofenceRegion>();
            foreach (var region in regions)
                await this.Create(region, true);
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
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
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
        var result = await this.platform.RequestPermissions(P.AccessCoarseLocation, P.AccessFineLocation);
        if (result.IsSuccess())
        {
            status = AccessState.Available;
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                status = await this.platform.RequestAccess(P.AccessBackgroundLocation);
        }

        return status;
    }


    public IList<GeofenceRegion> GetMonitorRegions()
        => this.repository.GetAll<GeofenceRegion>().ToList();


    public async Task StartMonitoring(GeofenceRegion region)
    {
        await this.Create(region).ConfigureAwait(false);
        this.repository.Set(region);
    }


    public async Task StopMonitoring(string identifier)
    {
        await this.client.RemoveGeofencesAsync(new List<string> { identifier }).ConfigureAwait(false);
        this.repository.Remove<GeofenceRegion>(identifier);
    }


    public async Task StopAllMonitoring()
    {
        var regions = this.repository.GetAll<GeofenceRegion>();
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


    /// <param name="region">The region to register with the OS.</param>
    /// <param name="restoring">
    /// True when re-registering at startup - the device's state was already reported for this region, so no initial trigger.
    /// </param>
    protected virtual Task Create(GeofenceRegion region, bool restoring = false)
    {
        var transitions = this.GetTransitions(region);

        var builder = new GeofenceBuilder()
            .SetRequestId(region.Identifier)
            .SetExpirationDuration(Geofence.NeverExpire)
            .SetCircularRegion(
                region.Center.Latitude,
                region.Center.Longitude,
                Convert.ToSingle(region.Radius.TotalMeters)
            )
            .SetTransitionTypes(transitions);

        if (region.DwellTime is { } dwell)
            builder.SetLoiteringDelay(Convert.ToInt32(Math.Min(dwell.TotalMilliseconds, int.MaxValue)));

        var geofence = builder.Build();

        var request = new GeofencingRequest.Builder()
            .SetInitialTrigger(this.GetInitialTrigger(region, restoring))
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

        if (region.DwellTime != null)
            i += Geofence.GeofenceTransitionDwell;

        return i;
    }


    // Matches iOS: already being inside when monitoring starts never reports Entered, but it does count toward a
    // dwell - INITIAL_TRIGGER_DWELL reports Dwelling once the device has been inside for the loitering delay.
    // Not on restore: re-registering at startup would report the dwell again for a stay that already had one.
    protected virtual int GetInitialTrigger(GeofenceRegion region, bool restoring)
        => region.DwellTime != null && !restoring
            ? GeofencingRequest.InitialTriggerDwell
            : 0;


    static GeofenceState FromNativeTransition(int transition) => transition switch
    {
        Geofence.GeofenceTransitionEnter => GeofenceState.Entered,
        Geofence.GeofenceTransitionExit => GeofenceState.Exited,
        Geofence.GeofenceTransitionDwell => GeofenceState.Dwelling,
        _ => GeofenceState.Unknown
    };


    protected virtual PendingIntent GetPendingIntent()
        => this.geofencePendingIntent ??= this.platform.GetBroadcastPendingIntent<GeofenceBroadcastReceiver>(IntentAction, PendingIntentFlags.UpdateCurrent);
}
