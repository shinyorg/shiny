using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Locations;

namespace Shiny.TripTracker
{
    public interface ITripTrackerManager
    {
        TripTrackingType? TrackingType { get; }

        Task StartTracking(TripTrackingType trackingType, GpsRequest? gpsConfiguration = null);
        Task StopTracking();

        Task<IList<Trip>> GetAllTrips();
        Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId);

        Task Remove(int tripId);
        Task Purge();
        Task<AccessState> RequestAccess();
    }
}
