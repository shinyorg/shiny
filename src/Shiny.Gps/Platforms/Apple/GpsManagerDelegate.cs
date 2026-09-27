using CoreLocation;
using Foundation;

namespace Shiny.Locations;


class GpsManagerDelegate(CLLocationGpsManager gpsManager) : ShinyLocationDelegate
{
    public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        => gpsManager.LocationsUpdated(locations);

    public override void Failed(CLLocationManager manager, NSError error)
        => gpsManager.OnFailed(error);
}
