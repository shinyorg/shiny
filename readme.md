# Shiny for Xamarin & Windows 
<img src="art/logo.png" width="100" /> 


Formerly the ACR Plugins

* [Samples](https://github.com/shinyorg/shinysamples) - Shows almost every single function point within Shiny
* [Beautiful Docs](https://shinylib.net) are in the works - for now, take a look here:
  * [Introducing Shiny](https://allancritchie.net/posts/introducingshiny)
  * [Background Jobs - Shiny Style](https://allancritchie.net/posts/shinyjobs)
  * [Settings in a New Light - Shiny Style](https://allancritchie.net/posts/shinysettings)
  * [Geofencing with a Pinch of Notifications - Shiny Style](https://allancritchie.net/posts/shiny-geofencing)
  * [Startup Tasks, Modules, and Stateful Delegates - Shiny Style](https://allancritchie.net/posts/shiny-di)
  * [Beacons - Shiny Style](https://allancritchie.net/posts/shiny-beacons)
  
  
## Builds

Branch|Status
------|------
Master|[![Build status](https://dev.azure.com/shinylib/shiny/_apis/build/status/Build?branchName=master)](https://dev.azure.com/shinylib/shiny/_build/latest?definitionId=1)
Dev|[![Build status](https://dev.azure.com/shinylib/shiny/_apis/build/status/Build?branchName=dev)](https://dev.azure.com/shinylib/shiny/_build/latest?definitionId=1)|


## NuGet Packages

Shiny official releases are available on NuGet. For early test builds, you can also use the beta MyGet feed.

To use the beta MyGet feed, add `https://www.myget.org/F/acrfeed/api/v3/index.json` as a package source to Visual Studio


## Libraries

|Project|NuGet|MyGet|
|-------|-----|-----|
|Core | [![CoreNugetShield]][CoreNuget] | [![CoreMygetShield]][CoreMyget] |
|Beacons | [![BeaconsNugetShield]][BeaconsNuget] | [![BeaconsMygetShield]][BeaconsMyget] |
|BluetoothLE| [![BleNugetShield]][BleNuget] | [![BleMygetShield]][BleMyget] |
|Locations| [![LocationsNugetShield]][LocationsNuget] | [![LocationsMygetShield]][LocationsMyget] |
|HTTP Transfers| [![HttpNugetShield]][HttpNuget] | [![HttpMygetShield]][HttpMyget] |
|Sensors| [![SensorsNugetShield]][SensorsNuget] | [![SensorsMygetShield]][SensorsMyget] |
|Notifications| [![NotificationsNugetShield]][NotificationsNuget] | [![NotificationsMygetShield]][NotificationsMyget] |

## Integrations
|Project|NuGet|MyGet|Description|
|-------|-----|-----|-----------|
|SQLite| [![SqliteNugetShield]][SqliteNuget] | [![SqliteMygetShield]][SqliteMyget] | Provides caching, logging, storage, & settings implementations
|AppCenter Logging| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterMygetShield]][AppCenterMyget] | Log errors to AppCenter
|System.Text.Json Serializer| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterMygetShield]][AppCenterMyget] | This is the new .NET serializer coming into modern .NET.  This will eventually be standard in Shiny
|(Android) Current Activity| [![CurrentActivityNugetShield]][CurrentActivityNuget] | [![CurrentActivityMygetShield]][CurrentActivityMyget] | If you use James Montemagno's current top activity plugin, Shiny can use it instead of its internal version

## Contributors
* Special Thanks to [Emily Stanek](https://github.com/emilystanek) from [BSI Labs](https://github.com/BSiLabs) for the fantastic logo  
[![BSI Labs - Enterprise Mobile Done Right.](https://bsilabs.ca/wp-content/uploads/2019/04/BSILabsLogo-Grey-Lg@0.25x.png)](https://bsilabs.ca)


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

[CurrentActivityNugetShield]: https://img.shields.io/nuget/v/Shiny.Integrations.CurrentActivityPlugin.svg
[CurrentActivityNuget]: https://www.nuget.org/packages/Shiny.Integrations.CurrentActivityPlugin/
[CurrentActivityMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Integrations.CurrentActivityPlugin.svg
[CurrentActivityMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Integrations.CurrentActivityPlugin

[SysTextJsonNugetShield]: https://img.shields.io/nuget/v/Shiny.Integrations.SysTextJson.svg
[SysTextJsonNuget]: https://www.nuget.org/packages/Shiny.Integrations.SysTextJson/
[SysTextJsonMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Integrations.SysTextJson.svg
[SysTextJsonMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Integrations.SysTextJson
