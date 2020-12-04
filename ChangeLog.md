# Change Log

2.0.0
---

### Core
* [BREAKING] Jobs no longer return Task of bool, they now only return Task
* [BREAKING][Android] All previous monikers have been removed, only android 10+ targets are supported now, this includes only AndroidX compatibility
* [BREAKING] Caching has been removed.  Use Microsoft.Extensions.Caching
* [Fix][Settings] Nullable enums and set null to remove
* [FEATURE] Source Generators
    * Auto Startup - creates the Shiny startup for you based on what Shiny nugets you install.  Simply mark [assembly:Shiny.GenerateStartupAttribute] on the assembly where you want it built
    * Static classes - creates static classes for all the Shiny nugets you use - Simply add [assembly:Shiny.GenerateStaticClassesAttribute] where you want them created
    * Auto boilerplate for iOS and Android - Don't like creating things on AppDelegate or Android, this will auto-magically do it for you and wire up to your startup.

### BluetoothLE
* [BREAKING] The original library has been split into 2 separate libraries (Client - Shiny.BluetoothLE and Server - Shiny.BluetoothLE.Hosting) with some common ties
* [Client] You don't like RX - there are new async extensions to skip the RX

### Locations
* [BREAKING] IsListening is gone in favor of CurrentListener configuration
* [Feature][Android] Now uses foreground service to achieve fast "background" GPS
* [Feature] Minimum Distance is now supported on Android and iOS

### Notifications
* [BREAKING][Feature] Channels - this exists to fit with the Android channels model and iOS category model
* [Feature][Android] More Android specific configuration added
* [Feature][Android][Uwp] Persistent & Progress notifications

### Push
* Firebase implementation is now released for iOS and Android  (Shiny.Push.FirebaseMessaging)
* Amazon Web Services is available for testing
* OneSignal is available for testing

### NFC
* Full release

### Beacons
* [BREAKING] Beacons are now separated into 2 different injection points, services.UseBeaconMonitoring()/IBeaconMonitoringManager and services.UseBeaconRanging()/IBeaconRangingManager
* [Android] Now uses a foreground service to maintain a continuous scan when using beacon monitoring

### AppServices (NEW MODULES)
* [Shiny.TripTracking] - A new module for tracking run, walks, and drives!
* [Shiny.Locations.Sync] - Track background GPS and/or geofence events and uses best practices to ensure these events hit your server in a timely fashion
* [Shiny.DataSync] - Sends data to your server in a reliable fashion (downloads coming soon)


1.2.0 (SP1)
---

### Shiny.Core
* [Fix][iOS] BG Tasks job manager registration issue
 
### Shiny.Notifications
* [Fix][Android] Sound serialization was not working for scheduled notifications

### Shiny.Push & Shiny.Push.AzureNotificationHubs
* [BREAKING] RequestAccess with tags presented issues - there is now SetTags
* [Fix][Android] Token registration issues with azure notification hubs
* [Fix][Android] Ensure local notifications are registered

### Shiny.Integrations.Sqlite
* [Fix] Strings not saving properly

### Shiny.MediaSync (alpha) - NEW Module
* Initial Alpha release - Synchronizes photos, videos, and audio recordings to the server using Shiny best practices


1.2.0
---

### Shiny.Core
* [Enhancement][Jobs] Job arguments are now like other delegates
* [Enhancement][Jobs][iOS] Background processing JobManager is now smarter with how it deals with misconfiguration
* [Enhancement][Jobs] New Extension called RunJobAsTask which allows you to run a job function
* [Enhancement][Logging] Global logging parameters via Log.Properties
* [Fix][iOS] NSDictionary fixes
* [Fix][Cache] Fix await issue in memory cache
* [Fix][Settings][Android] Preferences are now private
* [Fix][Settings][iOS] Using Preferences with Shiny Settings could cause issues
* [BREAKING][Settings] KeysNotClear and Keys enumerable have been removed to make room for more platforms and simplify the API - some methods were moved to extensions

### Shiny.Locations
* [Enhancement][GPS] Multiple delegate registrations
* [Enhancement][Geofencing] Multiple delegate registrations
* [Fix][Motion Activity][Android] Android 10 permission request will now request starting the listener when available
* [Fix][GPS/Geofencing][Android] Properly check everything under Android 8.1
* [Fix][Geofencing][Android] StopAllMonitoring shouldn't error if there are no geofences

