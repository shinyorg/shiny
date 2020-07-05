# Shiny for Xamarin & Windows 
<img src="art/logo.png" width="100" /> 

Shiny is a set of libraries designed to help make device services & backgrounding easy on Xamarin & UWP platforms (with more to come).

[Change Log - July 5, 2020](https://github.com/shinyorg/shiny/blob/master/ChangeLog.md)

* [Samples](https://github.com/shinyorg/shinysamples) - Shows almost every single function point within Shiny
* [Beautiful Docs](https://shinylib.net) are in the works - for now, take a look here:
  * [Introducing Shiny](https://allancritchie.net/posts/introducingshiny)
  * [Shiny 1.1](https://allancritchie.net/posts/shiny11)
  * [Background Jobs - Shiny Style](https://allancritchie.net/posts/shinyjobs)
  * [Settings in a New Light - Shiny Style](https://allancritchie.net/posts/shinysettings)
  * [Geofencing with a Pinch of Notifications - Shiny Style](https://allancritchie.net/posts/shiny-geofencing)
  * [Startup Tasks, Modules, and Stateful Delegates - Shiny Style](https://allancritchie.net/posts/shiny-di)
  * [Beacons - Shiny Style](https://allancritchie.net/posts/shiny-beacons)
  * [Easy Mode - NO DI](https://allancritchie.net/posts/shiny-easymode)


## Builds

Branch|Status
------|------
Master|[![Build status](https://dev.azure.com/shinylib/shiny/_apis/build/status/Build?branchName=master)](https://dev.azure.com/shinylib/shiny/_build/latest?definitionId=1)
Dev|[![Build status](https://dev.azure.com/shinylib/shiny/_apis/build/status/Build?branchName=dev)](https://dev.azure.com/shinylib/shiny/_build/latest?definitionId=1)|


## NuGet Packages

Shiny official releases are available on NuGet. For early test builds, you can also use the beta MyGet feed.

To use the beta MyGet feed, add `https://www.myget.org/F/acrfeed/api/v3/index.json` as a package source to Visual Studio


## Libraries

|Project|NuGet|MyGet|Description|
|-------|-----|-----|-----------|
|Core | [![CoreNugetShield]][CoreNuget] | [![CoreMygetShield]][CoreMyget] |
|Beacons | [![BeaconsNugetShield]][BeaconsNuget] | [![BeaconsMygetShield]][BeaconsMyget] |
|BluetoothLE Client| [![BleNugetShield]][BleNuget] | [![BleMygetShield]][BleMyget] |
|BluetoothLE Hosting| [![BleHostingNugetShield]][BleHostingNuget] | [![BleHostingMygetShield]][BleHostingMyget] |
|Locations| [![LocationsNugetShield]][LocationsNuget] | [![LocationsMygetShield]][LocationsMyget] |
|HTTP Transfers| [![HttpNugetShield]][HttpNuget] | [![HttpMygetShield]][HttpMyget] |
|Sensors| [![SensorsNugetShield]][SensorsNuget] | [![SensorsMygetShield]][SensorsMyget] |
|Notifications| [![NotificationsNugetShield]][NotificationsNuget] | [![NotificationsMygetShield]][NotificationsMyget] |
|Push| [![PushNugetShield]][PushNuget] | [![PushMygetShield]][PushMyget] |
|NFC| [![NfcNugetShield]][NfcNuget] | [![NfcMygetShield]][NfcMyget] |


## App Services
|Project|NuGet|MyGet|Description|
|-------|-----|-----|-----------|
|Location Sync| [![LocationSyncNugetShield]][LocationSyncNuget] | [![LocationSyncMygetShield]][LocationSyncMyget] | Sync Geofence & GPS data to the server using Shiny best practices
|Trip Tracker| [![TripTrackerNugetShield]][TripTrackerNuget] | [![TripTrackerMygetShield]][TripTrackerMyget] | Tracks your trips for automotive, cycling, running, and/or walking - includes the distance and all GPS coordinates throughout the trip
|Media Sync| [![MediaSyncNugetShield]][MediaSyncNuget] | [![MediaSyncMygetShield]][MediaSyncMyget] | Sync your Android & iOS media gallery to the server using Shiny best practices

## Integrations
|Project|NuGet|MyGet|Description|
|-------|-----|-----|-----------|
|Azure Notifications Push| [![AzureHubPushNugetShield]][AzureHubPushNuget] | [![AzureHubPushMygetShield]][AzureHubPushMyget] | Push Integration for Azure Notification Hubs - Support for iOS, Android, & UWP
|Firebase Push| [![FirebasePushNugetShield]][FirebasePushNuget] | [![FirebasePushMygetShield]][FirebasePushMyget] | Push Integration for Google Firebase - Support for iOS & Android
|SQLite| [![SqliteNugetShield]][SqliteNuget] | [![SqliteMygetShield]][SqliteMyget] | Provides caching, logging, storage, & settings implementations
|AppCenter Logging| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterMygetShield]][AppCenterMyget] | Log errors to AppCenter

## 3rd Party
|Project|NuGet|Repo|Description|
|-------|-----|----|-----------|
|Shiny.Prism|[![PrismNugetShield]][PrismNuget] | [GitHub](https://github.com/dansiegel/Prism.Container.Extensions) | Prism integration with Shiny maintained by Dan Siegel|

## Roadmap
* [Considering] VPN Client for Android, iOS, & UWP 
* [Considering] Exposure Notification API for iOS & Android
* [In-Progress] New Hosting Model based on Microsoft.Extensions.Hosting
* [Considering] BluetoothLE Hosting Model similar to SignalR
* [Considering] Boilerplate Source Generator
* [Considering] BluetoothLE Client Generator (Similar to Refit for HTTP Client)

## Contributors
* [Allan Ritchie](https://github.com/aritchie) - Project Lead
* [Emily Stanek](https://github.com/emilystanek) - Logo Designer
* [Dan Siegel](https://github.com/dansiegel) - Contributor
* [Keith Dahlby](https://twitter.com/dahlbyk) - Contributor

[PrismNugetShield]: https://img.shields.io/nuget/v/Shiny.Prism.svg
[PrismNuget]: https://www.nuget.org/packages/Shiny.Prism/

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

[BleHostingNugetShield]: https://img.shields.io/nuget/v/Shiny.BluetoothLE.Hosting.svg
[BleHostingNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE.Hosting/
[BleHostingMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.BluetoothLE.Hosting.svg
[BleHostingMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.BluetoothLE.Hosting

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

[PushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.svg
[PushNuget]: https://www.nuget.org/packages/Shiny.Push/
[PushMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Push.svg
[PushMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Push

[NfcNugetShield]: https://img.shields.io/nuget/v/Shiny.Nfc.svg
[NfcNuget]: https://www.nuget.org/packages/Shiny.Nfc/
[NfcMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Nfc.svg
[NfcMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Nfc

[SqliteNugetShield]: https://img.shields.io/nuget/v/Shiny.Integrations.Sqlite.svg
[SqliteNuget]: https://www.nuget.org/packages/Shiny.Integrations.Sqlite/
[SqliteMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Integrations.Sqlite.svg
[SqliteMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Integrations.Sqlite

[AppCenterNugetShield]: https://img.shields.io/nuget/v/Shiny.Logging.AppCenter.svg
[AppCenterNuget]: https://www.nuget.org/packages/Shiny.Logging.AppCenter/
[AppCenterMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Logging.AppCenter.svg
[AppCenterMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Logging.AppCenter

[PushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.svg
[PushNuget]: https://www.nuget.org/packages/Shiny.Push/
[PushMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Push.svg
[PushMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Push

[AzureHubPushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.AzureNotificationHubs.svg
[AzureHubPushNuget]: https://www.nuget.org/packages/Shiny.Push.AzureNotificationHubs/
[AzureHubPushMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Push.AzureNotificationHubs.svg
[AzureHubPushMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Push.AzureNotificationHubs

[FirebasePushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.FirebaseMessaging.svg
[FirebasePushNuget]: https://www.nuget.org/packages/Shiny.Push.FirebaseMessaging/
[FirebasePushMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Push.FirebaseMessaging.svg
[FirebasePushMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Push.FirebaseMessaging

[LocationSyncNugetShield]: https://img.shields.io/nuget/v/Shiny.Locations.Sync.svg
[LocationSyncNuget]: https://www.nuget.org/packages/Shiny.Locations.Sync/
[LocationSyncMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.Locations.Sync.svg
[LocationSyncMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.Locations.Sync

[MediaSyncNugetShield]: https://img.shields.io/nuget/v/Shiny.MediaSync.svg
[MediaSyncNuget]: https://www.nuget.org/packages/Shiny.MediaSync/
[MediaSyncMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.MediaSync.svg
[MediaSyncMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.MediaSync

[TripTrackerNugetShield]: https://img.shields.io/nuget/v/Shiny.TripTracker.svg
[TripTrackerNuget]: https://www.nuget.org/packages/Shiny.TripTracker/
[TripTrackerMygetShield]: https://img.shields.io/myget/acrfeed/vpre/Shiny.TripTracker.svg
[TripTrackerMyget]: https://www.myget.org/feed/acrfeed/package/nuget/Shiny.TripTracker