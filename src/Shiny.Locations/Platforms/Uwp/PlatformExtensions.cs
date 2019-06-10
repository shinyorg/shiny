using System;

namespace Shiny.Locations
{
    static class PlatformExtensions
    {
        internal static GeofenceState FromNative(this Windows.Devices.Geolocation.Geofencing.GeofenceState state)
        {
            switch (state)
            {
                case Windows.Devices.Geolocation.Geofencing.GeofenceState.Entered:
                    return GeofenceState.Entered;

                case Windows.Devices.Geolocation.Geofencing.GeofenceState.Exited:
                    return GeofenceState.Exited;

                default:
                    return GeofenceState.Unknown;
            }
        }
    }
}
