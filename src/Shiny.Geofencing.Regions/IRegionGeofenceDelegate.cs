using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Receives region geofence transitions. Implement this and register it with
/// <c>AddRegionGeofencing&lt;TDelegate&gt;</c>; it is resolved from the container as a singleton and is
/// invoked on a background thread, including while the app is backgrounded.
/// </summary>
public interface IRegionGeofenceDelegate
{
    /// <summary>
    /// Invoked once per region transition. Moving straight from region A to region B in the same set
    /// raises two calls - the exit from A first, then the entry into B.
    /// </summary>
    /// <param name="change">The transition.</param>
    Task OnRegionChanged(RegionChange change);
}
