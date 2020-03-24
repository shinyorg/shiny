﻿# Change Log

1.2.0
---

### Shiny.Core
* [Enhancement][iOS] Jobs - now uses iOS 13 background processing

1.1.0
---

### Shiny.Core
* [Feature] App State Delegate - In your shiny startup, use services.AddAppState<YourAppStateDelegate> that inherits from IAppStateDelegate.  Watch for app start, foreground, & background
* [Feature] PowerManager now has property/inpc for IsEnergySavingEnabled that checks for ios low power, android doze, & uwp energy saving mode
* [Feature] Ability to run jobs on timers while the application is in the foreground or app state changes (starting, resuming, or backgrounding)
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