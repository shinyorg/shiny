using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations.Sync;

public class LocationSyncGpsDelegate : IGpsSyncDelegate
{
    public Task Process(IEnumerable<GpsEvent> gpsEvent, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}