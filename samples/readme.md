## Shiny Samples

These samples make use of:
* Prism
* ReactiveUI & RX Principles
* Heavy use of dependency injection

If you don't like any of these or you find them "complex" - Shiny is probably not for you.

If you are looking to just use one part of Shiny, such as notifications, this probably isn't for you.  Shiny brings:
* Handles all of the cruft like Permissions, main thread traversal, and app restarts
* Your infrastructure to the background
* Gives a clean & testable API surface for your code


## Builds

OS|Status
--|------
Android|[![Build status](https://dev.azure.com/shinylib/Shiny/_apis/build/status/Android%20Sample)](https://dev.azure.com/shinylib/Shiny/_build/latest?definitionId=17)
iOS|[![Build status](https://dev.azure.com/shinylib/Shiny/_apis/build/status/iOS%20Sample)](https://dev.azure.com/shinylib/Shiny/_build/latest?definitionId=16)
Tizen|[![Build status](https://dev.azure.com/shinylib/Shiny/_apis/build/status/Tizen%20Mobile)](https://dev.azure.com/shinylib/Shiny/_build/latest?definitionId=12)
UWP|[![Build status](https://dev.azure.com/shinylib/Shiny/_apis/build/status/Sample%20UWP)](https://dev.azure.com/shinylib/Shiny/_build/latest?definitionId=9)


## Compiling on iOS
NFC & Push Notifications are enabled in the info.plist which means you need a custom provisioning profile (or you have to disable these before deploying to your device)