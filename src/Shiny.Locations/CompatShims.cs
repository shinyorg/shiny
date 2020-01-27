using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny
{
    public static class ShinyGps
    {
        static IGpsManager Current { get; } = ShinyHost.Resolve<IGpsManager>();

        public static bool IsListening => Current.IsListening;
        public static AccessState GetCurrentStatus(bool background) => Current.GetCurrentStatus(background);
        public static IObservable<IGpsReading?> GetLastReading() => Current.GetLastReading();
        public static Task<AccessState> RequestAccess(bool backgroundMode) => Current.RequestAccess(backgroundMode);
        public static Task StartListener(GpsRequest request) => Current.StartListener(request);
        public static Task StopListener() => Current.StopListener();
        public static IObservable<AccessState> WhenAccessStatusChanged(bool forBackground) => Current.WhenAccessStatusChanged(forBackground);
        public static IObservable<IGpsReading> WhenReading() => Current.WhenReading();
    }


    public static class ShinyGeofences
    {
        static IGeofenceManager Current { get; } = ShinyHost.Resolve<IGeofenceManager>();

        public static AccessState Status => Current.Status;
        public static Task<IEnumerable<GeofenceRegion>> GetMonitorRegions() => Current.GetMonitorRegions();
        public static Task<AccessState> RequestAccess() => Current.RequestAccess();
        public static Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
            => Current.RequestState(region, cancelToken);
        public static Task StartMonitoring(GeofenceRegion region) => Current.StartMonitoring(region);
        public static Task StopAllMonitoring() => Current.StopAllMonitoring();
        public static Task StopMonitoring(string identifier) => Current.StopMonitoring(identifier);
        public static IObservable<AccessState> WhenAccessStatusChanged() => Current.WhenAccessStatusChanged();
    }


    public static class ShinyMotionActivity
    {
        static IMotionActivityManager Current { get; } = ShinyHost.Resolve<IMotionActivityManager>();

        public static Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
            => Current.Query(start, end);
        public static Task<AccessState> RequestPermission() => Current.RequestPermission();
        public static IObservable<MotionActivityEvent> WhenActivityChanged() => Current.WhenActivityChanged();
    }
}
