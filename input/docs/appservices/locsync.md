Title: Location Sync
Order: 2
---

<!-- snippet: LocationSyncStartup.cs -->
<a id='snippet-LocationSyncStartup.cs'></a>
```cs
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class LocationSyncStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        //services.UseGps<>();
        //services.UseGeofencingSync<>();
    }
}
```
<sup><a href='/src/Snippets/LocationSyncStartup.cs#L1-L12' title='File snippet `LocationSyncStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-LocationSyncStartup.cs' title='Navigate to start of snippet `LocationSyncStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: LocationSyncGeofenceDelegate.cs -->
<a id='snippet-LocationSyncGeofenceDelegate.cs'></a>
```cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations.Sync;

public class LocationSyncGeofenceDelegate : IGeofenceSyncDelegate
{
    public Task Process(IEnumerable<GeofenceEvent> geofence, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}
```
<sup><a href='/src/Snippets/LocationSyncGeofenceDelegate.cs#L1-L13' title='File snippet `LocationSyncGeofenceDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-LocationSyncGeofenceDelegate.cs' title='Navigate to start of snippet `LocationSyncGeofenceDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: LocationSyncGpsDelegate.cs -->
<a id='snippet-LocationSyncGpsDelegate.cs'></a>
```cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Locations.Sync;

public class LocationSyncGpsDelegate : IGpsSyncDelegate
{
    public Task Process(IEnumerable<GpsEvent> gpsEvent, CancellationToken cancelToken)
    {
        throw new NotImplementedException();
    }
}
```
<sup><a href='/src/Snippets/LocationSyncGpsDelegate.cs#L1-L13' title='File snippet `LocationSyncGpsDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-LocationSyncGpsDelegate.cs' title='Navigate to start of snippet `LocationSyncGpsDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->
