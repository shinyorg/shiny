Title: Trip Tracker
---

```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class TripTrackerStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {

        // you can also pass Shiny.TripTracker.TripTrackingType to this method and it will request permissions
        // from the user directly on startup
        services.UseTripTracker<TripTrackerDelegate>();
    }
}
```

```cs
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

```

```cs
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

```

```cs
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

```