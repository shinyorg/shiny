using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Locations;
using Shiny.Settings;


namespace Shiny.TripTracker.Internals
{
    public class TripTrackerManagerImpl : ITripTrackerManager
    {
        readonly IGpsManager gpsManager;
        readonly IMotionActivityManager motionActivityManager;
        readonly IDataService dataService;
        readonly ISettings settings;


        public TripTrackerManagerImpl(IGpsManager gpsManager, 
                                      IMotionActivityManager motionActivityManager,                                      
                                      IDataService dataService,
                                      ISettings settings)
        {
            this.gpsManager = gpsManager;
            this.motionActivityManager = motionActivityManager;
            this.dataService = dataService;
            this.settings = settings;
        }


        public TripTrackingType? TrackingType
        {
            get => this.settings.Get<TripTrackingType?>(nameof(TrackingType), null);
            private set => this.settings.Set(nameof(TrackingType), value);
        }
        public Task<IList<Trip>> GetAllTrips() => this.dataService.GetAll();
        public Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId) => this.dataService.GetCheckinsByTrip(tripId);
        public Task<double> GetTripAverageSpeed(int tripId) => this.dataService.GetTripAverageSpeedInMetersPerHour(tripId);
        public Task Purge() => this.dataService.Purge();
        public Task Remove(int tripId) => this.dataService.Remove(tripId);


        public async Task<AccessState> RequestAccess()
        {
            var result = await this.motionActivityManager.RequestPermission();
            if (result != AccessState.Available)
                return result;

            result = await this.gpsManager.RequestAccess(GpsRequest.Realtime(true));
            return result;
        }


        public async Task StartTracking(TripTrackingType trackingType) 
        {
            if (this.TrackingType != null)
                throw new ArgumentException("Trip tracking is already running");

            (await this.RequestAccess()).Assert();
            this.TrackingType = trackingType;
            await this.gpsManager.StartListener(new GpsRequest
            {
                Interval = TimeSpan.FromSeconds(10),
                ThrottledInterval = TimeSpan.FromSeconds(5),
                UseBackground = true
            });
        }


        public async Task StopTracking()
        {
            if (this.TrackingType == null)
                return;

            this.TrackingType = null;
            await this.gpsManager.StopListener();
        }
    }
}
