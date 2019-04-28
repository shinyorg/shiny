Title: iOS
---

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
{
    JobManager.OnBackgroundFetch(completionHandler);
}
```

3. Add the following to your info.plist
```xml
<key>UIBackgroundModes</key>
<array>
	<string>fetch</string>
</array>
```
