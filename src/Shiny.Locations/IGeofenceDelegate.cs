using System;


namespace Shiny.Locations
{
    public interface IGeofenceDelegate
    {
        void OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
    }
}
