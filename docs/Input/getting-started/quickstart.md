Title: Quick Start
Order: 1
---
# Startup

## Setup

1. The first thing is to install Shiny.Core as it used by all of the Shiny libraries (or Shiny which contains the code gen, but we'll get to that later).  

2. In your shared code project.  Create a Shiny startup file:

```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace YourNamespace
{
    public class YourShinyStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        {
            // this is where you'll load things like BLE, GPS, etc - those are covered in other sections
            // things like the jobs, environment, power, are all installed automatically
        }
    }
}
```

As another alternative, you can have Shiny generate a startup file automatically at compile time.

### Android & iOS

The best option is to install the <?# NugetShield "Shiny" /?> in your head Android project.  The next thing is to add an [assembly: Shiny.ShinyApplication] to any class in the same head project.  This will tell Shiny to generate not only all the necessary boilerplate (Android application and all activities that are marked as partial - and the AppDelegate on iOS), but also generate all of the startup configuration to register services with Shiny.



Out of the box, Shiny automatically adds all of the services for Jobs, file system, power monitoring, and settings (as well as several other services need by the Shiny internals)

[ShinyNugetShield]: https://img.shields.io/nuget/v/Shiny.svg?style=for-the-badge
[ShinyNuget]: https://www.nuget.org/packages/Shiny/