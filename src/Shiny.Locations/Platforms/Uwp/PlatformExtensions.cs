using System;
using Windows.Devices.Geolocation.Geofencing;
using Native = Windows.Devices.Geolocation.Geofencing.GeofenceState;


namespace Shiny.Locations
{
    public static class PlatformExtensions
    {
        public static GeofenceState FromNative(this Native state) => state switch
        {
            Native.Entered => GeofenceState.Entered,
            Native.Exited => GeofenceState.Exited,
            _ => GeofenceState.Unknown
        };


        public static AccessState FromNative(this GeofenceMonitorStatus state) => GeofenceMonitor.Current.Status switch
        {
            GeofenceMonitorStatus.Ready => AccessState.Available,
            GeofenceMonitorStatus.Disabled => AccessState.Disabled,
            GeofenceMonitorStatus.NotAvailable => AccessState.NotSupported,
            _ => AccessState.Unknown
        };
    }
}
