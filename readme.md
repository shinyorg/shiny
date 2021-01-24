# Shiny for Xamarin & Windows 
<img src="art/logo.png" width="100" /> 

Shiny is a set of libraries designed to help make device services & backgrounding easy on Xamarin & UWP platforms (with more to come).

These samples make use of:
* Reactive Programming
* Dependency Injection

If you don't like any of these or you find them "complex" - Shiny is probably not for you.

If you are looking to just use one part of Shiny, such as notifications, this probably isn't for you.  Shiny brings:
* Handles all of the cruft like Permissions, main thread traversal, persistent storage and app restarts
* Your infrastructure to the background
* Gives a clean & testable API surface for your code

If you think Shiny is too heavy, try wiring all of these things up yourself with all of the above goals in mind. 

* [Documentation](https://shinylib.net)
* [Change Log](https://shinylib.net/blog)
* [Samples](https://github.com/shinyorg/shiny/samples) - Shows almost every single function point within Shiny
* Blog Posts
  * [Introducing Shiny](https://allancritchie.net/posts/introducingshiny)
  * [Background Jobs - Shiny Style](https://allancritchie.net/posts/shinyjobs)
  * [Settings in a New Light - Shiny Style](https://allancritchie.net/posts/shinysettings)
  * [Geofencing with a Pinch of Notifications - Shiny Style](https://allancritchie.net/posts/shiny-geofencing)
  * [Startup Tasks, Modules, and Stateful Delegates - Shiny Style](https://allancritchie.net/posts/shiny-di)
  * [Beacons - Shiny Style](https://allancritchie.net/posts/shiny-beacons)
 


## Builds

Branch|Status
------|------
Master|![Build](https://img.shields.io/github/checks-status/shinyorg/shiny/master?style=for-the-badge)|
Dev|![Build](https://img.shields.io/github/checks-status/shinyorg/shiny/dev?style=for-the-badge)|
PReview|![Build](https://img.shields.io/github/checks-status/shinyorg/shiny/preview?style=for-the-badge)|


## NuGet Packages

## Libraries

|Project|NuGet|Preview|Description|
|-------|-----|-----|-----------|
|Core + Generators | [![ShinyNugetShield]][ShinyNuget] | [![ShinyPreviewShield]][ShinyPreNuget] |
|Core | [![CoreNugetShield]][CoreNuget] | [![CorePreviewShield]][CorePreNuget] |
|Beacons | [![BeaconsNugetShield]][BeaconsNuget] | [![BeaconsPreviewShield]][BeaconsPreNuget] |
|BluetoothLE Client| [![BleNugetShield]][BleNuget] | [![BlePreviewShield]][BlePreNuget] |
|BluetoothLE Hosting| [![BleHostingNugetShield]][BleHostingNuget] | [![BleHostingPreviewShield]][BleHostingPreNuget] |
|Locations| [![LocationsNugetShield]][LocationsNuget] | [![LocationsPreviewShield]][LocationsPreNuget] |
|HTTP Transfers| [![HttpNugetShield]][HttpNuget] | [![HttpPreviewShield]][HttpPreNuget] |
|Sensors| [![SensorsNugetShield]][SensorsNuget] | [![SensorsPreviewShield]][SensorsPreNuget] |
|Notifications| [![NotificationsNugetShield]][NotificationsNuget] | [![NotificationsPreviewShield]][NotificationsPreNuget] |
|Push| [![PushNugetShield]][PushNuget] | [![PushPreviewShield]][PushPreNuget] |
|NFC| [![NfcNugetShield]][NfcNuget] | [![NfcPreviewShield]][NfcPreNuget] |


## Integrations
|Project|NuGet|Preview|Description|
|-------|-----|-------|-----------|
|Azure Notifications Push| [![AzureHubPushNugetShield]][AzureHubPushNuget] | [![AzureHubPushPreviewShield]][AzureHubPushPreNuget] | Push Integration for Azure Notification Hubs - Support for iOS, Android, & UWP
|Firebase Push| [![FirebasePushNugetShield]][FirebasePushNuget] | [![FirebasePushPreviewShield]][FirebasePushPreNuget] | Push Integration for Google Firebase - Support for iOS & Android
|SQLite| [![SqliteNugetShield]][SqliteNuget] | [![SqlitePreviewShield]][SqlitePreNuget] | Provides caching, logging, storage, & settings implementations
|AppCenter Logging| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterPreviewShield]][AppCenterPreNuget] | Log errors to AppCenter

## 3rd Party
|Project|NuGet|Repo|Description|
|-------|-----|----|-----------|
|Shiny.Prism|[![PrismNugetShield]][PrismNuget] | [GitHub](https://github.com/dansiegel/Prism.Container.Extensions) | Prism integration with Shiny maintained by Dan Siegel|
|Apizr.Integrations.Shiny| [![ApizrNugetShield]][ApizrNuget] | [GitHub](https://github.com/Respawnsive/Apizr) | Refit based web api client management, but resilient (retry, connectivity, cache, auth, log, priority, etc...)


## Contributors
* [Allan Ritchie](https://github.com/aritchie) - Project Lead
* [Emily Stanek](https://github.com/emilystanek) - Logo Designer
* [Dan Siegel](https://github.com/dansiegel) - Contributor
* [Keith Dahlby](https://twitter.com/dahlbyk) - Contributor

[ShinyNugetShield]: https://img.shields.io/nuget/v/Shiny.svg?style=for-the-badge
[ShinyNuget]: https://www.nuget.org/packages/Shiny/
[ShinyPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.svg?style=for-the-badge
[ShinyPreNuget]: https://www.nuget.org/packages/Shiny

[PrismNugetShield]: https://img.shields.io/nuget/v/Shiny.Prism.svg?style=for-the-badge
[PrismNuget]: https://www.nuget.org/packages/Shiny.Prism/

[ApizrNugetShield]: https://img.shields.io/nuget/v/Apizr.Integrations.Shiny.svg?style=for-the-badge
[ApizrNuget]: https://www.nuget.org/packages/Apizr.Integrations.Shiny/

[BeaconsNugetShield]: https://img.shields.io/nuget/v/Shiny.Beacons.svg?style=for-the-badge
[BeaconsNuget]: https://www.nuget.org/packages/Shiny.Beacons/
[BeaconsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Beacons.svg?style=for-the-badge
[BeaconsPreNuget]: https://www.nuget.org/packages/Shiny.Beacons

[CoreNugetShield]: https://img.shields.io/nuget/v/Shiny.Core.svg?style=for-the-badge
[CoreNuget]: https://www.nuget.org/packages/Shiny.Core/
[CorePreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Core.svg?style=for-the-badge
[CorePreNuget]: https://www.nuget.org/packages/Shiny.Core

[BleNugetShield]: https://img.shields.io/nuget/v/Shiny.BluetoothLE.svg?style=for-the-badge
[BleNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE/
[BlePreviewShield]: https://img.shields.io/nuget/vpre/Shiny.BluetoothLE.svg?style=for-the-badge
[BlePreNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE

[BleHostingNugetShield]: https://img.shields.io/nuget/v/Shiny.BluetoothLE.Hosting.svg?style=for-the-badge
[BleHostingNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE.Hosting/
[BleHostingPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.BluetoothLE.Hosting.svg?style=for-the-badge
[BleHostingPreNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE.Hosting

[LocationsNugetShield]: https://img.shields.io/nuget/v/Shiny.Locations.svg?style=for-the-badge
[LocationsNuget]: https://www.nuget.org/packages/Shiny.Locations/
[LocationsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Locations.svg?style=for-the-badge
[LocationsPreNuget]: https://www.nuget.org/packages/Shiny.Locations

[SensorsNugetShield]: https://img.shields.io/nuget/v/Shiny.Sensors.svg?style=for-the-badge
[SensorsNuget]: https://www.nuget.org/packages/Shiny.Sensors/
[SensorsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Sensors.svg?style=for-the-badge
[SensorsPreNuget]: https://www.nuget.org/packages/Shiny.Sensors

[HttpNugetShield]: https://img.shields.io/nuget/v/Shiny.Net.Http.svg?style=for-the-badge
[HttpNuget]: https://www.nuget.org/packages/Shiny.Net.Http/
[HttpPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Net.Http.svg?style=for-the-badge
[HttpPreNuget]: https://www.nuget.org/packages/Shiny.Net.Http

[NotificationsNugetShield]: https://img.shields.io/nuget/v/Shiny.Notifications.svg?style=for-the-badge
[NotificationsNuget]: https://www.nuget.org/packages/Shiny.Notifications/
[NotificationsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Notifications.svg?style=for-the-badge
[NotificationsPreNuget]: https://www.nuget.org/packages/Shiny.Notifications

[PushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.svg?style=for-the-badge
[PushNuget]: https://www.nuget.org/packages/Shiny.Push/
[PushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.svg?style=for-the-badge
[PushPreNuget]: https://www.nuget.org/packages/Shiny.Push

[NfcNugetShield]: https://img.shields.io/nuget/v/Shiny.Nfc.svg?style=for-the-badge
[NfcNuget]: https://www.nuget.org/packages/Shiny.Nfc/
[NfcPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Nfc.svg?style=for-the-badge
[NfcPreNuget]: https://www.nuget.org/packages/Shiny.Nfc

[SqliteNugetShield]: https://img.shields.io/nuget/v/Shiny.Integrations.Sqlite.svg?style=for-the-badge
[SqliteNuget]: https://www.nuget.org/packages/Shiny.Integrations.Sqlite/
[SqlitePreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Integrations.Sqlite.svg?style=for-the-badge
[SqlitePreNuget]: https://www.nuget.org/packages/Shiny.Integrations.Sqlite

[AppCenterNugetShield]: https://img.shields.io/nuget/v/Shiny.Logging.AppCenter.svg?style=for-the-badge
[AppCenterNuget]: https://www.nuget.org/packages/Shiny.Logging.AppCenter/
[AppCenterPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Logging.AppCenter.svg?style=for-the-badge
[AppCenterPreNuget]: https://www.nuget.org/packages/Shiny.Logging.AppCenter

[PushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.svg?style=for-the-badge
[PushNuget]: https://www.nuget.org/packages/Shiny.Push/
[PushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.svg?style=for-the-badge
[PushPreNuget]: https://www.nuget.org/packages/Shiny.Push

[AzureHubPushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.AzureNotificationHubs.svg?style=for-the-badge
[AzureHubPushNuget]: https://www.nuget.org/packages/Shiny.Push.AzureNotificationHubs/
[AzureHubPushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.AzureNotificationHubs.svg?style=for-the-badge
[AzureHubPushPreNuget]: https://www.nuget.org/packages/Shiny.Push.AzureNotificationHubs

[FirebasePushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.FirebaseMessaging.svg?style=for-the-badge
[FirebasePushNuget]: https://www.nuget.org/packages/Shiny.Push.FirebaseMessaging/
[FirebasePushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.FirebaseMessaging.svg?style=for-the-badge
[FirebasePushPreNuget]: https://www.nuget.org/packages/Shiny.Push.FirebaseMessaging
