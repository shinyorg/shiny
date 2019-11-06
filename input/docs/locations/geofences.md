Title: Geofences
---

# SETUP

## REGISTRATION

```csharp
using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;
using Microsoft.Extensions.DependencyInjection;


public class YourStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGeofencing<YourGeofenceDelegate>();
    }
}


public class YourGeofenceDelegate : IGeofenceDelegate 
{

    public async Task OnStatusChanged(GeofenceRegion region)
    {

    }
}
```

# HOW TO USE

## To start monitoring

```csharp
Shiny.ShinyHost.Resolve<Shiny.Locations.IGeofenceManager>().StartMonitoring(new GeofenceRegion( 
    "My House", // identifier - must be unique per registered geofence
    Center = new Position(LATITUDE, LONGITUDE), // center point    
    Distance.FromKilometers(1) // radius of fence
));
```

## Stop monitoring a region
    
```csharp
Shiny.ShinyHost.Resolve<Shiny.Locations.IGeofenceManager>().StopMonitoring(GeofenceRegion);

//or

Shiny.ShinyHost.Resolve<Shiny.Locations.IGeofenceManager>().StopAllMonitoring();
```
