Title: Geofences
Order: 2
---


<!-- snippet: GeofenceStartup.cs -->
<a id='snippet-GeofenceStartup.cs'></a>
```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class GeofenceStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGeofencing<GeofenceDelegate>();
    }
}
```
<sup><a href='/src/Snippets/GeofenceStartup.cs#L1-L10' title='File snippet `GeofenceStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-GeofenceStartup.cs' title='Navigate to start of snippet `GeofenceStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: GeofenceDelegate.cs -->
<a id='snippet-GeofenceDelegate.cs'></a>
```cs
using System.Threading.Tasks;
using Shiny.Locations;

public class GeofenceDelegate : IGeofenceDelegate
{
    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
    }
}
```
<sup><a href='/src/Snippets/GeofenceDelegate.cs#L1-L9' title='File snippet `GeofenceDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-GeofenceDelegate.cs' title='Navigate to start of snippet `GeofenceDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: GeofenceUsage.cs -->
<a id='snippet-GeofenceUsage.cs'></a>
```cs
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
```
<sup><a href='/src/Snippets/GeofenceUsage.cs#L1-L30' title='File snippet `GeofenceUsage.cs` was extracted from'>snippet source</a> | <a href='#snippet-GeofenceUsage.cs' title='Navigate to start of snippet `GeofenceUsage.cs`'>anchor</a></sup>
<!-- endSnippet -->
