#if APPLE
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;

namespace Shiny.Locations;


public static class LocationExtensions
{
    public static IObservable<AccessState> WhenAccessStatusChanged(this CLLocationManager locationManager, bool background)
        => ((ShinyLocationDelegate)locationManager.Delegate).WhenAccessStatusChanged(background);


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


    public static AccessState GetCurrentStatus<T>(this CLLocationManager locationManager, bool background) where T : CLRegion
    {
#if IOS
        if (!CLLocationManager.IsMonitoringAvailable(typeof(T)))
            return AccessState.NotSupported;
#endif

        return locationManager.GetCurrentStatus(background);
    }


    public static async Task<AccessState> RequestAccess(this CLLocationManager locationManager, bool background)
    {
        var status = await RequestTheAccess(locationManager, false);
        if (status == AccessState.Available && background)
            status = await RequestTheAccess(locationManager, true);

        return status;
    }


    static async Task<AccessState> RequestTheAccess(CLLocationManager locationManager, bool background)
    {
        var successState = background
            ? CLAuthorizationStatus.AuthorizedAlways
            : CLAuthorizationStatus.AuthorizedWhenInUse;

        //var status = locationManager.GetCurrentStatus(background);
        //if (status != AccessState.Unknown)
        //    return status;

        var task = locationManager
            .WhenAccessStatusChanged(background)
            .StartWith()
            .Take(1)
            .ToTask();

        if (background)
            locationManager.RequestAlwaysAuthorization();
        else
            locationManager.RequestWhenInUseAuthorization();

        var status = await task.ConfigureAwait(false);
        return status;
    }
}
#endif