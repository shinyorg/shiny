using System;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using P = Android.Manifest.Permission;


namespace Shiny.Locations
{
    static class PlatformExtensions
    {
        public const string ACCESS_BACKGROUND_LOCATION = "android.permission.ACCESS_BACKGROUND_LOCATION";


        internal static AccessState GetLocationManagerStatus(this AndroidContext context, bool gpsRequired)
        {
            //var hasGps = locationManager.IsProviderEnabled(LocationManager.GpsProvider);
            var lm = context.GetSystemService<LocationManager>(Context.LocationService);
            if (!lm.IsLocationEnabled)
                return AccessState.Disabled;

            if (gpsRequired && !lm.IsProviderEnabled(LocationManager.GpsProvider))
                return AccessState.Disabled;

            return AccessState.Available;
        }


        internal static AccessState GetCurrentLocationAccess(this AndroidContext context, bool background, bool fineAccess)
        {
            var status = context.GetLocationManagerStatus(false);
            if (status != AccessState.Available)
                return status;

            if (context.IsMinApiLevel(29) && background)
            {
                status = context.GetCurrentAccessState(ACCESS_BACKGROUND_LOCATION);
                if (status != AccessState.Available)
                    return status;
            }
            var next = fineAccess ? P.AccessFineLocation : P.AccessCoarseLocation;
            status = context.GetCurrentAccessState(next);

            return status;
        }


        internal static async Task<AccessState> RequestLocationAccess(this AndroidContext context, bool background, bool fineAccess)
        {
            var status = context.GetLocationManagerStatus(false);
            if (status != AccessState.Available)
                return status;

            var locationPerm = fineAccess ? P.AccessFineLocation : P.AccessCoarseLocation;
            if (context.IsMinApiLevel(29) && background)
            {
                return await context
                    .RequestAccess
                    (
                        ACCESS_BACKGROUND_LOCATION,
                        locationPerm
                    )
                    .ToTask();
            }

            return await context.RequestAccess(locationPerm).ToTask();
        }


        internal static long ToMillis(this TimeSpan ts)
            => Convert.ToInt64(ts.TotalMilliseconds);


        internal static long ToMillis(this TimeSpan? ts, long defaultValue)
            => Convert.ToInt64(ts?.TotalMilliseconds ?? defaultValue);
    }
}
