## Setup - Android

1.Install From [![NuGet](https://img.shields.io/nuget/v/Plugin.Jobs.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Jobs/)

2. In your Main/Launch Activity.OnCreate - add the following
```csharp
Plugin.Jobs.CrossJobs.Init(this, savedInstanceState); // activity
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