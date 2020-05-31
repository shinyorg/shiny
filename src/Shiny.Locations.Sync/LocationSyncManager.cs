using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync
{
    public enum LocationSyncType
    {
        GPS,
        Geofence
    }


    public interface ILocationSyncManager
    {
        Task ForceRun(LocationSyncType? syncType = null);

        Task<SyncConfig?> GetConfig(LocationSyncType syncType);
        Task SetConfig(LocationSyncType syncType, SyncConfig config);
        Task ClearEvents(LocationSyncType? syncType = null);

        Task<IList<GeofenceEvent>> GetPendingGeofenceEvents();
        Task<IList<GpsEvent>> GetPendingGpsEvents();
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


        public async Task ForceRun(LocationSyncType? syncType = null) 
        {
            switch (syncType)
            {
                case LocationSyncType.Geofence:
                    await this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer);
                    break;

                case LocationSyncType.GPS:
                    await this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier);
                    break;

                default:
                    await Task.WhenAll
                    (
                        this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer),
                        this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier)
                    );
                    break;
            }
        }

            
        public Task<IList<GeofenceEvent>> GetPendingGeofenceEvents() => this.repository.GetAll<GeofenceEvent>();
        public Task<IList<GpsEvent>> GetPendingGpsEvents() => this.repository.GetAll<GpsEvent>();


        static string GetKey(LocationSyncType syncType) => syncType == LocationSyncType.Geofence
            ? Constants.GeofenceJobIdentifer
            : Constants.GpsJobIdentifier;


        public async Task<SyncConfig?> GetConfig(LocationSyncType syncType)
        {
            var key = GetKey(syncType);
            var job = await this.jobManager.GetJob(key);
            if (job == null)
                return null;

            return job.GetParameter<SyncConfig>(Constants.SyncConfigJobParameterKey);
        }


        public async Task SetConfig(LocationSyncType syncType, SyncConfig config)
        {
            var key = GetKey(syncType);
            var job = await this.jobManager.GetJob(key);
            if (job == null)
                throw new ArgumentException("LocationSync type is not active");

            job.SetParameter(Constants.SyncConfigJobParameterKey, config);
            await this.jobManager.Schedule(job);
        }


        public async Task ClearEvents(LocationSyncType? syncType = null)
        {
            switch (syncType)
            {
                case LocationSyncType.Geofence:
                    await this.repository.Clear<GeofenceEvent>();
                    break;

                case LocationSyncType.GPS:
                    await this.repository.Clear<GpsEvent>();
                    break;

                default:
                    await Task.WhenAll(
                        this.repository.Clear<GeofenceEvent>(),
                        this.repository.Clear<GpsEvent>()
                    );
                    break;
            }
        }
    }
}
