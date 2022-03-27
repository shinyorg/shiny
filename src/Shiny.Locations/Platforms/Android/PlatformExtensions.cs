using System;
using Android.Gms.Location;


namespace Shiny.Locations
{
    static class PlatformExtensions
    {
        public static LocationRequest ToNative(this GpsRequest request)
        {
            var nativeRequest = LocationRequest
                .Create();
                //.SetInterval(request.Interval.ToMillis());

            switch (request.Priority)
            {
                case GpsPriority.Low:
                    nativeRequest.SetPriority(LocationRequest.PriorityLowPower);
                    break;

                case GpsPriority.Highest:
                    nativeRequest
                        .SetPriority(LocationRequest.PriorityHighAccuracy);
                    break;

                case GpsPriority.Normal:
                default:
                    nativeRequest.SetPriority(LocationRequest.PriorityBalancedPowerAccuracy);
                    break;
            }

            //if (request.ThrottledInterval != null)
            //    nativeRequest.SetFastestInterval(request.ThrottledInterval.Value.ToMillis());

            //if (request.MinimumDistance != null)
            //    nativeRequest.SetSmallestDisplacement((float)request.MinimumDistance.TotalMeters);

            return nativeRequest;
        }


                //// TODO: other accuracy values for iOS
                //case GpsPriority.Highest:
                //    this.locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
                //    break;

                //case GpsPriority.Normal:
                //    //CLActivityType.Airborne
                //    //CLActivityType.AutomotiveNavigation
                //    //CLActivityType.Fitness
                //    //CLActivityType.OtherNavigation

                //    //CLLocation.AccurracyBestForNavigation
                //    //CLLocation.AccuracyHundredMeters;
                //    //CLLocation.AccuracyKilometer
                //    //CLLocation.AccuracyThreeKilometers
                //    this.locationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
                //    break;

                //case GpsPriority.Low:
                //    this.locationManager.DesiredAccuracy = CLLocation.AccuracyHundredMeters;
                //    break;

        internal static long ToMillis(this TimeSpan ts)
            => Convert.ToInt64(ts.TotalMilliseconds);


        internal static long ToMillis(this TimeSpan? ts, long defaultValue)
            => Convert.ToInt64(ts?.TotalMilliseconds ?? defaultValue);
    }
}
