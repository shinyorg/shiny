using System;
using System.Threading.Tasks;

namespace Shiny.Locations
{
    public interface IGeofenceDelegate
    {
        /// <summary>
        /// This is fired when the geofence region status has changed
        /// </summary>
        /// <param name="newStatus"></param>
        /// <param name="region"></param>
        Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
    }
}
