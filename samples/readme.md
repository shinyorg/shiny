## Shiny Samples

These samples make use of:
* Prism
* ReactiveUI & RX Principles
* Heavy use of dependency injection

If you are looking to just use one part of Shiny, such as notifications, this probably isn't for you.  Shiny brings:
* Handles all of the cruft like Permissions, main thread traversal, and app restarts
* Your infrastructure to the background
* Gives a clean & testable API surface for your code

## Compiling on iOS
NFC & Push Notifications are enabled in the info.plist which means you need a custom provisioning profile (or you have to disable these before deploying to your device)