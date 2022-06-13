using System;
using Android.Locations;
using LocationRequest = Android.Gms.Location.LocationRequest;

namespace Shiny.Locations;


static class PlatformExtensions
{
    public static GpsReading FromNative(this Location location) => new GpsReading(
        new Position(location.Latitude, location.Longitude),
        location.Accuracy,
        DateTimeOffset.FromUnixTimeMilliseconds(location.Time).UtcDateTime,
        location.Bearing,
        location.BearingAccuracyDegrees,
        location.Altitude,
        location.Speed,
        location.SpeedAccuracyMetersPerSecond
    );


    public static LocationRequest ToNative(this GpsRequest request)
    {
        var nativeRequest = LocationRequest.Create();

        switch (request.Accuracy)
        {
            case GpsAccuracy.Lowest:
                nativeRequest
                    .SetPriority(LocationRequest.PriorityLowPower)
                    .SetInterval(1000 * 120) // 2 mins
                    .SetSmallestDisplacement(3000);
                break;

            case GpsAccuracy.Low:
                nativeRequest
                    .SetPriority(LocationRequest.PriorityLowPower)
                    .SetInterval(1000 * 60) // 1 min
                    .SetSmallestDisplacement(1000);
                break;

            case GpsAccuracy.Normal:
                nativeRequest
                    .SetPriority(LocationRequest.PriorityBalancedPowerAccuracy)
                    .SetInterval(1000 * 30) // 30 seconds
                    .SetSmallestDisplacement(100);
                break;

            case GpsAccuracy.High:
                nativeRequest
                    .SetPriority(LocationRequest.PriorityHighAccuracy)
                    .SetInterval(1000 * 10) // 10 seconds
                    .SetSmallestDisplacement(10);
                break;

            case GpsAccuracy.Highest:
                nativeRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
                nativeRequest
                    .SetPriority(LocationRequest.PriorityHighAccuracy)
                    .SetInterval(1000) // every second
                    .SetSmallestDisplacement(1);

                break;
        }

        return nativeRequest;
    }
}
