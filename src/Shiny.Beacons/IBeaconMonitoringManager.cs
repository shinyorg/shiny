using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Beacons;


/// <summary>
/// Watches beacon regions in the background and raises enter and exit transitions through
/// <see cref="IBeaconMonitorDelegate"/>.
/// </summary>
/// <remarks>
/// Monitored regions survive a process restart - they are persisted and re-armed on next startup.
/// Monitoring reports only that a region was entered or left; it does not report distance. Range
/// the region while the app is in the foreground when you need that.
/// </remarks>
public interface IBeaconMonitoringManager
{
    /// <summary>
    /// The current permission state, without prompting the user.
    /// </summary>
    AccessState CurrentStatus { get; }

    /// <summary>
    /// Requests the permissions background monitoring needs.
    /// </summary>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// The regions currently being monitored.
    /// </summary>
    IList<BeaconRegion> GetMonitoredRegions();

    /// <summary>
    /// Starts monitoring a region for enter and exit transitions.
    /// </summary>
    /// <param name="region">The region to monitor.</param>
    Task StartMonitoring(BeaconRegion region);

    /// <summary>
    /// Stops monitoring the region with the supplied identifier.
    /// </summary>
    /// <param name="identifier">The identifier of the region to drop.</param>
    Task StopMonitoring(string identifier);

    /// <summary>
    /// Stops monitoring every region.
    /// </summary>
    Task StopAllMonitoring();

    /// <summary>
    /// Asks for the current enter/exit state of a region.
    /// </summary>
    /// <param name="region">The region to evaluate.</param>
    /// <param name="cancelToken">Cancels the pending request.</param>
    Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken cancelToken = default);
}
