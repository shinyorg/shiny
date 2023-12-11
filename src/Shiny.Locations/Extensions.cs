using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


public static class Extensions
{
    ///// <summary>
    ///// This method will start the GPS with realtime and close on disposable (or when the app backgrounds)
    ///// </summary>
    ///// <param name="gpsManager"></param>
    ///// <returns></returns>
    //public static IObservable<GpsReading> StartAndReceive(this IGpsManager gpsManager) => Observable.Create<GpsReading>(ob =>
    //{
    //    var composite = new CompositeDisposable();
    //    var platform = ShinyHost.Resolve<IPlatform>();
    //    gpsManager
    //        .WhenReading()
    //        .Subscribe(
    //            ob.OnNext,
    //            ob.OnError
    //        )
    //        .DisposedBy(composite);

    //    platform
    //        .WhenStateChanged()
    //        .Where(x => x == PlatformState.Background)
    //        .Subscribe(_ => ob.Respond(null))
    //        .DisposedBy(composite);

    //    gpsManager
    //        .StartListener(GpsRequest.Foreground)
    //        .ContinueWith(x =>
    //        {
    //            if (x.IsFaulted)
    //                ob.OnError(x.Exception);
    //        });

    //    return () =>
    //    {
    //        composite.Dispose();
    //        gpsManager.StopListener();
    //    };
    //});



    /// <summary>
    /// Requests a single GPS reading by starting the listener and stopping once a reading is received
    /// Requests a single GPS reading - This will start & stop the gps listener if wasn't running already
    /// </summary>
    /// <param name="gpsManager"></param>
    /// <returns></returns>
    public static IObservable<GpsReading> GetCurrentPosition(this IGpsManager gpsManager) => Observable.FromAsync(async ct =>
    {
        var iStarted = false;
        try
        {
            await currentLocSemaphore
                .WaitAsync(ct)
                .ConfigureAwait(false);

            var task = gpsManager
                .WhenReading()
                .Take(1)
                .ToTask(ct);

            if (!gpsManager.IsListening())
            {
                iStarted = true;
                await gpsManager
                    .StartListener(GpsRequest.Foreground)
                    .ConfigureAwait(false);
            }
            var reading = await task.ConfigureAwait(false);

            return reading;
        }
        finally
        {
            if (iStarted)
                await gpsManager.StopListener().ConfigureAwait(false);

            currentLocSemaphore.Release();
        }
    });
    static readonly SemaphoreSlim currentLocSemaphore = new SemaphoreSlim(1, 1);


    /// <summary>
    /// Gets the last reading (can be filtered out by maxAgeOfLastReading) otherwise request the current reading
    /// </summary>
    /// <param name="gpsManager"></param>
    /// <param name="maxAgeOfLastReading"></param>
    /// <returns></returns>
    public static IObservable<GpsReading> GetLastReadingOrCurrentPosition(this IGpsManager gpsManager, DateTime? maxAgeOfLastReading = null) => Observable.FromAsync<GpsReading>(async ct =>
    {
        var reading = await gpsManager.GetLastReading().ToTask(ct);
        if (reading == null || (maxAgeOfLastReading != null && reading.Timestamp < maxAgeOfLastReading.Value))
            reading = await gpsManager.GetCurrentPosition().ToTask(ct);

        return reading;
    });


    /// <summary>
    /// Returns true if there is a current GPS listener configuration running
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
    ///
    /// </summary>
    /// <param name="gpsManager"></param>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="cancelToken"></param>
    /// <returns>Returns null if current position could not be determined - else returns true if in region, false otherwise</returns>
    public static async Task<bool?> IsInsideRegion(this IGpsManager gpsManager, Position center, Distance radius, CancellationToken cancelToken = default)
    {
        var result = await gpsManager.GetCurrentPosition().ToTask(cancelToken);
        var inside = result.Position.GetDistanceTo(center) < radius;
        return inside;
    }
}
