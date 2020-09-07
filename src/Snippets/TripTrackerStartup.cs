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