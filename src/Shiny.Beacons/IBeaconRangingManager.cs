using System;
using System.Threading.Tasks;

namespace Shiny.Beacons;


/// <summary>
/// Continuously reports the iBeacons in range along with an estimated distance to each.
/// </summary>
/// <remarks>
/// Ranging is a foreground activity - it needs the screen on and the app in front of the user.
/// To be told about beacons while the app is backgrounded or not running, use
/// <see cref="IBeaconMonitoringManager"/> instead.
/// </remarks>
public interface IBeaconRangingManager
{
    /// <summary>
    /// The current permission state, without prompting the user.
    /// </summary>
    AccessState CurrentStatus { get; }

    /// <summary>
    /// Requests the permissions ranging needs.
    /// </summary>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Emits an observation each time a beacon belonging to the region is seen.
    /// </summary>
    /// <param name="region">The beacons to watch for.</param>
    /// <returns>
    /// An observable of observations. Ranging starts on first subscription and stops when the last
    /// subscription is disposed.
    /// </returns>
    IObservable<Beacon> WhenBeaconRanged(BeaconRegion region);
}
