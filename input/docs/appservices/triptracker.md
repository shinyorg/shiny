Title: Trip Tracker
Order: 1
---

<!-- snippet: TripTrackerStartup.cs -->
<a id='snippet-TripTrackerStartup.cs'></a>
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
<sup><a href='/src/Snippets/TripTrackerStartup.cs#L1-L13' title='File snippet `TripTrackerStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-TripTrackerStartup.cs' title='Navigate to start of snippet `TripTrackerStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: TripTrackerDelegate.cs -->
<a id='snippet-TripTrackerDelegate.cs'></a>
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
<sup><a href='/src/Snippets/TripTrackerDelegate.cs#L1-L25' title='File snippet `TripTrackerDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-TripTrackerDelegate.cs' title='Navigate to start of snippet `TripTrackerDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: TripTrackerUsage.cs -->
<a id='snippet-TripTrackerUsage.cs'></a>
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
<sup><a href='/src/Snippets/TripTrackerUsage.cs#L1-L14' title='File snippet `TripTrackerUsage.cs` was extracted from'>snippet source</a> | <a href='#snippet-TripTrackerUsage.cs' title='Navigate to start of snippet `TripTrackerUsage.cs`'>anchor</a></sup>
<!-- endSnippet -->
