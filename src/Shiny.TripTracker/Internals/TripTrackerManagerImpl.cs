using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Shiny.Locations;

namespace Shiny.TripTracker.Internals
{
    public class TripTrackerManagerImpl : ITripTrackerManager
    {
        readonly IGpsManager gpsManager;
        readonly IMotionActivityManager motionActivityManager;
        readonly IDataService dataService;


        public TripTrackerManagerImpl(IGpsManager gpsManager, 
                                      IMotionActivityManager motionActivityManager,
                                      IDataService dataService)
        {
            this.gpsManager = gpsManager;
            this.motionActivityManager = motionActivityManager;
            this.dataService = dataService;
        }

        public Task<List<Trip>> GetAllTrips()
        {
            throw new NotImplementedException();
        }

        public Task<IList<TripCheckin>> GetCheckinsByTrip(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task Purge()
        {
            throw new NotImplementedException();
        }

        public Task Remove(Guid tripId)
        {
            throw new NotImplementedException();
        }

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public Task StartTracking()
        {
            throw new NotImplementedException();
        }

        public Task StopTracking()
        {
            throw new NotImplementedException();
        }
    }
}
