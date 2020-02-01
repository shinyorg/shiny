# Change Log

## 1.0.1

* Shiny.Core
    * [Enhancement][Android] AndroidX support on android 10 targets - WorkManager replaces JobService under the hood

* Shiny.Notifications
    * [Enhancement][Breaking] New way to set notification sounds which allows you to use system sounds - Notification.CustomSoundFilePath has been removed
    * [Enhancement][Android] AndroidX support on android 10 targets

* Shiny.Locations
    * [Fix][Geofences][Android] Status observable now works
    * [Fix][Geofences][Android] Status check now includes GPS radio checks + permissions
    * [Fix][GPS][BREAKING] RequestAccess, WhenStatusChanged, and GetCurrentStatus all now accept GpsRequest to increase the scope/accuracy of the necessary permission checks
    * [Fix][GPS][Android] RequestAccess now checks for new Android 10 permission for ACCESS_BACKGROUND_LOCATION

## 1.0.0
Initial Release for
* Shiny.Core
* Shiny.Locations
* Shiny.Notifications
* Shiny.Net.Http
* Shiny.Sensors
* Shiny.SpeechRecognition
* Shiny.Testing