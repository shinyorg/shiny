using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.TripTracker
{
    public interface ITripTrackerManager
    {
        MotionActivityType? TrackingActivityTypes { get; }

        Task StartTracking(MotionActivityType activityTypes);
        Task StopTracking();

        Task<IList<Trip>> GetAllTrips();
        Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId);

        Task Remove(int tripId);
        Task Purge();
        Task<AccessState> RequestAccess();
    }
}
