# Shiny Samples

## Sample
A kitchen sink using all of the support libraries that don't require special external entitlements from Apple (like Push or NFC)

## MAUI Push

* Open Sample.Push.Maui csproj
    * Change <PushProvider> to native, firebase, or azure

* Platform Support

|Messaging Platform|iOS|Mac Catalyst|Android|
|------------------|---|------------|-------|
|Native|Yes|Yes|Yes|
|Azure|Yes|Yes|Yes|
|Firebase|Yes|No|Yes|

## Azure Notification Hubs
* Edit appsettings.json and add the appropriate values to the AzureNotificationHubs section

### Firebase
* Edit appsettings.json - add the appropriate values to the Firebase section - The AppId from firebase will be different between Android & iOS
* Shiny does support embedded configuration (google-services.json & GoogleService-Info.plist), but it more complex to setup