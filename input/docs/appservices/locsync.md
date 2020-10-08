<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/appservices/locsync.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: Location Sync
Order: 2
---

<!-- snippet: LocationSyncStartup.cs -->
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
<sup>[snippet source](/src/Snippets/LocationSyncStartup.cs#L1-L13)</sup>
<!-- endsnippet -->

<!-- snippet: LocationSyncGeofenceDelegate.cs -->
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
<sup>[snippet source](/src/Snippets/LocationSyncGeofenceDelegate.cs#L1-L13)</sup>
<!-- endsnippet -->

<!-- snippet: LocationSyncGpsDelegate.cs -->
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
<sup>[snippet source](/src/Snippets/LocationSyncGpsDelegate.cs#L1-L13)</sup>
<!-- endsnippet -->
