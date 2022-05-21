using CoreLocation;
using Foundation;

namespace Shiny.Locations;


internal class GpsManagerDelegate : ShinyLocationDelegate
{
    readonly GpsManager manager;
    public GpsManagerDelegate(GpsManager manager) => this.manager = manager;

    public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
        => this.manager.LocationsUpdated(locations);

    public override void Failed(CLLocationManager manager, NSError error)
        => this.manager.OnFailed(error);
}
