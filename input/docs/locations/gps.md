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