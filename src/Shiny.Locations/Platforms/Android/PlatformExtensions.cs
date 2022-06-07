using System;
using Android.Gms.Location;


namespace Shiny.Locations
{
    static class PlatformExtensions
    {
        public static LocationRequest ToNative(this GpsRequest request)
        {
            var nativeRequest = LocationRequest.Create();
            var interval = TimeSpan.FromSeconds(30).TotalMilliseconds;

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
                    nativeRequest
                        .SetPriority(LocationRequest.PriorityHighAccuracy)
                        .SetInterval(1000) // every second
                        .SetSmallestDisplacement(1);

                    break;
            }

            //if (request.ThrottledInterval != null)
            //    nativeRequest.SetFastestInterval(request.ThrottledInterval.Value.ToMillis());

            //if (request.MinimumDistance != null)
            //    nativeRequest.SetSmallestDisplacement((float)request.MinimumDistance.TotalMeters);

            return nativeRequest;
        }
    }
}
