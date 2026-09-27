using CoreLocation;

namespace Shiny.Locations;


static class GeofencePlatformExtensions
{
    public static GeofenceState FromNative(this CLRegionState state) => state switch
    {
         CLRegionState.Inside => GeofenceState.Entered,
         CLRegionState.Outside => GeofenceState.Exited,
         _ => GeofenceState.Unknown
    };


    public static CLLocationCoordinate2D ToNative(this Position position)
        => new CLLocationCoordinate2D(position.Latitude, position.Longitude);


    public static CLCircularRegion ToNative(this GeofenceRegion region)
        => new CLCircularRegion
        (
            region.Center.ToNative(),
            region.Radius.TotalMeters,
            region.Identifier
        )
        {
            // dwell needs the entry to start its timer and the exit to cancel it
            NotifyOnEntry = region.NotifyOnEntry || region.DwellTime != null,
            NotifyOnExit = region.NotifyOnExit || region.DwellTime != null
        };
}
