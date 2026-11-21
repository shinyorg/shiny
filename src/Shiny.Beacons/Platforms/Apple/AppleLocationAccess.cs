using System;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Foundation;

namespace Shiny.Beacons;


/// <summary>
/// A CoreLocation delegate base that surfaces authorization changes.
/// </summary>
/// <remarks>
/// CoreLocation reports permission results through the delegate rather than a callback on the
/// request, so a delegate is the only place a request can be awaited from.
/// </remarks>
public abstract class ShinyBeaconLocationDelegate : CLLocationManagerDelegate
{
    /// <summary>Raised when the user answers a permission prompt, or changes it in Settings.</summary>
    public event EventHandler<CLAuthorizationStatus>? AuthorizationStatusChanged;

    /// <inheritdoc />
    public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        => this.AuthorizationStatusChanged?.Invoke(this, status);
}


/// <summary>
/// Location permission plumbing for the beacon managers.
/// </summary>
/// <remarks>
/// Deliberately a local copy of what Shiny.Geofencing does rather than a reference to it - beacons
/// would otherwise drag Google Play Services and a stack of AndroidX packages into every app that
/// only wanted to read a beacon.
/// </remarks>
public static class AppleLocationAccess
{
    /// <summary>
    /// Maps CoreLocation's authorization status onto <see cref="AccessState"/>.
    /// </summary>
    /// <param name="status">CoreLocation's status.</param>
    /// <param name="background">
    /// True when always-on authorization is required. When-in-use is reported as
    /// <see cref="AccessState.Restricted"/> in that case - the app has permission, just not enough.
    /// </param>
    public static AccessState FromNative(this CLAuthorizationStatus status, bool background) => status switch
    {
        CLAuthorizationStatus.Denied => AccessState.Denied,
        CLAuthorizationStatus.Restricted => AccessState.Restricted,
        CLAuthorizationStatus.AuthorizedWhenInUse => background ? AccessState.Restricted : AccessState.Available,
        CLAuthorizationStatus.AuthorizedAlways => AccessState.Available,
        _ => AccessState.Unknown
    };


    /// <summary>
    /// The current authorization state without prompting.
    /// </summary>
    /// <param name="manager">The location manager.</param>
    /// <param name="background">Whether always-on authorization is required.</param>
    public static AccessState GetCurrentStatus(this CLLocationManager manager, bool background)
        => CLLocationManager.LocationServicesEnabled
            ? manager.AuthorizationStatus.FromNative(background)
            : AccessState.Disabled;


    /// <summary>
    /// Prompts for location permission if it has not been decided yet.
    /// </summary>
    /// <param name="manager">The location manager. Its delegate must be a <see cref="ShinyBeaconLocationDelegate"/>.</param>
    /// <param name="background">
    /// True to escalate to always-on authorization, which background region monitoring requires.
    /// </param>
    public static async Task<AccessState> RequestAccess(this CLLocationManager manager, bool background)
    {
        if (!CLLocationManager.LocationServicesEnabled)
            return AccessState.Disabled;

        if (manager.Delegate is not ShinyBeaconLocationDelegate locationDelegate)
            throw new InvalidOperationException("The location manager's delegate must derive from ShinyBeaconLocationDelegate");

        // iOS insists on when-in-use being granted before always can even be asked for
        if (manager.AuthorizationStatus == CLAuthorizationStatus.NotDetermined)
        {
            // undecided again (a fresh install, or reset in Settings), so the upgrade prompt is back too
            NSUserDefaults.StandardUserDefaults.RemoveObject(UpgradeRequestedKey);
            await Wait(locationDelegate, CLAuthorizationStatus.NotDetermined, manager.RequestWhenInUseAuthorization).ConfigureAwait(false);
        }

#if !MACOS
        if (background && manager.AuthorizationStatus == CLAuthorizationStatus.AuthorizedWhenInUse)
            await RequestAlways(manager, locationDelegate).ConfigureAwait(false);
#endif

        return manager.GetCurrentStatus(background);
    }


    const string UpgradeRequestedKey = "shiny.location.alwaysUpgradeRequested";

#if !MACOS
    static readonly TimeSpan UpgradeAnswerTimeout = TimeSpan.FromSeconds(30);

    // iOS shows the upgrade to always once per install, and the app cannot see it: it stays active while
    // the prompt is up, "keep only while using" changes nothing so no callback comes, and asked again iOS
    // stays silent. So the first request waits (bounded) for the answer and later ones don't wait at all.
    // The key is shared by every Shiny location package, because the prompt is per app.
    static async Task RequestAlways(CLLocationManager manager, ShinyBeaconLocationDelegate locationDelegate)
    {
        var defaults = NSUserDefaults.StandardUserDefaults;
        var asked = defaults.BoolForKey(UpgradeRequestedKey);
        defaults.SetBool(true, UpgradeRequestedKey);

        using var cts = new CancellationTokenSource();
        var answered = Wait(locationDelegate, CLAuthorizationStatus.AuthorizedWhenInUse, manager.RequestAlwaysAuthorization, cts.Token);
        if (!asked)
            await Task.WhenAny(answered, Task.Delay(UpgradeAnswerTimeout, cts.Token)).ConfigureAwait(false);

        cts.Cancel();
    }
#endif


    static Task Wait(ShinyBeaconLocationDelegate locationDelegate, CLAuthorizationStatus from, Action request, CancellationToken cancelToken = default)
    {
        var tcs = new TaskCompletionSource();

        EventHandler<CLAuthorizationStatus>? handler = null;
        handler = (_, status) =>
        {
            // CoreLocation also reports the unchanged status when a manager is created
            if (status == CLAuthorizationStatus.NotDetermined || status == from)
                return;

            locationDelegate.AuthorizationStatusChanged -= handler;
            tcs.TrySetResult();
        };

        locationDelegate.AuthorizationStatusChanged += handler;
        cancelToken.Register(() =>
        {
            locationDelegate.AuthorizationStatusChanged -= handler;
            tcs.TrySetCanceled();
        });
        request();

        return tcs.Task;
    }
}
