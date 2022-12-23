using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Beacons;


public interface IBeaconMonitoringManager
{
    /// <summary>
    /// Request necessary permissions to beacon scanning
    /// </summary>
    /// <returns></returns>
    Task<AccessState> RequestAccess();


    /// <summary>
    /// Current set of geofences being monitored
    /// </summary>
    IList<BeaconRegion> GetMonitoredRegions();


    /// <summary>
    /// Initiates a background beacon region monitor
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    Task StartMonitoring(BeaconRegion region);


    /// <summary>
    /// Stops monitoring a beacon region
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    void StopMonitoring(string identifier);


    /// <summary>
    /// Stops all monitoring of beacon regions
    /// </summary>
    /// <returns></returns>
    void StopAllMonitoring();
}
