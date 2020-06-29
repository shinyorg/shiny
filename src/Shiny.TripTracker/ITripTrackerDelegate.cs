using System;
using System.Threading.Tasks;


namespace Shiny.TripTracker
{
    public interface ITripTrackerDelegate
    {
        Task OnTripStart(Trip trip);
        Task OnTripEnd(Trip trip);
    }
}
