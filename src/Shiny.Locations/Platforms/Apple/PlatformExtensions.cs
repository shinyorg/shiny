using System;
using CoreLocation;

namespace Shiny.Locations;


static class PlatformExtensions
{

    public static Position FromNative(this CLLocationCoordinate2D native)
        => new Position(native.Latitude, native.Longitude);


    public static GpsReading FromNative(this CLLocation location) => new GpsReading(
        location.Coordinate.FromNative(),
        location.HorizontalAccuracy,
        DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(location.Timestamp.SecondsSince1970)),
        location.Course,
        location.VerticalAccuracy,
        location.Altitude,
        location.Speed,
        location.SpeedAccuracy
    );


    public static GeofenceState FromNative(this CLRegionState state) => state switch
    {
         CLRegionState.Inside => GeofenceState.Entered,
         CLRegionState.Outside => GeofenceState.Exited,
         _ => GeofenceState.Unknown
    };


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
