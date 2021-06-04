using System;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using P = Android.Manifest.Permission;


namespace Shiny.Locations
{
    public static class PlatformExtensions
    {
        public static AccessState GetLocationManagerStatus(this IAndroidContext context, bool gpsRequired, bool networkRequired)
        {
            var lm = context.GetSystemService<LocationManager>(Context.LocationService);

            if (context.IsMinApiLevel(28) && !lm.IsLocationEnabled)
                return AccessState.Disabled;

            if (networkRequired && !lm.IsProviderEnabled(LocationManager.NetworkProvider))
                return AccessState.Disabled;

            if (gpsRequired && !lm.IsProviderEnabled(LocationManager.GpsProvider))
                return AccessState.Disabled;

            return AccessState.Available;
        }


        public static AccessState GetCurrentLocationAccess(this IAndroidContext context, bool background, bool fineAccess, bool gpsRequired, bool networkRequired)
        {
            var status = context.GetLocationManagerStatus(gpsRequired, networkRequired);
            if (status != AccessState.Available)
                return status;

            if (context.IsMinApiLevel(29) && background)
            {
                status = context.GetCurrentAccessState(P.AccessBackgroundLocation);
                if (status != AccessState.Available)
                    return status;
            }
            var next = fineAccess ? P.AccessFineLocation : P.AccessCoarseLocation;
            status = context.GetCurrentAccessState(next);

            return status;
        }


        public static async Task<AccessState> RequestLocationAccess(this IAndroidContext context, bool background, bool fineAccess, bool gpsRequired, bool networkRequired)
        {
            var status = context.GetLocationManagerStatus(gpsRequired, networkRequired);
            if (status != AccessState.Available)
                return status;

            var locationPerm = fineAccess ? P.AccessFineLocation : P.AccessCoarseLocation;
            if (!context.IsMinApiLevel(29) || !background)
                return await context.RequestAccess(locationPerm).ToTask();

            var access = await context
                .RequestPermissions
                (
                    P.AccessBackgroundLocation,
                    P.ForegroundService,
                    locationPerm
                )
                .ToTask();

            if (!access.IsGranted(locationPerm))
                return AccessState.Denied;

            if (!access.IsGranted(P.AccessBackgroundLocation))
                return AccessState.Restricted;

            return AccessState.Available;
        }
    }
}
