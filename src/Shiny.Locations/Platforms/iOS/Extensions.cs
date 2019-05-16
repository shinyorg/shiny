using System;
using System.Threading.Tasks;
using CoreLocation;


namespace Shiny.Locations
{
    public static class iOSExtensions
    {
        public static void TrySetDeferrals(this CLLocationManager manager, GpsRequest request)
        {
            var meters = request?.DeferredDistance?.TotalMeters ?? 0;
            var secs = request?.DeferredTime?.TotalSeconds ?? 0;
            if (meters > 0 || secs > 0)
            {
                ((GpsManagerDelegate)manager.Delegate).Request = request;
                manager.AllowDeferredLocationUpdatesUntil(meters, secs);
            }
        }


        public static async Task<AccessState> RequestGpsAccess(this CLLocationManager locationManager, bool backgroundMode)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            var gdelegate = (GpsManagerDelegate)locationManager.Delegate;
            var result = CLLocationManager.Status;

            if (result == CLAuthorizationStatus.NotDetermined)
            {
                var tcs = new TaskCompletionSource<CLAuthorizationStatus>();
                var handler = new EventHandler<CLAuthorizationStatus>((sender, status) => tcs.TrySetResult(status));
                gdelegate.AuthStatusChanged += handler;

                if (backgroundMode)
                    locationManager.RequestAlwaysAuthorization();
                else
                    locationManager.RequestWhenInUseAuthorization();

                result = await tcs.Task;
                gdelegate.AuthStatusChanged -= handler;
            }
            return result.FromNative(backgroundMode);
        }


        public static async Task<AccessState> RequestGeofenceAccess(this CLLocationManager locationManager)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            if (!CLLocationManager.IsMonitoringAvailable(typeof(CLCircularRegion)))
                return AccessState.NotSupported;

            var gdelegate = (GeofenceManagerDelegate)locationManager.Delegate;
            var result = CLLocationManager.Status;

            if (result == CLAuthorizationStatus.NotDetermined)
            {
                var tcs = new TaskCompletionSource<CLAuthorizationStatus>();
                var handler = new EventHandler<CLAuthorizationStatus>((sender, status) => tcs.TrySetResult(status));
                gdelegate.AuthStatusChanged += handler;
                locationManager.RequestAlwaysAuthorization();

                result = await tcs.Task;
                gdelegate.AuthStatusChanged -= handler;
            }
            return result.FromNative(true);
        }

        public static AccessState CurrentAccessStatus(this CLLocationManager locationManager)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            //if (!CLLocationManager.IsMonitoringAvailable(typeof(CLCircularRegion)))
            //    return AccessState.NotSupported;

            return CLLocationManager.Status.FromNative(true);
        }



        public static GeofenceState FromNative(this CLRegionState state)
        {
            switch (state)
            {
                case CLRegionState.Inside:
                    return GeofenceState.Entered;

                case CLRegionState.Outside:
                    return GeofenceState.Exited;

                case CLRegionState.Unknown:
                default:
                    return GeofenceState.Unknown;
            }
        }


        public static CLLocationCoordinate2D ToNative(this Position position)
            => new CLLocationCoordinate2D(position.Latitude, position.Longitude);


        public static CLCircularRegion ToNative(this GeofenceRegion region)
            => new CLCircularRegion
            (
                region.Center.ToNative(),
                region.Radius.TotalMeters,
                region.Identifier
            )
            {
                NotifyOnEntry = region.NotifyOnEntry,
                NotifyOnExit = region.NotifyOnExit,
            };
    }
}
