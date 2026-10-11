using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Content;
using AndroidX.Core.App;
using Microsoft.Maui.ApplicationModel;
using MauiPermissionException = Microsoft.Maui.ApplicationModel.PermissionException;
using Shiny.Hosting;
using P = Android.Manifest.Permission;

namespace Shiny;


public abstract partial class ShinyPermission
{
    /// <summary>
    /// The Shiny Android platform - permission requests and their results go through it.
    /// </summary>
    protected internal static AndroidPlatform Platform => Host.GetService<AndroidPlatform>()
        ?? throw new InvalidOperationException("The Shiny host is not running - call UseShiny() on your MauiAppBuilder before checking Shiny permissions");


    /// <summary>
    /// The runtime permissions this check covers on the running OS version. Each must be declared in the manifest.
    /// </summary>
    protected virtual string[] AndroidPermissions => [];


    void EnsurePlatformDeclared()
    {
        var platform = Platform;
        foreach (var permission in this.AndroidPermissions)
        {
            if (!platform.IsInManifest(permission))
                throw new MauiPermissionException($"You need to declare {permission} in your AndroidManifest.xml");
        }
    }


    bool ShouldShowPlatformRationale()
    {
        var activity = Platform.CurrentActivity;
        return activity != null && this.AndroidPermissions.Any(x => ActivityCompat.ShouldShowRequestPermissionRationale(activity, x));
    }


    /// <summary>
    /// Every permission granted is <see cref="AccessState.Available"/>; any one denied is <see cref="AccessState.Denied"/>;
    /// otherwise one has not been asked for yet and the state is <see cref="AccessState.Unknown"/>.
    /// </summary>
    protected internal static AccessState GetPermissionStatus(params string[] permissions)
    {
        var platform = Platform;
        var states = permissions.Select(platform.GetCurrentPermissionStatus).ToList();
        if (states.All(x => x == AccessState.Available))
            return AccessState.Available;

        if (states.Any(x => x == AccessState.Denied))
            return AccessState.Denied;

        return AccessState.Unknown;
    }


    /// <summary>
    /// Requests every permission in one prompt - <see cref="AccessState.Available"/> only if all are granted.
    /// </summary>
    protected internal static async Task<AccessState> RequestPermissions(params string[] permissions)
    {
        if (permissions.Length == 0)
            return AccessState.Available;

        var result = await Platform.RequestPermissions(permissions).ConfigureAwait(false);
        return result.IsSuccess() ? AccessState.Available : AccessState.Denied;
    }


    /// <summary>
    /// Whether the Bluetooth adapter exists and is switched on.
    /// </summary>
    protected internal static AccessState GetAdapterState()
    {
        var manager = (BluetoothManager?)Platform.AppContext.GetSystemService(Context.BluetoothService);
        var adapter = manager?.Adapter;
        if (adapter == null)
            return AccessState.NotSupported;

        return adapter.IsEnabled ? AccessState.Available : AccessState.Disabled;
    }


    /// <summary>
    /// Whether the user has notifications switched on for the app - separate from <c>POST_NOTIFICATIONS</c>, and
    /// the only switch below Android 13.
    /// </summary>
    protected internal static bool AreNotificationsEnabled()
        => NotificationManagerCompat.From(Platform.AppContext)!.AreNotificationsEnabled();


    /// <summary>
    /// The foreground service permissions and the notification it posts. Notifications denied is
    /// <see cref="AccessState.Restricted"/> - the service still runs, its notification is just not shown.
    /// </summary>
    protected internal static string[] ForegroundServicePermissions
    {
        get
        {
            var list = new List<string>();
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
                list.Add(P.ForegroundService);

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
                list.Add(P.PostNotifications);

            return list.ToArray();
        }
    }


    protected internal static AccessState GetForegroundServiceStatus()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && Platform.GetCurrentPermissionStatus(P.ForegroundService) != AccessState.Available)
            return AccessState.NotSetup;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var notifications = Platform.GetCurrentPermissionStatus(P.PostNotifications);
            return notifications == AccessState.Denied ? AccessState.Restricted : notifications;
        }
        return AccessState.Available;
    }


    /// <summary>
    /// Coarse + fine location (plus any extras in the same prompt), then background location on its own - Android 11+
    /// refuses background location in the same request as the others. Approximate only is
    /// <see cref="AccessState.Restricted"/>.
    /// </summary>
    protected internal static async Task<AccessState> RequestLocation(bool background, params string[] extras)
    {
        var platform = Platform;
        var foreground = new List<string> { P.AccessCoarseLocation, P.AccessFineLocation };
        foreground.AddRange(extras);

        var result = await platform.RequestPermissions(foreground.ToArray()).ConfigureAwait(false);
        if (!result.IsGranted(P.AccessCoarseLocation))
            return AccessState.Denied;

        var status = result.IsGranted(P.AccessFineLocation) && extras.All(result.IsGranted)
            ? AccessState.Available
            : AccessState.Restricted;

        if (background && OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var bg = await platform.RequestAccess(P.AccessBackgroundLocation).ConfigureAwait(false);
            if (bg != AccessState.Available)
                return AccessState.Denied;
        }
        return status;
    }


    protected internal static AccessState GetLocationStatus(bool background, params string[] extras)
    {
        var platform = Platform;
        var coarse = platform.GetCurrentPermissionStatus(P.AccessCoarseLocation);
        if (coarse != AccessState.Available)
            return coarse;

        var status = platform.GetCurrentPermissionStatus(P.AccessFineLocation) == AccessState.Available &&
                     extras.All(x => platform.GetCurrentPermissionStatus(x) == AccessState.Available)
            ? AccessState.Available
            : AccessState.Restricted;

        if (background && OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            var bg = platform.GetCurrentPermissionStatus(P.AccessBackgroundLocation);
            if (bg != AccessState.Available)
                return bg;
        }
        return status;
    }


    protected internal static string[] LocationPermissions(bool background, params string[] extras)
    {
        var list = new List<string> { P.AccessCoarseLocation, P.AccessFineLocation };
        list.AddRange(extras);
        if (background && OperatingSystem.IsAndroidVersionAtLeast(29))
            list.Add(P.AccessBackgroundLocation);

        return list.ToArray();
    }
}