### Shiny.Notifications
* [Enhancement] Multiple delegate registrations
* [Enhancement][Android] Ability to set custom launch activity type
* [Fix][Android] Pending launch intent is always set regardless of category
* [Fix][UWP] Cancelling notifications was not removing the notification

### Shiny.Push
* [Enhancement] Multiple delegate registrations
* [Enhancement] Adds ability to see when token expires (if applicable, otherwise null)
* [Enhancement] Adds ability to register with tags, update tags, and see currently registered tags if the push mechanism supports it.  Check if IPushManager can be cast to IPushTagSupport

### Shiny.Push.AzureNotificationHubs
* [Enhancement] RequestAccess with new tags support

### Shiny.Locations.Sync (beta) - New Module
* This will sync geofence and GPS events to your backend utilizing best practices with background jobs

1.1.0
---

### Shiny.Core
* [Feature] App State Delegate - In your shiny startup, use services.AddAppState<YourAppStateDelegate> that inherits from IAppStateDelegate.  Watch for app start, foreground, & background
* [Feature] PowerManager now has property/inpc for IsEnergySavingEnabled that checks for ios low power, android doze, & uwp energy saving mode
* [Feature] Ability to run jobs on timers while the application is in the foreground or app state changes (starting, resuming, or backgrounding)
* [Enhancement][Jobs][iOS] Jobs - now uses iOS 13 background processing
* [Enhancement] Increased discoverability via new AppDelegate & Android app/activity extension methods.   Simply add the Shiny namespace and type this.Shiny to see all of the points you should be attaching
* [Enhancement][iOS][Android] Easier boilerplate setup
* [Enhancement][Android] JobManager.RunTask will now use wakeful locks to run tasks if available
* [Enhancement][Android] AndroidX support on android 10 targets - WorkManager replaces JobService under the hood
* [Enhancement] Message bus name-only void events
* [Enhancement] Connectivity now exposes cellular carrier
* [Enhancement] Power Manager now exposes energy saver mode detection
* [fix][breaking][uwp] UwpShinyHost.Init now requires the UWP application instance is passed as part of the arguments

### Shiny.Notifications
* [Enhancement][Breaking] New way to set notification sounds which allows you to use system sounds - Notification.CustomSoundFilePath has been removed
* [Enhancement][Android] AndroidX support on android 10 targets
* [Enhancement][Android] Use Big Text Style via AndroidOptions argument of notification
* [Enhancement][Android] Use Large Icons via AndroidOptions argument of notification

### Shiny.Locations
* [Fix][Geofences][Android] Status observable now works
* [Fix][Geofences][Android] Status check now includes GPS radio checks + permissions
* [Fix][GPS][BREAKING] RequestAccess, WhenStatusChanged, and GetCurrentStatus all now accept GpsRequest to increase the scope/accuracy of the necessary permission checks
* [Fix][GPS][Android] RequestAccess now checks for new Android 10 permission ACCESS_BACKGROUND_LOCATION
* [Fix][Motion Activity][Android] RequestAccess now checks for new ANdroid permission ACTIVITY_RECOGNITION
* [Feature] Added full background GPS geofence module - it is not friendly to battery
* [Feature][Android] If Google Play Services is not available, we switch to the GPS direct module

### Shiny.Integrations.XamarinForms
* [Feature] Less boilerplate to wire into XF
* [Feature] Instead of ShinyHost.Resolve, you can now use the XF DependencyService.Get

### [NEW] Shiny.Push
* BETA - while this does work with the push notification mechanics, its primary purpose is to provide
    * A wrapper for all push messaging systems - if appcenter dies today, you can be on azure notification hubs tomorrow with 1 line of code change
    * A consistent event structure to work with in the background (delegates - like all other shiny services)

### [NEW] Shiny.Push.AzureNotificationHubs
* Wraps the Azure Notification Hubs in an injectable/testable interface (and gives you comfort of being able to swap out mechanisms easily)

### [NEW] Shiny.Push.FirebaseMessaging
* Wraps the Firebase messaging for Android & iOS

1.0.0
---
Initial Release for
* Shiny.Core
* Shiny.Locations
* Shiny.Notifications
* Shiny.Net.Http
* Shiny.Sensors
* Shiny.SpeechRecognition
* Shiny.Testing