using System.Threading.Tasks;
using Shiny.TripTracker;

public class TripTrackerDelegate : ITripTrackerDelegate
{
    readonly ITripTrackerManager manager;
    public TripTrackerDelegate(ITripTrackerManager manager)
        => this.manager = manager;


    public async Task OnTripStart(Trip trip)
    {
        // called when a new trip is detected
    }


    public async Task OnTripEnd(Trip trip)
    {
        // called when a new trip is detected

        // if you want all the GPS pings while the trip was going. NOTE: the use of dependency injection
        // to get access to the ITripTrackerManager
        var route = await this.manager.GetCheckinsByTrip(trip.Id);
    }
}
