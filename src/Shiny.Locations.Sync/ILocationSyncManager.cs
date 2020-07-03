using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Locations.Sync
{
    public interface ILocationSyncManager
    {
        Task StartGeofenceMonitoring(GeofenceRegion? region = null, SyncConfig? config = null);
        Task StartGpsMonitoring(GpsRequest request, SyncConfig? config = null);
        Task StopMonitoring(LocationSyncType syncType);
        Task<bool> IsMonitoring(LocationSyncType syncType);

        Task ForceRun(LocationSyncType? syncType = null);
        Task ClearEvents(LocationSyncType? syncType = null);

        Task<IList<GeofenceEvent>> GetPendingGeofenceEvents();
        Task<IList<GpsEvent>> GetPendingGpsEvents();
    }
}
