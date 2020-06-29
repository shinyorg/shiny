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


        public MotionActivityType? TrackingActivityTypes
        {
            get => this.settings.Get<MotionActivityType?>(nameof(TrackingActivityTypes), null);
            private set => this.settings.Set(nameof(TrackingActivityTypes), value);
        }


        public Task<IList<Trip>> GetAllTrips() => this.dataService.GetAll();
        public Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId) => this.dataService.GetCheckinsByTrip(tripId);
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


        public async Task StartTracking(MotionActivityType activityTypes) 
        {
            (await this.RequestAccess()).Assert();

            await this.gpsManager.StartListener(GpsRequest.Realtime(true));
            this.TrackingActivityTypes = activityTypes;
        }


        public async Task StopTracking()
        {
            this.TrackingActivityTypes = null;
            await this.gpsManager.StopListener();
        }
    }
}
