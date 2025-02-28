using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


public interface IGeofenceManager
{
    /// <summary>
    /// Gets the current permission status
    /// </summary>
    AccessState CurrentStatus { get; }

    /// <summary>
    /// Requests/ensures appropriate platform permissions where necessary
    /// </summary>
    /// <returns></returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Current set of geofences being monitored
    /// </summary>
    IList<GeofenceRegion> GetMonitorRegions();

    /// <summary>
    /// Start monitoring a geofence
    /// </summary>
    /// <param name="region"></param>
    Task StartMonitoring(GeofenceRegion region);

    /// <summary>
    /// Stop monitoring a geofence
    /// </summary>
    /// <param name="identifier"></param>
    Task StopMonitoring(string identifier);

    /// <summary>
    /// Stop monitoring all active geofences
    /// </summary>
    Task StopAllMonitoring();

    /// <summary>
    /// This will request the current status of a geofence region
    /// </summary>
    /// <param name="region"></param>
    /// <param name="cancelToken"></param>
    /// <returns>Status of geofence</returns>
    Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default);
}