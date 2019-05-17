using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public abstract class AbstractGeofenceManager : IGeofenceManager
    {
        protected AbstractGeofenceManager(IRepository repository)
        {
            this.Repository = repository.Wrap();
        }


        public RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> Repository { get; }

        public abstract AccessState Status { get; }
        public abstract Task<AccessState> RequestAccess();
        public abstract IObservable<AccessState> WhenAccessStatusChanged();
        public abstract Task StartMonitoring(GeofenceRegion region);
        public abstract Task StopMonitoring(GeofenceRegion region);
        public abstract Task StopAllMonitoring();
        public abstract Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default);
        public virtual async Task<IEnumerable<GeofenceRegion>> GetMonitorRegions() => await this.Repository.GetAll();
    }
}
