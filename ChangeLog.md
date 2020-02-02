# Change Log

## 1.1.0

* Shiny.Core
    * [Feature] App State Delegate - In your shiny startup, use services.AddAppState<YourAppStateDelegate> that inherits from IAppStateDelegate.  Watch for app start, background, foreground, & termination
    * [Enhancement][iOS][Android] Easier boilerplate setup
    * [Enhancement][Android] AndroidX support on android 10 targets - WorkManager replaces JobService under the hood

* Shiny.Notifications
    * [Enhancement][Breaking] New way to set notification sounds which allows you to use system sounds - Notification.CustomSoundFilePath has been removed
    * [Enhancement][Android] AndroidX support on android 10 targets

* Shiny.Locations
    * [Fix][Geofences][Android] Status observable now works
    * [Fix][Geofences][Android] Status check now includes GPS radio checks + permissions
    * [Fix][GPS][BREAKING] RequestAccess, WhenStatusChanged, and GetCurrentStatus all now accept GpsRequest to increase the scope/accuracy of the necessary permission checks
    * [Fix][GPS][Android] RequestAccess now checks for new Android 10 permission for ACCESS_BACKGROUND_LOCATION

* Shiny.Integrations.XamarinForms
    * [Feature] Less boilerplate to wire into XF
    * [Feature] You can now use the DependencyService to Shiny services in your viewmodels

## 1.0.0
Initial Release for
* Shiny.Core
* Shiny.Locations
* Shiny.Notifications
* Shiny.Net.Http
* Shiny.Sensors
* Shiny.SpeechRecognition
* Shiny.Testing