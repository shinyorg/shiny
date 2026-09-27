using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Manages geofence regions, permissions, and enter/exit transition monitoring on the device.
/// </summary>
public interface IGeofenceManager
{
    /// <summary>
    /// Gets the current platform permission status for geofence monitoring.
    /// </summary>
    AccessState CurrentStatus { get; }

    /// <summary>
    /// Requests the platform permissions required to monitor geofences.
    /// </summary>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Returns the set of geofence regions currently being monitored.
    /// </summary>
    IList<GeofenceRegion> GetMonitorRegions();

    /// <summary>
    /// Starts monitoring a geofence region for enter and exit transitions.
    /// </summary>
    /// <param name="region">The region to monitor.</param>
    Task StartMonitoring(GeofenceRegion region);

    /// <summary>
    /// Stops monitoring the geofence region with the specified identifier.
    /// </summary>
    /// <param name="identifier">The identifier of the region to stop monitoring.</param>
    Task StopMonitoring(string identifier);

    /// <summary>
    /// Stops monitoring all currently active geofence regions.
    /// </summary>
    Task StopAllMonitoring();

    /// <summary>
    /// Queries the platform for the current enter/exit state of a geofence region.
    /// </summary>
    /// <param name="region">The region to evaluate.</param>
    /// <param name="cancelToken">Cancels the pending request.</param>
    /// <returns>The current state of the region (entered, exited, or unknown).</returns>
    Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default);
}
