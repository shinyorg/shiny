using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class LocationSyncManager : NotifyPropertyChanged, ILocationSyncManager
    {
        readonly IJobManager jobManager;
        readonly IRepository repository;
        readonly IMotionActivityManager activityManager;
        readonly IGeofenceManager geofenceManager;
        readonly IGpsManager gpsManager;


        public LocationSyncManager(IRepository repository, 
                                   IJobManager jobManager,
                                   IMotionActivityManager activityManager,
                                   IGeofenceManager geofenceManager,
                                   IGpsManager gpsManager)
        {
            this.repository = repository;
            this.jobManager = jobManager;
            this.activityManager = activityManager;
            this.geofenceManager = geofenceManager;
            this.gpsManager = gpsManager;
        }


        public async Task StartGeofenceMonitoring(GeofenceRegion? region = null, SyncConfig? config = null)
        {
            (await this.geofenceManager.RequestAccess()).Assert();

            var jobInfo = new JobInfo(typeof(SyncGeofenceJob));
            jobInfo.SetSyncConfig(config ?? new SyncConfig
            {
                BatchSize = 1,
                SortMostRecentFirst = true
            });
            await this.jobManager.Schedule(jobInfo);
            if (!(region is null))
                await this.geofenceManager.StartMonitoring(region);            
        }


        public async Task StartGpsMonitoring(GpsRequest request, SyncConfig? config = null)
        {
            if (this.gpsManager.IsListening)
                throw new ArgumentException("GPS Manager is already listening");

            (await this.gpsManager.RequestAccessAndStart(request)).Assert();
            
            var jobInfo = new JobInfo(typeof(SyncGpsJob));
            jobInfo.SetSyncConfig(config ?? new SyncConfig
            {
                BatchSize = 10,
                SortMostRecentFirst = false
            });
            await this.jobManager.Schedule(jobInfo);
            await this.gpsManager.StartListener(request);
        }


        public async Task StopMonitoring(LocationSyncType syncType)
        {
            var result = await this.IsMonitoring(syncType);
            if (!result)
                return;

            switch (syncType)
            {
                case LocationSyncType.Geofence:
                    await this.jobManager.Cancel(nameof(SyncGeofenceJob));
                    await this.geofenceManager.StopAllMonitoring();
                    break;

                case LocationSyncType.GPS:
                    await this.jobManager.Cancel(nameof(SyncGpsJob));
                    await this.gpsManager.StopListener();
                    break;
            }
        }


        public async Task<bool> IsMonitoring(LocationSyncType syncType)
        {
            var jobId = syncType == LocationSyncType.Geofence 
                ? nameof(SyncGeofenceJob) 
                : nameof(SyncGpsJob);

            var job = await this.jobManager.GetJob(jobId);
            return (job != null);
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
