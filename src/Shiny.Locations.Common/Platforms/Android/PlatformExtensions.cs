using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.Content;
using Android.Locations;
using P = Android.Manifest.Permission;

namespace Shiny.Locations;


public enum LocationPermissionType
{
    Coarse,
    Fine,
    FineRequired
}


public static class PlatformExtensions
{
    public static bool IsLocationEnabled(this AndroidPlatform platform, bool gpsRequired, bool networkRequired)
    {
        var lm = platform.GetSystemService<LocationManager>(Context.LocationService);

        if (platform.IsMinApiLevel(28) && !lm.IsLocationEnabled)
            return false;

        if (networkRequired && !lm.IsProviderEnabled(LocationManager.NetworkProvider))
            return false;

        if (gpsRequired && !lm.IsProviderEnabled(LocationManager.GpsProvider))
            return false;

        return true;
    }


    public static async Task<AccessState> RequestLocationAccess(this AndroidPlatform platform, LocationPermissionType locType)
    {
        if (!platform.IsLocationEnabled(false, false))
            return AccessState.Disabled;

        var perms = new List<string> { P.AccessCoarseLocation };
        if (locType != LocationPermissionType.Coarse)
            perms.Add(P.AccessFineLocation);

        var results = await platform.RequestPermissions(perms.ToArray()).ToTask();
        var status = FromResult(results, locType);
        return status;
    }


    public static async Task<AccessState> RequestBackgroundLocationAccess(this AndroidPlatform platform, LocationPermissionType locType)
    {
        if (!platform.IsLocationEnabled(false, false))
            return AccessState.Disabled;

        platform.AssertBackgroundInManifest();

        AccessState status;
        if (platform.IsMinApiLevel(30))
        {
            // Android 11+ need to request background separately
            // Android 12+ user can decline fine, but allow coarse
            status = await platform.RequestLocationAccess(locType);
            if (status == AccessState.Available || status == AccessState.Restricted)
            {
                var bg = await platform.RequestAccess(P.AccessBackgroundLocation).ToTask();
                status = bg == AccessState.Available ? status : AccessState.Restricted;
            }
        }
        else if (platform.IsMinApiLevel(29))
        {
            // Android 10: Request BG permission with other permissions
            var perms = new List<string> { P.AccessBackgroundLocation, P.AccessCoarseLocation };
            if (locType != LocationPermissionType.Coarse)
                perms.Add(P.AccessFineLocation);

            var results = await platform
                .RequestPermissions(perms.ToArray())
                .ToTask();

            status = FromResult(results, locType);
            if (status == AccessState.Available || status == AccessState.Available)
                status = results.IsGranted(P.AccessBackgroundLocation) ? status : AccessState.Restricted;
        }
        else
        {
            status = await platform.RequestLocationAccess(locType);
        }
        return status;
    }


    static void AssertBackgroundInManifest(this AndroidPlatform platform)
    {
        if (platform.IsMinApiLevel(29) && !platform.IsInManifest(P.AccessBackgroundLocation))
            throw new ArgumentException($"{P.AccessBackgroundLocation} is not in your manifest but is required");
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

//public static async Task<AccessState> RequestRealtimeLocationAccess(this IAndroidplatform platform)
//{
//    if (!platform.IsLocationEnabled(false, false))
//        return AccessState.Disabled;

//    await platform.RequestLocationAccess(LocationPermissionType.Fine).ToTask();
//    var access = await platform
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