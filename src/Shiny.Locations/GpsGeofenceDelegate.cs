using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Locations;


/// <summary>
/// Drives geofence transitions (including dwell) from realtime GPS readings - registered by <c>AddGpsDirectGeofencing</c>.
/// </summary>
public class GpsGeofenceDelegate : IGpsDelegate, IShinyStartupTask
{
    readonly IGeofenceManager geofenceManager;
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly GeofenceDwellTracker dwell;
    public Dictionary<string, GeofenceState> CurrentStates { get; } = new();


    public GpsGeofenceDelegate(
        IGeofenceManager geofenceManager,
        IRepository repository,
        IServiceProvider services,
        ILogger<GpsGeofenceDelegate> logger
    )
    {
        this.geofenceManager = geofenceManager;
        this.services = services;
        this.logger = logger;
        this.dwell = new GeofenceDwellTracker(
            repository,
            logger,
            geofenceManager.RequestState,
            r => this.FireDelegate(r, GeofenceState.Dwelling)
        );
    }


    public void Start()
    {
        try
        {
            this.dwell.Restore();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to restore pending geofence dwells");
        }
    }


    public async Task OnReading(GpsReading reading)
    {
        var geofences = this.geofenceManager.GetMonitorRegions();

        foreach (var geofence in geofences)
        {
            var state = geofence.IsPositionInside(reading.Position)
                ? GeofenceState.Entered
                : GeofenceState.Exited;

            var current = this.GetState(geofence.Identifier);
            if (state != current)
            {
                this.SetState(geofence.Identifier, state);

                // the first reading only establishes where the device is, like the native managers' initial state:
                // no Entered/Exited is reported for it, but it still starts (or ends) a dwell
                var initial = current == GeofenceState.Unknown;

                if (state == GeofenceState.Entered)
                {
                    this.dwell.Entered(geofence, reading.Timestamp);
                    if (!initial && geofence.NotifyOnEntry)
                        await this.FireDelegate(geofence, state).ConfigureAwait(false);
                }
                else
                {
                    // reports Dwelling first if the stay lasted the dwell time and the timer didn't already
                    await this.dwell.Exited(geofence, reading.Timestamp).ConfigureAwait(false);
                    if (!initial && geofence.NotifyOnExit && this.CurrentStates.ContainsKey(geofence.Identifier))
                        await this.FireDelegate(geofence, state).ConfigureAwait(false);
                }
            }
        }
    }


    protected GeofenceState GetState(string geofenceId)
        => this.CurrentStates.ContainsKey(geofenceId)
            ? this.CurrentStates[geofenceId]
            : GeofenceState.Unknown;


    protected virtual void SetState(string geofenceId, GeofenceState state)
    {
        this.CurrentStates[geofenceId] = state;
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
        {
            this.CurrentStates.Remove(region.Identifier);
            await this.geofenceManager.StopMonitoring(region.Identifier).ConfigureAwait(false);
        }
    }
}
