using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;


namespace Shiny.Locations
{
    public static class iOSExtensions
    {
        public static IObservable<AccessState> WhenAccessStatusChanged(this CLLocationManager locationManager, bool background)
            => ((ShinyLocationDelegate)locationManager.Delegate).WhenAccessStatusChanged(background);


        public static AccessState GetCurrentStatus(this CLLocationManager locationManager, bool background)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            return CLLocationManager.Status.FromNative(true);
        }


        public static AccessState GetCurrentStatus<T>(this CLLocationManager locationManager, bool background) where T : CLRegion
        {
            if (!CLLocationManager.IsMonitoringAvailable(typeof(T)))
                return AccessState.NotSupported;

            return locationManager.GetCurrentStatus(background);
        }


        public static async Task<AccessState> RequestAccess(this CLLocationManager locationManager, bool background)
        {
            var status = locationManager.GetCurrentStatus(background);
            if (status != AccessState.Unknown)
                return status;

            status = await locationManager
                .WhenAccessStatusChanged(background)
                .Take(1)
                .ToTask();

            return status;
        }
    }
}
