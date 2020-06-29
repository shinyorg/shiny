using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.TripTracker.Internals
{
    public interface IDataService
    {
        Task<IList<Trip>> GetAll();
        Task<IList<TripCheckin>> GetCheckinsByTrip(Guid tripId);
        Task Purge();

        Task Save(Trip trip);
        Task Checkin(TripCheckin checkin);
        Task Remove(Guid tripId);
    }
}
