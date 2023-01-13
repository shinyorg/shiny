# Shiny Samples

## Sample (Sample.Maui)
A kitchen sink using all of the support libraries that don't require special external entitlements from Apple (like Push or NFC)

## MAUI Push (Sample.Push.Maui)

* Open Sample.Push.Maui csproj
    * Change <PushProvider> to native, firebase, or azure

* Platform Support

|Messaging Platform|iOS|Mac Catalyst|Android|
|------------------|---|------------|-------|
|Native|Yes|Yes|Yes|
|Azure|Yes|Yes|Yes|
|Firebase|Yes|No|Yes|

### Azure Notification Hubs
* Edit appsettings.json and add the appropriate values to the AzureNotificationHubs section
* Send a payload to an active app with `content-available:1` and then it will be received via `DidReceiveRemoteNotification` of AppDelegate class.
```json
{"aps":{
  "alert":"Notification Hub test notification",
  "content-available": 1
}}
```

### Firebase
* Edit appsettings.json - add the appropriate values to the Firebase section - The AppId from firebase will be different between Android & iOS
* Shiny does support embedded configuration (google-services.json & GoogleService-Info.plist), but it more complex to setup

## Blazor (Sample.Blazor)
Currently, a vast majority of the APIs that are coming to the web (ie. BluetoothLE, Notifications, & event GPS) are still experimental.  Thus, you need to enable experimental features for Shiny Blazor to work