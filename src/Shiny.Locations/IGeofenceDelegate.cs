using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// The transition state of a monitored geofence region.
/// </summary>
public enum GeofenceState
{
    /// <summary>
    /// The current state of the region has not yet been determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The device is currently inside the region.
    /// </summary>
    Entered = 1,

    /// <summary>
    /// The device is currently outside the region.
    /// </summary>
    Exited = 2
}


/// <summary>
/// Background listener invoked by the geofence manager when a monitored region's transition state changes.
/// </summary>
public interface IGeofenceDelegate
{
    /// <summary>
    /// Invoked when the device crosses the boundary of a monitored geofence region.
    /// </summary>
    /// <param name="newStatus">The new transition state of the region.</param>
    /// <param name="region">The region whose state changed.</param>
    Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
}
