using System;


namespace Shiny.Locations
{
    public interface IGeofenceDelegate
    {
        /// <summary>
        /// This is fired when the geofence region status has changed
        /// </summary>
        /// <param name="newStatus"></param>
        /// <param name="region"></param>
        void OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
    }
}
