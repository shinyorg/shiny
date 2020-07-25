using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync.Infrastructure
{
    public interface IGeofenceDataService
    {
        Task<List<GeofenceEvent>> GetAll();
        Task Create(GeofenceEvent geofenceEvent);
        Task Remove(GeofenceEvent geofenceEvent);
        Task<int> GetPendingCount();
    }
}
