using CoreLocation;

namespace Shiny.Locations;


class GeofenceManagerDelegate(CLLocationGeofenceManager geofenceManager) : ShinyLocationDelegate
{
    public override void RegionEntered(CLLocationManager manager, CLRegion region)
        => geofenceManager.OnRegionChanged(region, true);

    public override void RegionLeft(CLLocationManager manager, CLRegion region)
        => geofenceManager.OnRegionChanged(region, false);

    public override void DidDetermineState(CLLocationManager manager, CLRegionState state, CLRegion region)
        => geofenceManager.OnStateDetermined(state, region);
}
