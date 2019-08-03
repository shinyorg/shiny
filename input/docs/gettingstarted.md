Title: Getting Started
Order: 1
RedirectFrom:
  - index
---

## Platform Setup

 
|Project|NuGet|MyGet|
|-------|-----|-----|
|Core | [![CoreNugetShield]][CoreNuget] | [![CoreMygetShield]][CoreMyget] |
|Beacons | [![BeaconsNugetShield]][BeaconsNuget] | [![BeaconsMygetShield]][BeaconsMyget] |
|BluetoothLE| [![BleNugetShield]][BleNuget] | [![BleMygetShield]][BleMyget] |
|Locations| [![LocationsNugetShield]][LocationsNuget] | [![LocationsMygetShield]][LocationsMyget] |
|HTTP Transfers| [![HttpNugetShield]][HttpNuget] | [![HttpMygetShield]][HttpMyget] |
|Sensors| [![SensorsNugetShield]][SensorsNuget] | [![SensorsMygetShield]][SensorsMyget] |
|Notifications| [![NotificationsNugetShield]][NotificationsNuget] | [![NotificationsMygetShield]][NotificationsMyget] |
|SQLite Integration| [![SqliteNugetShield]][SqliteNuget] | [![SqliteMygetShield]][SqliteMyget] |
|AppCenter Logging Integration| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterMygetShield]][AppCenterMyget] |

[BeaconsNugetShield]: https://img.shields.io/nuget/v/Shiny.Beacons.svg
[BeaconsNuget]: https://www.nuget.org/packages/Shiny.Beacons/
[BeaconsMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Beacons.svg
[BeaconsMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Beacons

[CoreNugetShield]: https://img.shields.io/nuget/v/Shiny.Core.svg
[CoreNuget]: https://www.nuget.org/packages/Shiny.Core/
[CoreMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Core.svg
[CoreMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Core

[BleNugetShield]: https://img.shields.io/nuget/v/Shiny.BluetoothLE.svg
[BleNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE/
[BleMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.BluetoothLE.svg
[BleMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.BluetoothLE

[LocationsNugetShield]: https://img.shields.io/nuget/v/Shiny.Locations.svg
[LocationsNuget]: https://www.nuget.org/packages/Shiny.Locations/
[LocationsMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Locations.svg
[LocationsMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Locations

[SensorsNugetShield]: https://img.shields.io/nuget/v/Shiny.Sensors.svg
[SensorsNuget]: https://www.nuget.org/packages/Shiny.Sensors/
[SensorsMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Sensors.svg
[SensorsMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Sensors

[HttpNugetShield]: https://img.shields.io/nuget/v/Shiny.Net.Http.svg
[HttpNuget]: https://www.nuget.org/packages/Shiny.Net.Http/
[HttpMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Net.Http.svg
[HttpMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Net.Http

[NotificationsNugetShield]: https://img.shields.io/nuget/v/Shiny.Notifications.svg
[NotificationsNuget]: https://www.nuget.org/packages/Shiny.Notifications/
[NotificationsMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Notifications.svg
[NotificationsMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Notifications

[SqliteNugetShield]: https://img.shields.io/nuget/v/Shiny.Integrations.Sqlite.svg
[SqliteNuget]: https://www.nuget.org/packages/Shiny.Integrations.Sqlite/
[SqliteMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Integrations.Sqlite.svg
[SqliteMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Integrations.Sqlite

[AppCenterNugetShield]: https://img.shields.io/nuget/v/Shiny.Logging.AppCenter.svg
[AppCenterNuget]: https://www.nuget.org/packages/Shiny.Logging.AppCenter/
[AppCenterMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Logging.AppCenter.svg
[AppCenterMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Logging.AppCenter


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