<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/appservices/triptracker.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: Trip Tracker
Order: 1
---

<!-- snippet: TripTrackerStartup.cs -->
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
<sup>[snippet source](/src/Snippets/TripTrackerStartup.cs#L1-L13)</sup>
<!-- endsnippet -->

<!-- snippet: TripTrackerDelegate.cs -->
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
<sup>[snippet source](/src/Shiny.TripTracker/ITripTrackerDelegate.cs#L1-L13)</sup>
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
<sup>[snippet source](/src/Snippets/TripTrackerDelegate.cs#L1-L26)</sup>
<!-- endsnippet -->

<!-- snippet: TripTrackerUsage.cs -->
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
<sup>[snippet source](/src/Snippets/TripTrackerUsage.cs#L1-L15)</sup>
<!-- endsnippet -->
