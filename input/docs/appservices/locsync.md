Title: Location Sync
---

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
