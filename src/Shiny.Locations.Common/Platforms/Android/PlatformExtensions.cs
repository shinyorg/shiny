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
        static bool IsLocationEnabled(IAndroidContext context, bool gpsRequired, bool networkRequired)
        {
            var lm = context.GetSystemService<LocationManager>(Context.LocationService);

            if (context.IsMinApiLevel(28) && !lm.IsLocationEnabled)
                return false;

            if (networkRequired && !lm.IsProviderEnabled(LocationManager.NetworkProvider))
                return false;

            if (gpsRequired && !lm.IsProviderEnabled(LocationManager.GpsProvider))
                return false;

            return true;
        }


        //static AccessState GetCurrentLocationAccess(this IAndroidContext context, bool background, bool fineAccess, bool gpsRequired, bool networkRequired)
        //{
        //    var status = context.GetLocationManagerStatus(gpsRequired, networkRequired);
        //    if (status != AccessState.Available)
        //        return status;

        //    if (context.IsMinApiLevel(29) && background)
        //    {
        //        status = context.GetCurrentAccessState(P.AccessBackgroundLocation);
        //        if (status != AccessState.Available)
        //            return status;
        //    }
        //    var next = fineAccess ? P.AccessFineLocation : P.AccessCoarseLocation;
        //    status = context.GetCurrentAccessState(next);

        //    return status;
        //}


        public static async Task<AccessState> RequestLocationAccess(this IAndroidContext context, bool background, bool gpsRequired, bool networkRequired)
        {
            if (!IsLocationEnabled(context, gpsRequired, networkRequired))
                return AccessState.Disabled;

            if (!context.IsMinApiLevel(29) || !background)
                return await context.RequestAccess(P.AccessFineLocation).ToTask();

            // android 11+ requires bg check & permissions are separate - Android 12 can also return coarse even though fine was requested, so you must ask for both
            var access = await context
                .RequestPermissions
                (
                    P.ForegroundService,
                    P.AccessFineLocation,
                    P.AccessCoarseLocation
                )
                .ToTask();

            var fine = access.IsGranted(P.AccessFineLocation);
            var coarse = access.IsGranted(P.AccessCoarseLocation);

            if (!fine && !coarse)
                return AccessState.Denied;

            access = await context
                .RequestPermissions(P.AccessBackgroundLocation)
                .ToTask();

            if (!access.IsGranted(P.AccessBackgroundLocation))
                return AccessState.Restricted;

            if (!fine)
                return AccessState.Restricted;

            return AccessState.Available;
        }


        static async Task<AccessState> RequestBackground(this IAndroidContext context, string permission)
        {
            var access = await context
                .RequestPermissions(permission)
                .ToTask();

            if (!access.IsGranted(permission))
                return AccessState.Denied;

            //if (!access.IsGranted(P.AccessBackgroundLocation))
            //    return AccessState.Restricted;

            return AccessState.Available;
        }
    }
}
