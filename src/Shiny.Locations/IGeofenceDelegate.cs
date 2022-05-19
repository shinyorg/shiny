﻿using System.Threading.Tasks;

namespace Shiny.Locations;


public interface IGeofenceDelegate
{
    /// <summary>
    /// This is fired when the geofence region status has changed
    /// </summary>
    /// <param name="newStatus">The new geofence state.</param>
    /// <param name="region">The geofence region.</param>
    Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
}
