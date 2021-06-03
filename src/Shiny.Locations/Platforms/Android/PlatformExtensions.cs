using System;
using Android.Gms.Location;


namespace Shiny.Locations
{
    static class PlatformExtensions
    {
        public static LocationRequest ToNative(this GpsRequest request)
        {
            var nativeRequest = LocationRequest
                .Create()
                .SetInterval(request.Interval.ToMillis());

            switch (request.Priority)
            {
                case GpsPriority.Low:
                    nativeRequest.SetPriority(LocationRequest.PriorityLowPower);
                    break;

                case GpsPriority.Highest:
                    nativeRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
                    break;

                case GpsPriority.Normal:
                default:
                    nativeRequest.SetPriority(LocationRequest.PriorityBalancedPowerAccuracy);
                    break;
            }

            if (request.ThrottledInterval != null)
                nativeRequest.SetFastestInterval(request.ThrottledInterval.Value.ToMillis());

            if (request.MinimumDistance != null)
                nativeRequest.SetSmallestDisplacement((float)request.MinimumDistance.TotalMeters);

            return nativeRequest;
        }


        internal static long ToMillis(this TimeSpan ts)
            => Convert.ToInt64(ts.TotalMilliseconds);


        internal static long ToMillis(this TimeSpan? ts, long defaultValue)
            => Convert.ToInt64(ts?.TotalMilliseconds ?? defaultValue);
    }
}
