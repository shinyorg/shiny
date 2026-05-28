using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


public static class Extensions
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
    /// Requests a single GPS reading by starting the listener and stopping once a reading is received.
    /// This will start and stop the GPS listener if it wasn't already running.
    /// </summary>
    /// <param name="gpsManager"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<GpsReading?> GetCurrentPosition(this IGpsManager gpsManager, CancellationToken cancellationToken = default)
    {
        var iStarted = false;
        try
        {
            await currentLocSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            var tcs = new TaskCompletionSource<GpsReading?>();

            EventHandler<GpsReading> handler = null!;
            handler = (_, reading) =>
            {
                gpsManager.GpsReadingReceived -= handler;
                tcs.TrySetResult(reading);
            };
            gpsManager.GpsReadingReceived += handler;

            cancellationToken.Register(() =>
            {
                gpsManager.GpsReadingReceived -= handler;
                tcs.TrySetCanceled(cancellationToken);
            });

            if (!gpsManager.IsListening())
            {
                iStarted = true;
                await gpsManager.StartListener(GpsRequest.Foreground).ConfigureAwait(false);
            }

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            if (iStarted)
                await gpsManager.StopListener().ConfigureAwait(false);

            currentLocSemaphore.Release();
        }
    }
    static readonly SemaphoreSlim currentLocSemaphore = new SemaphoreSlim(1, 1);


    /// <summary>
    /// Gets the last reading (can be filtered out by maxAgeOfLastReading) otherwise requests the current reading.
    /// </summary>
    /// <param name="gpsManager"></param>
    /// <param name="maxAgeOfLastReading"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<GpsReading?> GetLastReadingOrCurrentPosition(this IGpsManager gpsManager, DateTime? maxAgeOfLastReading = null, CancellationToken cancellationToken = default)
    {
        var reading = await gpsManager.GetLastReading().ConfigureAwait(false);
        if (reading == null || (maxAgeOfLastReading != null && reading.Timestamp < maxAgeOfLastReading.Value))
            reading = await gpsManager.GetCurrentPosition(cancellationToken).ConfigureAwait(false);

        return reading;
    }


    /// <summary>
    /// Returns true if there is a current GPS listener configuration running.
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
    public static bool IsListening(this IGpsManager manager) => manager.CurrentListener != null;


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
    /// Returns null if current position could not be determined - else returns true if in region, false otherwise.
    /// </summary>
    /// <param name="gpsManager"></param>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public static async Task<bool?> IsInsideRegion(this IGpsManager gpsManager, Position center, Distance radius, CancellationToken cancelToken = default)
    {
        var result = await gpsManager.GetCurrentPosition(cancelToken).ConfigureAwait(false);
        if (result == null)
            return null;

        var inside = result.Position.GetDistanceTo(center) < radius;
        return inside;
    }
}
