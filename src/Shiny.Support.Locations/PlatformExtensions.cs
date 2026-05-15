#if APPLE
using System;
using System.Threading.Tasks;
using CoreLocation;

namespace Shiny.Locations;


public static class LocationExtensions
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
        var status = locationManager.GetCurrentStatus(background);
        if (status != AccessState.Unknown)
            return status;

        locationManager.Delegate ??= new ShinyLocationDelegate();
        if (locationManager.Delegate is not ShinyLocationDelegate shinyDelegate)
            throw new NotSupportedException("You cannot call this method with non-ShinyLocationDelegate");

        // locationManager.AccuracyAuthorization
        // locationManager.RequestTemporaryFullAccuracyAuthorizationAsync()
        status = await WaitForAuthorization(shinyDelegate, false, locationManager.RequestWhenInUseAuthorization).ConfigureAwait(false);

        if (status == AccessState.Available && background)
            status = await WaitForAuthorization(shinyDelegate, true, locationManager.RequestAlwaysAuthorization).ConfigureAwait(false);

        return status;
    }


    static Task<AccessState> WaitForAuthorization(ShinyLocationDelegate shinyDelegate, bool background, Action requestAction)
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
#endif
