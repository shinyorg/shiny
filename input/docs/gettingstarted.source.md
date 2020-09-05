Title: Getting Started
Order: 1
RedirectFrom: index
---

## Platform Setup

 
|Project|NuGet|MyGet|
|-------|-----|-----|
|Core | [![CoreNugetShield]][CoreNuget] | [![CoreMygetShield]][CoreMyget] |
|Beacons | [![BeaconsNugetShield]][BeaconsNuget] | [![BeaconsMygetShield]][BeaconsMyget] |
|Beacon Advertising|||
|BluetoothLE| [![BleNugetShield]][BleNuget] | [![BleMygetShield]][BleMyget] |
|BluetoothLE Hosting||||
|Locations| [![LocationsNugetShield]][LocationsNuget] | [![LocationsMygetShield]][LocationsMyget] |
|NFC|||
|HTTP Transfers| [![HttpNugetShield]][HttpNuget] | [![HttpMygetShield]][HttpMyget] |
|Sensors| [![SensorsNugetShield]][SensorsNuget] | [![SensorsMygetShield]][SensorsMyget] |
|Notifications| [![NotificationsNugetShield]][NotificationsNuget] | [![NotificationsMygetShield]][NotificationsMyget] |
|Push|||
|Push - Azure Notification Hubs|||
|Push - Firebase||||
|Integrations - SQLite| [![SqliteNugetShield]][SqliteNuget] | [![SqliteMygetShield]][SqliteMyget] |
|Intgrations - AppCenter| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterMygetShield]][AppCenterMyget] |

|App Service - Data Sync|||
|App Service - Location Sync|||
|App Service - Media Sync|||
|App Service - Trip Tracker|||

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

## Setup

1. The first thing is to install any of the nuget packages you need from above.  

2. In your shared code project.  Create a Shiny startup file:

snippet: YourShinyStartup

### Android

1. Create a new "MainApplication" in your Android head project.

```csharp
using System;
using Android.App;
using Android.Runtime;

namespace YourNamespace.Droid
{
    [Application]
    public class MainApplication : Shiny.ShinyAndroidApplication<YourNamespace.YourShinyStartup>
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}

```

IF you have an application file already, simple add the following to your OnCreate method

```csharp
public override void OnCreate()
{
    base.OnCreate();

    Shiny.AndroidShinyHost.Init(
        this,
        new YourShinyStartup()
    );
}
```


2. Add the following to your activity classes

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
{
    AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

### iOS

1. In your ApplicationDelegate.cs, add the following in your FinishedLaunching method
```csharp
public override bool FinishedLaunching(UIApplication app, NSDictionary options)
{
    Shiny.iOSShinyHost.Init(new YourShinyStartup());
    ...
}
```

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

### Tizen
COMING SOON

### macOS
macOS, watchOS, & tvOS are not officially supported by Shiny yet, but will be in the future

# Shiny Startup

Startup is the place where you wire up all of the necessary application depedencies you need


Out of the box, Shiny automatically pushes the following on to the service container

* IEnvironment
* IPowerManager
* IJobManager
* ISettings
