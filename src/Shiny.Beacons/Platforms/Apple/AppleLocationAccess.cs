using System;
using System.Threading.Tasks;
using CoreLocation;

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
/// Deliberately a local copy of what Shiny.Locations does rather than a reference to it - beacons
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
        var status = manager.GetCurrentStatus(background);
        if (status != AccessState.Unknown && !(background && status == AccessState.Restricted))
            return status;

        if (manager.Delegate is not ShinyBeaconLocationDelegate locationDelegate)
            throw new InvalidOperationException("The location manager's delegate must derive from ShinyBeaconLocationDelegate");

        // iOS insists on when-in-use being granted before always can even be asked for
        if (manager.AuthorizationStatus == CLAuthorizationStatus.NotDetermined)
            status = await Wait(locationDelegate, false, manager.RequestWhenInUseAuthorization).ConfigureAwait(false);

        if (background && status is AccessState.Available or AccessState.Restricted)
            status = await Wait(locationDelegate, true, manager.RequestAlwaysAuthorization).ConfigureAwait(false);

        return status;
    }


    static Task<AccessState> Wait(ShinyBeaconLocationDelegate locationDelegate, bool background, Action request)
    {
        var tcs = new TaskCompletionSource<AccessState>();

        EventHandler<CLAuthorizationStatus>? handler = null;
        handler = (_, status) =>
        {
            if (status == CLAuthorizationStatus.NotDetermined)
                return;

            locationDelegate.AuthorizationStatusChanged -= handler;
            tcs.TrySetResult(status.FromNative(background));
        };

        locationDelegate.AuthorizationStatusChanged += handler;
        request();

        return tcs.Task;
    }
}
