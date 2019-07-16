using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations
{
    public class GeofenceManagerImpl : IGeofenceManager
    {
        public AccessState Status => throw new NotImplementedException();

        public Task<IEnumerable<GeofenceRegion>> GetMonitorRegions()
        {
            throw new NotImplementedException();
        }

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public Task StartMonitoring(GeofenceRegion region)
        {
            throw new NotImplementedException();
        }

        public Task StopAllMonitoring()
        {
            throw new NotImplementedException();
        }

        public Task StopMonitoring(GeofenceRegion region)
        {
            throw new NotImplementedException();
        }

        public IObservable<AccessState> WhenAccessStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}
