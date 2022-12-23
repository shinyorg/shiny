using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Locations;


public class GpsGeofenceDelegate : NotifyPropertyChanged, IGpsDelegate
{
    readonly IGeofenceManager geofenceManager;
    readonly IGeofenceDelegate geofenceDelegate;
    public Dictionary<string, GeofenceState> CurrentStates { get; } = new();


    public GpsGeofenceDelegate(IGeofenceManager geofenceManager, IGeofenceDelegate geofenceDelegate)
    {
        this.CurrentStates = new Dictionary<string, GeofenceState>();
        this.geofenceManager = geofenceManager;
        this.geofenceDelegate = geofenceDelegate;
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
                await this.geofenceDelegate.OnStatusChanged(state, geofence);
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
        this.RaisePropertyChanged(nameof(this.CurrentStates));
    }
}

