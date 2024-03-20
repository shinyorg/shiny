# Shiny Samples

These samples make use great open source frameworks like [Prism](https://prismlibrary.com) & ReactiveUI (https://reactiveui.net) as well
as principles like dependency injection and reactive extensions... much like Shiny itself.  If you find these complex or you simply don't like them, don't complain - the library wasn't built for you...
remember - this is not a product! 

## Sample (Sample.Maui)
A kitchen sink using all of the support libraries that don't require special external entitlements from Apple (like Push).
This sample also uses Shiny.Framework (https://github.com/shinyorg/framework) which has helpers around ReactiveUI & Prism.

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

### Firebase Messaging
* Edit appsettings.apple.json & appsettings.android.json and enter the appropriate firebase values in each config file

## Blazor (Sample.Blazor)
Currently, a vast majority of the APIs that are coming to the web (ie. BluetoothLE, Notifications, & event GPS) are still experimental.
This is a testing platform not meant for general consumption.  Don't expect much

## Setup BluetoothLE Tests
* Start Sample.MAUI - go to menu - Tap on "BLE Host for Unit Tests"
* Start Unit Test Project & Run BLE Tests

## Setup HTTP Transfer Tests
First get a public URL for your locally running project that you can use for testing.

Install Tunnelmole (an open source tunneling solution) with one of these options:
- NPM: `npm install -g tunnelmole`
- Linux: `curl -s https://tunnelmole.com/sh/install-linux.sh | sudo bash`
- Mac: `curl -s https://tunnelmole.com/sh/install-mac.sh --output install-mac.sh && sudo bash install-mac.sh`
- Windows: Install with NPM, or if you don't have NodeJS installed download the `exe` file for Windows [here](https://tunnelmole.com/downloads/tmole.exe) and put it somewhere in your PATH.
* Alternatively, to use ngrok (a popular closed source tunneling solution) first [download the binary for your platform](https://ngrok.com/download) then run "ngrok http https://localhost:7133"

* In Shiny.Tests, go to appsettings.json and update HttpTestsUrl to the tunnelmole or ngrok url
* Setup launch targets for the Samples.Api and the Shiny.Tests project
* Run HTTP Tests
