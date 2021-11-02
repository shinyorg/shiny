using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using P = Android.Manifest.Permission;


namespace Shiny.Locations
{
    public static class PlatformExtensions
    {
        public static bool IsLocationEnabled(this IAndroidContext context, bool gpsRequired, bool networkRequired)
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


        public static async Task<AccessState> RequestLocationAccess(this IAndroidContext context, LocationPermissionType locType)
        {
            if (!context.IsLocationEnabled(false, false))
                return AccessState.Disabled;

            var perms = new List<string> { P.AccessCoarseLocation };
            if (locType != LocationPermissionType.Coarse)
                perms.Add(P.AccessFineLocation);

            var results = await context.RequestPermissions(perms.ToArray()).ToTask();
            var status = FromResult(results, locType);
            return status;
        }


        public static async Task<AccessState> RequestBackgroundLocationAccess(this IAndroidContext context, LocationPermissionType locType)
        {
            if (!context.IsLocationEnabled(false, false))
                return AccessState.Disabled;

            var status = AccessState.Unknown;

            if (context.IsMinApiLevel(30))
            {
                // Android 11+ need to request background separately
                // Android 12+ user can decline fine, but allow coarse
                status = await context.RequestLocationAccess(locType);
                if (status == AccessState.Available || status == AccessState.Restricted)
                {
                    var bg = await context.RequestAccess(P.AccessBackgroundLocation).ToTask();
                    status = bg == AccessState.Available ? status : AccessState.Restricted;
                }
            }
            else if (context.IsMinApiLevel(29))
            {
                // Android 10: Request BG permission with other permissions
                var perms = new List<string> { P.AccessBackgroundLocation, P.AccessCoarseLocation };
                if (locType != LocationPermissionType.Coarse)
                    perms.Add(P.AccessFineLocation);

                var results = await context.RequestPermissions(perms.ToArray()).ToTask();
                status = FromResult(results, locType);
                if (status == AccessState.Available || status == AccessState.Available)
                    status = results.IsGranted(P.AccessBackgroundLocation) ? status : AccessState.Restricted;
            }
            else
            {
                status = await context.RequestLocationAccess(locType);
            }
            return status;
        }


        static AccessState FromResult(PermissionRequestResult results, LocationPermissionType locType)
        {
            AccessState status;
            var coarse = results.IsGranted(P.AccessCoarseLocation);

            if (locType != LocationPermissionType.Coarse)
            {
                var fine = results.IsGranted(P.AccessFineLocation);
                if (!coarse && !fine)
                    status = AccessState.Denied;

                else if (fine)
                    status = AccessState.Available;

                else if (locType == LocationPermissionType.FineRequired)
                    status = AccessState.Denied;

                else
                    status = AccessState.Restricted;
            }
            else
            {
                status = coarse ? AccessState.Available : AccessState.Denied;
            }
            return status;
        }
    }
}

//public static async Task<AccessState> RequestRealtimeLocationAccess(this IAndroidContext context)
//{
//    if (!context.IsLocationEnabled(false, false))
//        return AccessState.Disabled;

//    await context.RequestLocationAccess(LocationPermissionType.Fine).ToTask();
//    var access = await context
//        .RequestPermissions
//        (
//            P.ForegroundService,
//            P.AccessFineLocation,
//            P.AccessCoarseLocation
//        )
//        .ToTask();

//    var fine = access.IsGranted(P.AccessFineLocation);
//    var coarse = access.IsGranted(P.AccessCoarseLocation);

//    if (!fine && !coarse)
//        return AccessState.Denied;

//    if (!fine)
//        return AccessState.Restricted;

//    return AccessState.Available;
//}