using CoreLocation;

namespace Shiny.Locations;


internal class GeofenceManagerDelegate : ShinyLocationDelegate
{
    readonly GeofenceManager manager;
    public GeofenceManagerDelegate(GeofenceManager geofenceImpl) => this.manager = geofenceImpl;


    public override void RegionEntered(CLLocationManager manager, CLRegion region)
        => this.manager.OnRegionChanged(region, true);

    public override void RegionLeft(CLLocationManager manager, CLRegion region)
        => this.manager.OnRegionChanged(region, false);

    public override void DidDetermineState(CLLocationManager manager, CLRegionState state, CLRegion region)
        => this.manager.OnStateDetermined(state, region);
}
