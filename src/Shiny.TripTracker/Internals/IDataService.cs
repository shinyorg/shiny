using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.TripTracker.Internals
{
    public interface IDataService
    {
        Task<Trip> GetTrip(int tripId);
        Task<IList<Trip>> GetAll();
        Task<IList<TripCheckin>> GetCheckinsByTrip(int tripId);
        Task Purge();
        Task<double> GetTripAverageSpeed(int tripId);

        Task Save(Trip trip);
        Task Checkin(int tripId, IGpsReading reading);
        Task Remove(int tripId);
    }
}
