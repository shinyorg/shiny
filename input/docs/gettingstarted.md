Title: Getting Started
Order: 1
RedirectFrom:
  - index
---

## Platform Setup

 TODO: Links to all nuget packages on nuget/myget
 
### Android


```csharp

public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
{
    AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}


using System;
using Shiny;
using Shiny.Jobs;
using Android.App;
using Android.Runtime;
using Samples.ShinySetup;


namespace Samples.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    //public class MainApplication : ShinyAndroidApplication<SampleStartup>
    public class MainApplication : Application
    {
        public MainApplication() : base() { }
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }


        public override void OnCreate()
        {
            base.OnCreate();

            AndroidShinyHost.Init(
                this,
                new SampleStartup()
#if DEBUG
                , services =>
                {
                    // TODO: make android great again - by running jobs faster for debugging purposes ;)
                    services.ConfigureJobService(TimeSpan.FromMinutes(1));
                }
#endif
            );
        }
    }
}
```
# Shiny Startup

Startup is the place where you wire up all of the necessary application depedencies you need


Out of the box, Shiny automatically pushes the following on to the service container

* IEnvironment
* IPowerManager
* IJobManager
* ISettings

## Modules

```csharp
using Shiny;
using Microsoft.Extensions.DependencyInjection;


public class YourModule : ShinyModule 
{
    public override void Register(IServiceCollection services) 
    {

    }
}
```

## Startup Tasks

```csharp
public class YourStartupTask : IShinyStartupTask
{
    // you can inject into the constructor here as long as you register the service in the sta
    public void Start() 
    {

    }
}
```

## State Restorable Services

This is pretty cool, imagine you want the state of your service preserved across restarts - Shiny does this in epic fashion

Simply turn your service into a viewmodel and register it in your shiny startup and Shiny will take care of the rest

```csharp
// you can inject into this thing as well add IShinyStartupTask as well
public class MyBadAssService : INotifyPropertyChanged, IMyBadAssService, IStartupTask
{
    public int RunCount
    {
        // left out for brevity
        get ...
        set ...
    }


    public void Start()
    {
        this.Count++;
    }
}
```