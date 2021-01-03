Title: Getting Started
Order: 1
---

## Platform Setup


 


## Setup

1. The first thing is to install any of the nuget packages you need from above.  

2. In your shared code project.  Create a Shiny startup file:

```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace YourNamespace
{
    public class YourShinyStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // this is where you'll load things like BLE, GPS, etc - those are covered in other sections
            // things like the jobs, environment, power, are all installed automatically
        }
    }
}
```

As another alternative, you can have Shiny generate a startup file automatically at compile time.

### Android & iOS

The best option is to install the [![ShinyNugetShield]][ShinyNuget] in your head Android project.  The next thing is to add an [assembly: Shiny.ShinyApplication] to any class in the same head project.  This will tell Shiny to generate not only all the necessary boilerplate (Android application and all activities that are marked as partial - and the AppDelegate on iOS), but also generate all of the startup configuration to register services with Shiny.




### UWP

1. Add the following to your App.xaml.cs constructor

```csharp
this.ShinyInit(new YourStartup());
```

2. Add the following to your Package.appxmanifest under the <Application><Extensions> node

```xml
<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="general"/>
        <Task Type="systemEvent"/>
        <Task Type="timer"/>
    </BackgroundTasks>
</Extension>
```

Out of the box, Shiny automatically adds all of the services for Jobs, file system, power monitoring, and settings (as well as several other services need by the Shiny internals)

[ShinyNugetShield]: https://img.shields.io/nuget/v/Shiny.svg?style=for-the-badge
[ShinyNuget]: https://www.nuget.org/packages/Shiny/