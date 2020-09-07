Title: GPS
Description: Monitoring GPS
---
# GPS

The Global Position System (GPS) on Shiny is actually a bit more complicated than other Shiny modules as it can really "hammer" away on your resources if you set it up wrong.

<!-- snippet: GpsStartup.cs -->
<a id='snippet-GpsStartup.cs'></a>
```cs
using Microsoft.Extensions.DependencyInjection;

using Shiny;

public class GpsStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGps();

        // OR

        services.UseGps<GpsDelegate>();
    }
}
```
<sup><a href='/src/Snippets/GpsStartup.cs#L1-L15' title='File snippet `GpsStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-GpsStartup.cs' title='Navigate to start of snippet `GpsStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: GpsDelegate.cs -->
<a id='snippet-GpsDelegate.cs'></a>
```cs
using System.Threading.Tasks;
using Shiny.Locations;

public class GpsDelegate : IGpsDelegate
{
    public async Task OnReading(IGpsReading reading)
    {
    }
}
```
<sup><a href='/src/Snippets/GpsDelegate.cs#L1-L9' title='File snippet `GpsDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-GpsDelegate.cs' title='Navigate to start of snippet `GpsDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: GpsUsage.cs -->
<a id='snippet-GpsUsage.cs'></a>
```cs
using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;


public class GpsUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IGpsManager>();
        var result = await manager.RequestAccess(GpsRequest.Realtime(true));
        if (result == AccessState.Available)
        {
            //manager.WhenReading().Subscribe(reading =>
            //{

            //});
            await manager.StartListener(GpsRequest.Realtime(true));

            await manager.StopListener();
        }
    }
}
```
<sup><a href='/src/Snippets/GpsUsage.cs#L1-L23' title='File snippet `GpsUsage.cs` was extracted from'>snippet source</a> | <a href='#snippet-GpsUsage.cs' title='Navigate to start of snippet `GpsUsage.cs`'>anchor</a></sup>
<!-- endSnippet -->
