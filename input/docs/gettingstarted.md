Title: Getting Started
Order: 1
RedirectFrom: index
---

## Platform Setup


 


## Setup

1. The first thing is to install any of the nuget packages you need from above.  

2. In your shared code project.  Create a Shiny startup file:

<!-- snippet: YourShinyStartup.cs -->
<a id='snippet-YourShinyStartup.cs'></a>
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
<sup><a href='/src/Snippets/YourShinyStartup.cs#L1-L14' title='File snippet `YourShinyStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-YourShinyStartup.cs' title='Navigate to start of snippet `YourShinyStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

### Android
At build time, Shiny will attempt to auto-generate an Android application file and wire-up all of the necessary boilerplate to any of your Android activities. 


### iOS

At build time, Shiny will attempt to auto-generate all of the necessary boilerplate into your AppDelegate. 


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


Out of the box, Shiny automatically pushes the following on to the service container

* IEnvironment
* IPowerManager
* IJobManager
* ISettings
