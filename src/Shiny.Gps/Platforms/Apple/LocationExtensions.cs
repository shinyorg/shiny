#if APPLE
using System;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;

namespace Shiny.Locations;


public static class AppleLocationExtensions
{
    public static AccessState FromNative(this CLAuthorizationStatus status, bool background) => status switch
    {
        CLAuthorizationStatus.Denied => AccessState.Denied,
        CLAuthorizationStatus.Restricted => AccessState.Restricted,
        CLAuthorizationStatus.AuthorizedWhenInUse => background ? AccessState.Restricted : AccessState.Available,
        CLAuthorizationStatus.AuthorizedAlways => AccessState.Available,
        CLAuthorizationStatus.NotDetermined => AccessState.Unknown,
        _ => AccessState.Unknown
    };


    public static AccessState GetCurrentStatus(this CLLocationManager locationManager, bool background)
    {
        if (!CLLocationManager.LocationServicesEnabled)
            return AccessState.Disabled;

        return locationManager.AuthorizationStatus.FromNative(background);
    }


    public static AccessState GetCurrentStatus(this CLLocationManager locationManager, bool background, bool precise)
    {
        var status = locationManager.GetCurrentStatus(background);
        if (
            status == AccessState.Available &&
            precise &&
            locationManager.AccuracyAuthorization == CLAccuracyAuthorization.ReducedAccuracy
        )
        {
            return AccessState.Restricted;
        }

        return status;
    }


    public static AccessState GetCurrentStatus<T>(this CLLocationManager locationManager, bool background) where T : CLRegion
    {
#if __IOS__
        if (!CLLocationManager.IsMonitoringAvailable(typeof(T)))
            return AccessState.NotSupported;
#endif

        return locationManager.GetCurrentStatus(background);
    }


    public static Task<AccessState> RequestAccess(bool background)
    {
        var lm = new CLLocationManager
        {
            Delegate = new ShinyLocationDelegate()
        };
        return lm.RequestAccess(background);
    }


    public static async Task<AccessState> RequestAccess(this CLLocationManager locationManager, bool background)
    {
        if (!CLLocationManager.LocationServicesEnabled)
            return AccessState.Disabled;

        locationManager.Delegate ??= new ShinyLocationDelegate();
        if (locationManager.Delegate is not ShinyLocationDelegate shinyDelegate)
            throw new NotSupportedException("You cannot call this method with non-ShinyLocationDelegate");

        // iOS insists on when-in-use being granted before always can even be asked for
        if (locationManager.AuthorizationStatus == CLAuthorizationStatus.NotDetermined)
        {
            // undecided again (a fresh install, or reset in Settings), so the upgrade prompt is back too
            NSUserDefaults.StandardUserDefaults.RemoveObject(UpgradeRequestedKey);
            await WaitForAuthorization(shinyDelegate, CLAuthorizationStatus.NotDetermined, locationManager.RequestWhenInUseAuthorization).ConfigureAwait(false);
        }

        if (background && locationManager.AuthorizationStatus == CLAuthorizationStatus.AuthorizedWhenInUse)
            await RequestAlways(locationManager, shinyDelegate).ConfigureAwait(false);

        return locationManager.GetCurrentStatus(background);
    }


    const string UpgradeRequestedKey = "shiny.location.alwaysUpgradeRequested";
    static readonly TimeSpan UpgradeAnswerTimeout = TimeSpan.FromSeconds(30);

    // iOS shows the upgrade to always once per install, and the app cannot see it: it stays active while
    // the prompt is up, "keep only while using" changes nothing so no callback comes, and asked again iOS
    // stays silent. So the first request waits (bounded) for the answer and later ones don't wait at all.
    // The key is shared by every Shiny location package, because the prompt is per app.
    static async Task RequestAlways(CLLocationManager locationManager, ShinyLocationDelegate shinyDelegate)
    {
        var defaults = NSUserDefaults.StandardUserDefaults;
        var asked = defaults.BoolForKey(UpgradeRequestedKey);
        defaults.SetBool(true, UpgradeRequestedKey);

        using var cts = new CancellationTokenSource();
        var answered = WaitForAuthorization(shinyDelegate, CLAuthorizationStatus.AuthorizedWhenInUse, locationManager.RequestAlwaysAuthorization, cts.Token);
        if (!asked)
            await Task.WhenAny(answered, Task.Delay(UpgradeAnswerTimeout, cts.Token)).ConfigureAwait(false);

        cts.Cancel();
    }


    static Task WaitForAuthorization(ShinyLocationDelegate shinyDelegate, CLAuthorizationStatus from, Action requestAction, CancellationToken cancelToken = default)
    {
        var tcs = new TaskCompletionSource();

        EventHandler<CLAuthorizationStatus>? handler = null;
        handler = (_, authStatus) =>
        {
            // CoreLocation also reports the unchanged status when a manager is created
            if (authStatus == CLAuthorizationStatus.NotDetermined || authStatus == from)
                return;

            shinyDelegate.AuthorizationStatusChanged -= handler;
            tcs.TrySetResult();
        };

        shinyDelegate.AuthorizationStatusChanged += handler;
        cancelToken.Register(() =>
        {
            shinyDelegate.AuthorizationStatusChanged -= handler;
            tcs.TrySetCanceled();
        });
        requestAction();

        return tcs.Task;
    }
}
#endif
