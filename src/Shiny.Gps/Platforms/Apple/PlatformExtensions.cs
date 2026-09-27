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


    public static CLLocationCoordinate2D ToNative(this Position position)
        => new CLLocationCoordinate2D(position.Latitude, position.Longitude);
}
