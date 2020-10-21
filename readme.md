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


[Change Log - October 20, 2020](https://github.com/shinyorg/shiny/blob/master/ChangeLog.md)

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
Master|![Build](https://github.com/shinyorg/shiny/workflows/Build/badge.svg)|
Dev|![Build](https://github.com/shinyorg/shiny/workflows/Build/badge.svg?branch=dev)|


## NuGet Packages

## Libraries

|Project|NuGet|Preview|Description|
|-------|-----|-----|-----------|
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


## App Services
|Project|NuGet|MyGet|Description|
|-------|-----|-----|-----------|
|Location Sync| [![LocationSyncNugetShield]][LocationSyncNuget] | [![LocationSyncPreviewShield]][LocationSyncPreNuget] | Sync Geofence & GPS data to the server using Shiny best practices
|Trip Tracker| [![TripTrackerNugetShield]][TripTrackerNuget] | [![TripTrackerPreviewShield]][TripTrackerPreNuget] | Tracks your trips for automotive, cycling, running, and/or walking - includes the distance and all GPS coordinates throughout the trip
|Media Sync| [![MediaSyncNugetShield]][MediaSyncNuget] | [![MediaSyncPreviewShield]][MediaSyncPreNuget] | Sync your Android & iOS media gallery to the server using Shiny best practices
|Data Sync| [![DataSyncNugetShield]][DataSyncNuget] | [![DataSyncPreviewShield]][DataSyncPreNuget] | Sync your data to the server using Shiny best practices

## Integrations
|Project|NuGet|MyGet|Description|
|-------|-----|-----|-----------|
|Azure Notifications Push| [![AzureHubPushNugetShield]][AzureHubPushNuget] | [![AzureHubPushPreviewShield]][AzureHubPushPreNuget] | Push Integration for Azure Notification Hubs - Support for iOS, Android, & UWP
|Firebase Push| [![FirebasePushNugetShield]][FirebasePushNuget] | [![FirebasePushPreviewShield]][FirebasePushPreNuget] | Push Integration for Google Firebase - Support for iOS & Android
|Amazon Web Services| [![AwsPushNugetShield]][AwsPushNuget] | [![AwsPushPreviewShield]][AwsPushPreNuget] | Push Integration for AWS - Support for iOS & Android
|SQLite| [![SqliteNugetShield]][SqliteNuget] | [![SqlitePreviewShield]][SqlitePreNuget] | Provides caching, logging, storage, & settings implementations
|AppCenter Logging| [![AppCenterNugetShield]][AppCenterNuget] | [![AppCenterPreviewShield]][AppCenterPreNuget] | Log errors to AppCenter

## 3rd Party
|Project|NuGet|Repo|Description|
|-------|-----|----|-----------|
|Shiny.Prism|[![PrismNugetShield]][PrismNuget] | [GitHub](https://github.com/dansiegel/Prism.Container.Extensions) | Prism integration with Shiny maintained by Dan Siegel|


## Contributors
* [Allan Ritchie](https://github.com/aritchie) - Project Lead
* [Emily Stanek](https://github.com/emilystanek) - Logo Designer
* [Dan Siegel](https://github.com/dansiegel) - Contributor
* [Keith Dahlby](https://twitter.com/dahlbyk) - Contributor

[PrismNugetShield]: https://img.shields.io/nuget/v/Shiny.Prism.svg
[PrismNuget]: https://www.nuget.org/packages/Shiny.Prism/

[BeaconsNugetShield]: https://img.shields.io/nuget/v/Shiny.Beacons.svg
[BeaconsNuget]: https://www.nuget.org/packages/Shiny.Beacons/
[BeaconsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Beacons.svg
[BeaconsPreNuget]: https://www.nuget.org/packages/Shiny.Beacons

[CoreNugetShield]: https://img.shields.io/nuget/v/Shiny.Core.svg
[CoreNuget]: https://www.nuget.org/packages/Shiny.Core/
[CorePreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Core.svg
[CorePreNuget]: https://www.nuget.org/packages/Shiny.Core

[BleNugetShield]: https://img.shields.io/nuget/v/Shiny.BluetoothLE.svg
[BleNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE/
[BlePreviewShield]: https://img.shields.io/nuget/vpre/Shiny.BluetoothLE.svg
[BlePreNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE

[BleHostingNugetShield]: https://img.shields.io/nuget/v/Shiny.BluetoothLE.Hosting.svg
[BleHostingNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE.Hosting/
[BleHostingPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.BluetoothLE.Hosting.svg
[BleHostingPreNuget]: https://www.nuget.org/packages/Shiny.BluetoothLE.Hosting

[LocationsNugetShield]: https://img.shields.io/nuget/v/Shiny.Locations.svg
[LocationsNuget]: https://www.nuget.org/packages/Shiny.Locations/
[LocationsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Locations.svg
[LocationsPreNuget]: https://www.nuget.org/packages/Shiny.Locations

[SensorsNugetShield]: https://img.shields.io/nuget/v/Shiny.Sensors.svg
[SensorsNuget]: https://www.nuget.org/packages/Shiny.Sensors/
[SensorsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Sensors.svg
[SensorsPreNuget]: https://www.nuget.org/packages/Shiny.Sensors

[HttpNugetShield]: https://img.shields.io/nuget/v/Shiny.Net.Http.svg
[HttpNuget]: https://www.nuget.org/packages/Shiny.Net.Http/
[HttpPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Net.Http.svg
[HttpPreNuget]: https://www.nuget.org/packages/Shiny.Net.Http

[NotificationsNugetShield]: https://img.shields.io/nuget/v/Shiny.Notifications.svg
[NotificationsNuget]: https://www.nuget.org/packages/Shiny.Notifications/
[NotificationsPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Notifications.svg
[NotificationsPreNuget]: https://www.nuget.org/packages/Shiny.Notifications

[PushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.svg
[PushNuget]: https://www.nuget.org/packages/Shiny.Push/
[PushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.svg
[PushPreNuget]: https://www.nuget.org/packages/Shiny.Push

[NfcNugetShield]: https://img.shields.io/nuget/v/Shiny.Nfc.svg
[NfcNuget]: https://www.nuget.org/packages/Shiny.Nfc/
[NfcPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Nfc.svg
[NfcPreNuget]: https://www.nuget.org/packages/Shiny.Nfc

[SqliteNugetShield]: https://img.shields.io/nuget/v/Shiny.Integrations.Sqlite.svg
[SqliteNuget]: https://www.nuget.org/packages/Shiny.Integrations.Sqlite/
[SqlitePreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Integrations.Sqlite.svg
[SqlitePreNuget]: https://www.nuget.org/packages/Shiny.Integrations.Sqlite

[AppCenterNugetShield]: https://img.shields.io/nuget/v/Shiny.Logging.AppCenter.svg
[AppCenterNuget]: https://www.nuget.org/packages/Shiny.Logging.AppCenter/
[AppCenterPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Logging.AppCenter.svg
[AppCenterPreNuget]: https://www.nuget.org/packages/Shiny.Logging.AppCenter

[PushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.svg
[PushNuget]: https://www.nuget.org/packages/Shiny.Push/
[PushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.svg
[PushPreNuget]: https://www.nuget.org/packages/Shiny.Push

[AwsPushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.AwsSns.svg
[AwsPushNuget]: https://www.nuget.org/packages/Shiny.Push.AwsSns/
[AwsPushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.AwsSns.svg
[AwsPushPreNuget]: https://www.nuget.org/packages/Shiny.Push.AwsSns

[AzureHubPushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.AzureNotificationHubs.svg
[AzureHubPushNuget]: https://www.nuget.org/packages/Shiny.Push.AzureNotificationHubs/
[AzureHubPushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.AzureNotificationHubs.svg
[AzureHubPushPreNuget]: https://www.nuget.org/packages/Shiny.Push.AzureNotificationHubs

[FirebasePushNugetShield]: https://img.shields.io/nuget/v/Shiny.Push.FirebaseMessaging.svg
[FirebasePushNuget]: https://www.nuget.org/packages/Shiny.Push.FirebaseMessaging/
[FirebasePushPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Push.FirebaseMessaging.svg
[FirebasePushPreNuget]: https://www.nuget.org/packages/Shiny.Push.FirebaseMessaging

[LocationSyncNugetShield]: https://img.shields.io/nuget/v/Shiny.Locations.Sync.svg
[LocationSyncNuget]: https://www.nuget.org/packages/Shiny.Locations.Sync/
[LocationSyncPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.Locations.Sync.svg
[LocationSyncPreNuget]: https://www.nuget.org/packages/Shiny.Locations.Sync

[MediaSyncNugetShield]: https://img.shields.io/nuget/v/Shiny.MediaSync.svg
[MediaSyncNuget]: https://www.nuget.org/packages/Shiny.MediaSync/
[MediaSyncPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.MediaSync.svg
[MediaSyncPreNuget]: https://www.nuget.org/packages/Shiny.MediaSync

[DataSyncNugetShield]: https://img.shields.io/nuget/v/Shiny.DataSync.svg
[DataSyncNuget]: https://www.nuget.org/packages/Shiny.DataSync/
[DataSyncPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.DataSync.svg
[DataSyncPreNuget]: https://www.nuget.org/packages/Shiny.DataSync

[TripTrackerNugetShield]: https://img.shields.io/nuget/v/Shiny.TripTracker.svg
[TripTrackerNuget]: https://www.nuget.org/packages/Shiny.TripTracker/
[TripTrackerPreviewShield]: https://img.shields.io/nuget/vpre/Shiny.TripTracker.svg
[TripTrackerPreNuget]: https://www.nuget.org/packages/Shiny.TripTracker