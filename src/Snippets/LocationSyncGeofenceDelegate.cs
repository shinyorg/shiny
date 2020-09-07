using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations.Sync;

public class LocationSyncGeofenceDelegate : IGeofenceSyncDelegate
{
    public Task Process(IEnumerable<GeofenceEvent> geofence, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}