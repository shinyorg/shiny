using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;


namespace Shiny.Locations
{
    public static class LocationExtensions
    {
        public static IObservable<AccessState> WhenAccessStatusChanged(this CLLocationManager locationManager, bool background)
                    => ((ShinyLocationDelegate)locationManager.Delegate).WhenAccessStatusChanged(background);


        public static AccessState FromNative(this CLAuthorizationStatus status, bool background)
        {
            switch (status)
            {
                case CLAuthorizationStatus.Restricted:
                    return AccessState.Restricted;

                case CLAuthorizationStatus.Denied:
                    return AccessState.Denied;

                case CLAuthorizationStatus.AuthorizedWhenInUse:
                    return background ? AccessState.Restricted : AccessState.Available;

                case CLAuthorizationStatus.AuthorizedAlways:
                    return AccessState.Available;

                case CLAuthorizationStatus.NotDetermined:
                default:
                    return AccessState.Unknown;
            }
        }


        public static AccessState GetCurrentStatus(this CLLocationManager locationManager, bool background)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            return CLLocationManager.Status.FromNative(background);
        }


        public static AccessState GetCurrentStatus<T>(this CLLocationManager locationManager, bool background) where T : CLRegion
        {
#if __IOS__
            if (!CLLocationManager.IsMonitoringAvailable(typeof(T)))
                return AccessState.NotSupported;
#endif

            return locationManager.GetCurrentStatus(background);
        }


        public static async Task<AccessState> RequestAccess(this CLLocationManager locationManager, bool background)
        {
            var status = locationManager.GetCurrentStatus(background);
            if (status == AccessState.Available)
                return status;

            var task = locationManager
                .WhenAccessStatusChanged(background)
                .StartWith()
                .Take(1)
                .ToTask();
#if __IOS__
            if (background)
                locationManager.RequestAlwaysAuthorization();
            else
                locationManager.RequestWhenInUseAuthorization();
#elif !__TVOS__
            locationManager.RequestAlwaysAuthorization();
#endif

            status = await task;
            return status;
        }
    }
}
