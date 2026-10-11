#if APPLE
using System;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;
using Microsoft.Maui.ApplicationModel;
#if MACOS
using AppKit;
#else
using UIKit;
#endif

namespace Shiny;


/// <summary>
/// The CoreLocation side of the location-based <see cref="ShinyPermissions"/> checks.
/// </summary>
static class AppleLocationPermission
{
    public const string WhenInUseKey = "NSLocationWhenInUseUsageDescription";
    public const string AlwaysKey = "NSLocationAlwaysAndWhenInUseUsageDescription";
    public const string MacOSKey = "NSLocationUsageDescription";


    public static void EnsureInfoPlist(bool background)
    {
#if MACOS
        ShinyPermission.RequireInfoPlistKey(MacOSKey, WhenInUseKey);
#else
        ShinyPermission.RequireInfoPlistKey(WhenInUseKey);
        if (background)
            ShinyPermission.RequireInfoPlistKey(AlwaysKey);
#endif
    }


    public static Task<PermissionStatus> GetStatus(bool background) => ShinyPermission.OnMainThread(() =>
    {
        if (!CLLocationManager.LocationServicesEnabled)
            return PermissionStatus.Disabled;

        using var manager = new CLLocationManager();
        return ToStatus(manager, background);
    });


    public static Task<PermissionStatus> Request(bool background) => ShinyPermission.OnMainThread(async () =>
    {
        if (!CLLocationManager.LocationServicesEnabled)
            return PermissionStatus.Disabled;

        var del = new AuthorizationDelegate();
        var manager = new CLLocationManager { Delegate = del };
        try
        {
            if (manager.AuthorizationStatus == CLAuthorizationStatus.NotDetermined)
            {
#if MACOS
                // macOS has no separate "always" step - asking for it up front is the only prompt there is
                Action ask = background ? manager.RequestAlwaysAuthorization : manager.RequestWhenInUseAuthorization;
#else
                Action ask = manager.RequestWhenInUseAuthorization;
#endif
                await del.WaitFor(manager, ask, x => x != CLAuthorizationStatus.NotDetermined);
            }

#if !MACOS
            if (background && manager.AuthorizationStatus == CLAuthorizationStatus.AuthorizedWhenInUse)
                await WaitForAlwaysUpgrade(manager, del);
#endif
            return ToStatus(manager, background);
        }
        finally
        {
            manager.Delegate = null!;
            manager.Dispose();
        }
    });


    static PermissionStatus ToStatus(CLLocationManager manager, bool background)
    {
#pragma warning disable CA1422 // macOS reports when-in-use as "authorized" - iOS still needs the distinction
        var status = manager.AuthorizationStatus switch
        {
            CLAuthorizationStatus.AuthorizedAlways => PermissionStatus.Granted,
            CLAuthorizationStatus.AuthorizedWhenInUse => background ? PermissionStatus.Limited : PermissionStatus.Granted,
            CLAuthorizationStatus.Denied => PermissionStatus.Denied,
            CLAuthorizationStatus.Restricted => PermissionStatus.Restricted,
            _ => PermissionStatus.Unknown
        };
#pragma warning restore CA1422

        // approximate location is a partial grant
        if (status == PermissionStatus.Granted && manager.AccuracyAuthorization == CLAccuracyAuthorization.ReducedAccuracy)
            status = PermissionStatus.Limited;

        return status;
    }


#if !MACOS
    /// <summary>
    /// iOS only shows the when-in-use to always upgrade prompt once, and says nothing when it decides not to - so
    /// this waits for an authorization change, or for the app to come back from a prompt, or briefly for a prompt
    /// that never appears.
    /// </summary>
    static async Task WaitForAlwaysUpgrade(CLLocationManager manager, AuthorizationDelegate del)
    {
        var resigned = false;
        var promptDone = new TaskCompletionSource();
        var center = NSNotificationCenter.DefaultCenter;
        var resignObserver = center.AddObserver(UIApplication.WillResignActiveNotification, _ => resigned = true);
        var activeObserver = center.AddObserver(UIApplication.DidBecomeActiveNotification, _ =>
        {
            if (resigned)
                promptDone.TrySetResult();
        });

        try
        {
            var changed = del.WaitFor(manager, manager.RequestAlwaysAuthorization, x => x != CLAuthorizationStatus.AuthorizedWhenInUse);
            var noPrompt = Task.Delay(TimeSpan.FromSeconds(3));

            var first = await Task.WhenAny(changed, promptDone.Task, noPrompt);
            if (first == noPrompt && resigned)
                await Task.WhenAny(changed, promptDone.Task);
        }
        finally
        {
            center.RemoveObserver(resignObserver);
            center.RemoveObserver(activeObserver);
        }
    }
#endif


    sealed class AuthorizationDelegate : CLLocationManagerDelegate
    {
        Func<CLAuthorizationStatus, bool>? predicate;
        TaskCompletionSource? tcs;


        public Task WaitFor(CLLocationManager manager, Action request, Func<CLAuthorizationStatus, bool> until)
        {
            this.predicate = until;
            this.tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            request();
            return this.tcs.Task;
        }


        public override void DidChangeAuthorization(CLLocationManager manager)
        {
            if (this.predicate?.Invoke(manager.AuthorizationStatus) ?? false)
                this.tcs?.TrySetResult();
        }
    }
}
#endif
