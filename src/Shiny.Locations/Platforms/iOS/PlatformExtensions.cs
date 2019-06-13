using System;
using CoreLocation;


namespace Shiny.Locations
{
    static class PlatformExtensions
    {
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
