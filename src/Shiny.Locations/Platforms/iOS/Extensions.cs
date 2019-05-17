using System;
using CoreLocation;


namespace Shiny.Locations
{
    internal static class iOSExtensions
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
                NotifyOnExit = region.NotifyOnExit
            };
    }
}
