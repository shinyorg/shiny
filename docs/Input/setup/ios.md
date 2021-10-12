Title: Platform - iOS
Order: 2
Xref: ios
---
# iOS

## AppDelegate

|Method|Shiny Wire-In|Purpose|
|------|-------------|-------|
ReceivedRemoteNotification|ShinyDidReceiveRemoteNotification|Used by Shiny.Push and some app services
DidReceiveRemoteNotification|ShinyDidReceiveRemoteNotification|Used by Shiny.Push and some app services
RegisteredForRemoteNotifications|ShinyRegisteredForRemoteNotifications|Used by Shiny.Push and some app services
FailedToRegisterForRemoteNotifications|ShinyFailedToRegisterForRemoteNotifications|Used by Shiny.Push and some app services
PerformFetch|ShinyPerformFetch|Used by Shiny Core mostly for jobs, but there are lots of places in Shiny where Jobs are used
HandleEventsForBackgroundUrl|ShinyHandleEventsForBackgroundUrl|Used by Shiny.Net.Http for background transfers

This is a fully "hooked" appdelegate to 

```csharp
using System;
using Shiny;
using Foundation;
using UIKit;

public partial class AppDelegate
{
    // special method called by shiny if exists
    partial void OnPreFinishedLaunching(UIApplication app, NSDictionary options);

    // special method called by shiny if exists
    partial void OnPostFinishedLaunching(UIApplication app, NSDictionary options);


    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        this.OnPreFinishedLaunching(app, options);
        this.ShinyFinishedLaunching(new Samples.SampleStartup());
        this.LoadApplication(new Samples.App());        
        global::Xamarin.Forms.Forms.Init();
        
        global::XF.Material.iOS.Material.Init();
        this.OnPostFinishedLaunching(app, options);
        return base.FinishedLaunching(app, options);
    }


    /*
    THESE METHODS ARE ONLY NEEDED IF A PUSH PACKAGE IS USED
    */
    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) 
        => this.ShinyRegisteredForRemoteNotifications(deviceToken);

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) 
        => this.ShinyFailedToRegisterForRemoteNotifications(error);

    public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler) 
        => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);

    /*
    THIS METHOD IS NEEDED BY MOST OF SHINY - ALWAYS ADD IT
    */
    public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) 
        => this.ShinyPerformFetch(completionHandler);

    /*
    THIS METHOD IS NEEEDED IF YOU ARE USING SHINY.NET.HTTP
    */
    public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler) 
        => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);
}
```

### Info.plist

```xml
<!-- required for location and beacons -->
<key>NSLocationAlwaysUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>

<key>NSMotionUsageDescription</key>
<string>Say something clever here</string>

<key>UIBackgroundModes</key>
<array>
    <string>location</string>
    <string>fetch</string>
    <string>processing</string>
</array>

<key>BGTaskSchedulerPermittedIdentifiers</key>
<array>
    <string>com.shiny.job</string>
    <string>com.shiny.jobpower</string>
    <string>com.shiny.jobnet</string>
    <string>com.shiny.jobpowernet</string>
</array>
