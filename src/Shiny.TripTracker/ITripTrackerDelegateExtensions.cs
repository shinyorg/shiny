using System;
using System.Threading.Tasks;


namespace Shiny.TripTracker
{
    public interface ITripTrackerDelegateExtensions
    {
        Task<bool> ShouldMarkTripCompleted(Trip trip, TripCheckin checkin);
    }
}
