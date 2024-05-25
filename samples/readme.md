# Shiny Samples

These samples make use great open source frameworks like [Prism](https://prismlibrary.com) & ReactiveUI (https://reactiveui.net) as well
as principles like dependency injection and reactive extensions... much like Shiny itself.  

If you find these complex OR you simply don't like them, don't complain - the library wasn't built for you...
remember - this is not a product! 

[For the push sample, please GO HERE](https://github.com/shinyorg/pushtester)

## Sample (Sample.Maui)
A kitchen sink using almost all of Shiny that don't require special external entitlements from Apple (like Push).
This sample also uses Shiny.Framework (https://github.com/shinyorg/framework) which has helpers around ReactiveUI & Prism.

## Blazor (Sample.Blazor)
Currently, a vast majority of the APIs that are coming to the web (ie. BluetoothLE, Notifications, & event GPS) are still experimental.
This is a testing platform not meant for general consumption.  Don't expect much

## Setup BluetoothLE Tests
* Start Sample.MAUI - go to menu - Tap on "BLE Host for Unit Tests"
* Start Unit Test Project & Run BLE Tests

## Setup HTTP Transfer Tests
* IN command line - run "ngrok http https://localhost:7133"
* In Shiny.Tests, go to appsettings.json and update HttpTestsUrl to the ngrok url
* Setup launch targets for the Samples.Api and the Shiny.Tests project
* Run HTTP Tests