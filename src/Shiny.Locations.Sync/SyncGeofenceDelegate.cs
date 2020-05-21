using Shiny.Locations;
using System;
using System.Threading.Tasks;


namespace Shiny.Services.LocationSync
{
    public class SyncGeofenceDelegate : IGeofenceDelegate
    {
        public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            throw new NotImplementedException();
        }
    }
}
