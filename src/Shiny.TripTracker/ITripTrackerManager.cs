using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.TripTracker
{
    public interface ITripTrackerManager
    {
        Task StartTracking();
        Task StopTracking();

        Task<List<Trip>> GetAllTrips();
        Task<IList<TripCheckin>> GetCheckinsByTrip(Guid tripId);

        Task Remove(Guid tripId);
        Task Purge();
        Task<AccessState> RequestAccess();
    }
}
