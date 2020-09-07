using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.TripTracker;

public class TripTrackerUsage
{
    public async Task YourMethod()
    {
        var manager = ShinyHost.Resolve<ITripTrackerManager>();
        var trips = await manager.GetAllTrips();

    }
}
