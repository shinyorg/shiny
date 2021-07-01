using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations.Infrastructure
{
    public abstract class AbstractGeofenceManager : IGeofenceManager
    {
        protected AbstractGeofenceManager(IRepository repository)
        {
            this.Repository = repository.Wrap();
        }


        public RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> Repository { get; }

        public abstract Task<AccessState> RequestAccess();
        public abstract Task StartMonitoring(GeofenceRegion region);
        public abstract Task StopMonitoring(string identifier);
        public abstract Task StopAllMonitoring();
        public abstract Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default);
        public virtual async Task<IEnumerable<GeofenceRegion>> GetMonitorRegions() => await this.Repository.GetAll();
    }
}
