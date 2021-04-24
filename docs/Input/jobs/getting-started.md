# Getting Started

#iOS
As with all Core services, you don't need to do anything beyond the traditional shiny Init calls in each platform.  Jobs (Shiny.Job.IJobManager) is installed into the container by default.

### FYI
Please note that this framework uses background fetch on iOS.  Be aware of the following:
* We cannot guarantee when/if background fetch will wake up
* if you have other frameworks using background fetch, Shiny may not play nice with them since it needs to own the completion handler.

### Setup
1. Install From [![NuGet](https://img.shields.io/nuget/v/Shiny.Core.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Core/)

2. In your AppDelegate.cs, add the following:

```csharp
public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler) 
    => this.ShinyPerformFetch(completionHandler);
```



3.Add the following to your AndroidManifest.xml

```xml
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.BATTERY_STATS" />	
<uses-permission android:name="android.permission.RECEIVE_BOOT_COMPLETED" />
```


### NOTES
* If Doze is enabled, the reschedule period is not guaranteed to be an average of 10 mins.  It may be much longer. 
* IF YOU ARE NOT SCHEDULING YOUR JOBS ON EVERY START - If you application force quits, Android will not restart the job scheduler.  For this, there is CrossJobs.EnsureJobServiceStarted


3. Add the following to your info.plist
```xml
<key>UIBackgroundModes</key>
<array>
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
```
