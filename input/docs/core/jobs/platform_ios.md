## iOS

### FYI
Please note that this framework uses background fetch on iOS.  Be aware of the following:
* We cannot guarantee when/if background fetch will wake up
* if you have other frameworks using background fetch, this plugin may not play nice with them since it needs to own the completion handler.

### Setup
1. Install From [![NuGet](https://img.shields.io/nuget/v/Plugin.Jobs.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Jobs/)

2. In your AppDelegate.cs, add the following:
```csharp

In FinishedLaunching method, add
Plugin.Jobs.CrossJobs.Init();


public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
{
    Plugin.Jobs.CrossJobs.OnBackgroundFetch(completionHandler);
}
```

3. Add the following to your info.plist
```xml
<key>UIBackgroundModes</key>
<array>
	<string>fetch</string>
</array>
```
