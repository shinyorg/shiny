using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync.Infrastructure
{
    public interface IGpsDataService
    {
        Task<List<GpsEvent>> GetAll();
        Task Create(GpsEvent geofenceEvent);
        Task Remove(GpsEvent geofenceEvent);
        Task<int> GetPendingCount();
    }
}
