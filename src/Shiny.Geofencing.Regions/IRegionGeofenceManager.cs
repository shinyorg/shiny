using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Controls GPS-driven geofence monitoring over region sets. Inject it into a page or view model.
/// </summary>
public interface IRegionGeofenceManager
{
    /// <summary>Whether monitoring is currently running. Survives an app restart.</summary>
    bool IsStarted { get; }

    /// <summary>Requests the GPS permissions the monitor needs (background location).</summary>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Starts background GPS and region detection. Idempotent - calling it while already started is a no-op.
    /// </summary>
    Task Start();

    /// <summary>Stops monitoring and forgets which regions the device was in.</summary>
    Task Stop();

    /// <summary>
    /// Resolves the device's current region in each monitored set from the last GPS reading, without
    /// raising transitions or touching the monitor's state.
    /// </summary>
    /// <param name="cancellationToken">Cancels the pending queries.</param>
    /// <returns>One entry per monitored set. Empty when no GPS reading is available.</returns>
    Task<IReadOnlyList<CurrentRegion>> GetCurrent(CancellationToken cancellationToken = default);
}
