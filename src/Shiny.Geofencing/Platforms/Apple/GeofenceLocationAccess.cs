using System;
using System.Threading.Tasks;
using CoreLocation;

namespace Shiny.Locations;


/// <summary>
/// CoreLocation's authorization delegate for the pre-iOS 18 geofence manager.
/// </summary>
/// <remarks>
/// Deliberately a local copy of Shiny.Gps' public ShinyLocationDelegate/AppleLocationExtensions rather than
/// a reference to it - Shiny.Geofencing does not depend on Shiny.Gps.
/// </remarks>
class GeofenceLocationDelegate : CLLocationManagerDelegate
{
    public event EventHandler<CLAuthorizationStatus>? AuthorizationStatusChanged;

    public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        => this.AuthorizationStatusChanged?.Invoke(this, status);
}


static class GeofenceLocationAccess
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


    public static async Task<AccessState> RequestAccess(this CLLocationManager locationManager, bool background)
    {
        var status = locationManager.GetCurrentStatus(background);
        if (status != AccessState.Unknown)
            return status;

        locationManager.Delegate ??= new GeofenceLocationDelegate();
        if (locationManager.Delegate is not GeofenceLocationDelegate shinyDelegate)
            throw new NotSupportedException("You cannot call this method with non-GeofenceLocationDelegate");

        status = await WaitForAuthorization(shinyDelegate, false, locationManager.RequestWhenInUseAuthorization).ConfigureAwait(false);

        if (status == AccessState.Available && background)
            status = await WaitForAuthorization(shinyDelegate, true, locationManager.RequestAlwaysAuthorization).ConfigureAwait(false);

        return status;
    }


    static Task<AccessState> WaitForAuthorization(GeofenceLocationDelegate shinyDelegate, bool background, Action requestAction)
    {
        var tcs = new TaskCompletionSource<AccessState>();

        EventHandler<CLAuthorizationStatus>? handler = null;
        handler = (_, authStatus) =>
        {
            if (authStatus == CLAuthorizationStatus.NotDetermined)
                return;

            shinyDelegate.AuthorizationStatusChanged -= handler;
            tcs.TrySetResult(authStatus.FromNative(background));
        };

        shinyDelegate.AuthorizationStatusChanged += handler;
        requestAction();

        return tcs.Task;
    }
}
