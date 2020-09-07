using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;

public class GeofenceUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IGeofenceManager>();
        var result = await manager.RequestAccess();
        if (result == AccessState.Available)
        {
            await manager.StartMonitoring(new GeofenceRegion(
                "YourIdentifier",
                new Position(1, 1),
                Distance.FromKilometers(1)
            ));
        }
    }


    public async Task Stop()
    {
        await ShinyHost.Resolve<IGeofenceManager>().StopMonitoring("YourIdentifier");

        // or

        await ShinyHost.Resolve<IGeofenceManager>().StopAllMonitoring();
    }
}