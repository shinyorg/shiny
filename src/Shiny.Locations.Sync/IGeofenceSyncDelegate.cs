using System;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync
{
    public interface IGeofenceSyncDelegate
    {
        Task Process(GeofenceEvent[] geofence);
    }
}
