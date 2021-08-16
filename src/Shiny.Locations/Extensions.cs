using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;


namespace Shiny.Locations
{
    public static class Extensions
    {
        /// <summary>
        /// This method will start the GPS with realtime and close on disposable (or when the app backgrounds)
        /// </summary>
        /// <param name="gpsManager"></param>
        /// <returns></returns>
        public static IObservable<IGpsReading> StartAndReceive(this IGpsManager gpsManager) => Observable.Create<IGpsReading>(ob =>
        {
            var composite = new CompositeDisposable();
            var platform = ShinyHost.Resolve<IPlatform>();
            gpsManager
                .WhenReading()
                .Subscribe(
                    ob.OnNext,
                    ob.OnError
                )
                .DisposedBy(composite);

            platform
                .WhenStateChanged()
                .Where(x => x == PlatformState.Background)
                .Subscribe(_ => ob.Respond(null))
                .DisposedBy(composite);

            gpsManager
                .StartListener(GpsRequest.Foreground)
                .ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        ob.OnError(x.Exception);
                });

            return () =>
            {
                composite.Dispose();
                gpsManager.StopListener();
            };
        });



        /// <summary>
        /// Requests a single GPS reading by starting the listener and stopping once a reading is received
        /// Requests a single GPS reading - This will start & stop the gps listener if wasn't running already
        /// </summary>
        /// <param name="gpsManager"></param>
        /// <returns></returns>
        public static IObservable<IGpsReading> GetCurrentPosition(this IGpsManager gpsManager) => Observable.FromAsync(async ct =>
        {
            var iStarted = false;
            try
            {
                var task = gpsManager.WhenReading().Take(1).ToTask(ct);
                if (!gpsManager.IsListening())
                {
                    iStarted = true;
                    await gpsManager.StartListener(GpsRequest.Foreground);
                }

                var reading = await task.ConfigureAwait(false);
                return reading;
            }
            finally
            {
                if (iStarted)
                    await gpsManager.StopListener();
            }
        });


        /// <summary>
        /// Gets the last reading (can be filtered out by maxAgeOfLastReading) otherwise request the current reading
        /// </summary>
        /// <param name="gpsManager"></param>
        /// <param name="maxAgeOfLastReading"></param>
        /// <returns></returns>
        public static IObservable<IGpsReading> GetLastReadingOrCurrentPosition(this IGpsManager gpsManager, DateTime? maxAgeOfLastReading = null) => Observable.FromAsync<IGpsReading>(async ct =>
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
            var distance = region.Center.GetDistanceTo(position);
            var inside = distance.TotalMeters <= region.Radius.TotalMeters;
            return inside;
        }


        ///// <summary>
        ///// Requests access for GPS and starts listening for changes.
        ///// </summary>
        ///// <param name="gps">The gps manager.</param>
        ///// <param name="request">The gps request.</param>
        ///// <returns></returns>
        //public static async Task<AccessState> RequestAccessAndStart(this IGpsManager gps, GpsRequest request)
        //{
        //    var access = await gps.RequestAccess(request);
        //    if (access == AccessState.Available)
        //        await gps.StartListener(request);

        //    return access;
        //}
    }
}
