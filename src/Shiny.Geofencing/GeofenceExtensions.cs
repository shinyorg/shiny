using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Convenience extensions over <see cref="IGeofenceManager"/> and <see cref="GeofenceRegion"/>.
/// </summary>
public static class GeofenceExtensions
{
    /// <summary>
    /// Tries to create the geofence region if the identifier does not already exist - returns true if it exists
    /// </summary>
    /// <param name="geofenceManager">The geofence manager abstraction</param>
    /// <param name="region">The geofence region</param>
    /// <param name="replaceIfExists">This will replace the geofence if the identifier already exists - maybe the position or notification types have changed</param>
    /// <returns></returns>
    public static async Task<bool> TryStartMonitoring(
        this IGeofenceManager geofenceManager, 
        GeofenceRegion region,
        bool replaceIfExists = true
    )
    {
        var exists = geofenceManager
            .GetMonitorRegions()
            .Any(x => x.Identifier.Equals(region.Identifier, StringComparison.InvariantCultureIgnoreCase));

        if (exists)
        {
            if (replaceIfExists)
            {
                await geofenceManager.StopMonitoring(region.Identifier);
                await geofenceManager.StartMonitoring(region);
            }
        }
        else
        {
            await geofenceManager.StartMonitoring(region);
        }
        return exists;
    }


    /// <summary>
    /// Determines if the provided position is inside the region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static bool IsPositionInside(this GeofenceRegion region, Position position)
    {
        var center = new Position(region.Center.Latitude, region.Center.Longitude);
        var distance = center.GetDistanceTo(position);
        var inside = distance.TotalMeters <= region.Radius.TotalMeters;
        return inside;
    }


    /// <summary>
    /// A single-use region is removed after its first transition - unless it has a dwell time, then after the dwell.
    /// </summary>
    internal static bool IsSingleUseComplete(this GeofenceRegion region, GeofenceState state)
        => region.SingleUse && (region.DwellTime == null || state == GeofenceState.Dwelling);
}
