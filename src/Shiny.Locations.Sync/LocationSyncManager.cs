using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync
{
    public interface ILocationSyncManager
    {
        Task ForceRun();

        // TODO: what if a geofence is removed?  let user deal with it?
        Task<IList<GeofenceEvent>> GetPendingGeofenceEvents();
        Task ClearGeofenceEvents();

        Task<IList<GpsEvent>> GetPendingGpsEvents();
        Task ClearGpsEvents();
    }


    public class LocationSyncManager : ILocationSyncManager
    {
        readonly IJobManager jobManager;
        readonly IRepository repository;


        public LocationSyncManager(IRepository repository, IJobManager jobManager)
        {
            this.repository = repository;
            this.jobManager = jobManager;
        }


        public Task ForceRun() => Task.WhenAll
        (
            this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer),
            this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier)
        );
        public Task ClearGeofenceEvents() => this.repository.Clear<GeofenceEvent>();
        public Task ClearGpsEvents() => this.repository.Clear<GpsEvent>();
        public Task<IList<GeofenceEvent>> GetPendingGeofenceEvents() => this.repository.GetAll<GeofenceEvent>();
        public Task<IList<GpsEvent>> GetPendingGpsEvents() => this.repository.GetAll<GpsEvent>();
    }
}
