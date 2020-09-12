Title: Push
Description: One push provider to rule them all!
---

# Push
Push notifications come in so many flavours these days.  The community needed one way of doing things, but the ability to swap providers in and out at will.  The reason, push providers, aside from native have been dropping
like flies the last several years.  The latest to go was AppCenter which it turns out the Xamarin community was heavily invested in.  


## Providers
|Provider|iOS|Android|UWP|Server Setup|NuGet|
|---|---|---|---|---|---|---|
|Native|10+|6+|16299+|||
|Azure Notification Hubs|10+|6+|16299+|[Link](https://docs.microsoft.com/en-ca/azure/notification-hubs/)||
|Firebase|10+|6||||
|OneSignal|10+|6||||
|Amazon|10+|6|||

## Registration
Look to each appropriate provider to see setups for each.  

snippet: PushStartup.cs

All providers use the native implementations on the platform to some degree, as such, you will always need to call

snippet: PushPermissions.cs

## Background Delegate
snippet: PushDelegate.cs


## Foreground Monitoring
It is quite often that you may want to change data due to a silent notification being received.  This is similar to watching a SignalR broadcast, but with observables because RX is awesome and Shiny dies on the RX hill!

snippet: PushForeground.cs

## Additional Features
Like other modules in Shiny, there are certain providers that support additional feature sets.  Push really only has 1 extra, tagging.

The following providers, support tagging
* Azure Notification Hubs
* Firebase

In order to safely support tagging without the need for constantly feature flag or type checking, the following extension methods exist to make life easy

snippet: PushExtensions.cs