using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync
{
    public interface IGeofenceSyncDelegate
    {
        Task Process(IEnumerable<GeofenceEvent> geofence);
    }
}
