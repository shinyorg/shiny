Title: GPS
Description: Monitoring GPS
---
# GPS

The Global Position System (GPS) on Shiny is actually a bit more complicated than other Shiny modules as it can really "hammer" away on your resources if you set it up wrong.

```csharp
using Shiny;
using Microsoft.Extensions.DependencyInjection;


public class YourStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseGps(); // TODO: background
    }
}
```


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


public class YourGpsDelegate : IGpsDelegate 
{

    public async Task OnStatusChanged(GeofenceRegion region)
    {

    }
}
```

# HOW TO USE

## To start monitoring

```csharp
Shiny.ShinyHost.Resolve<Shiny.Locations.IGpsManager>()
```
