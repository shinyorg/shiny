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
        if (locationManager.Delegate is not ShinyLocationDelegate)
            throw new NotSupportedException("You cannot call this method with non-ShinyLocationDleegate");
        
        var task = locationManager
            .WhenAccessStatusChanged(false)
            .Where(x => x != AccessState.Unknown)
            .Take(1)
            .ToTask();
        
        locationManager.RequestWhenInUseAuthorization();
        status = await task.ConfigureAwait(false);

        if (status == AccessState.Available && background)
        {
            task = locationManager
                .WhenAccessStatusChanged(true)
                // .StartWith()
                .Take(1)
                .ToTask();
            
            locationManager.RequestAlwaysAuthorization();
            status = await task.ConfigureAwait(false);
        }

        return status;
    }
}
#endif