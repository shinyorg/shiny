Title: Getting Started
Order: 1
RedirectFrom: index
---

## Platform Setup


 


## Setup

1. The first thing is to install any of the nuget packages you need from above.  

2. In your shared code project.  Create a Shiny startup file:

snippet: YourShinyStartup

### Android

1. Create a new "MainApplication" in your Android head project.

```csharp
using System;
using Android.App;
using Android.Runtime;

namespace YourNamespace.Droid
{
    [Application]
    public class MainApplication : Shiny.ShinyAndroidApplication<YourNamespace.YourShinyStartup>
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}

```

IF you have an application file already, simple add the following to your OnCreate method

```csharp
public override void OnCreate()
{
    base.OnCreate();

    Shiny.AndroidShinyHost.Init(
        this,
        new YourShinyStartup()
    );
}
```


2. Add the following to your activity classes

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
{
    AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

### iOS

1. In your ApplicationDelegate.cs, add the following in your FinishedLaunching method
```csharp
public override bool FinishedLaunching(UIApplication app, NSDictionary options)
{
    Shiny.iOSShinyHost.Init(new YourShinyStartup());
    ...
}
```

### UWP

1. Add the following to your App.xaml.cs constructor

```csharp
this.ShinyInit(new YourStartup());
```

2. Add the following to your Package.appxmanifest under the <Application><Extensions> node

```xml
<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="general"/>
        <Task Type="systemEvent"/>
        <Task Type="timer"/>
    </BackgroundTasks>
</Extension>
```

### Tizen
COMING SOON

### macOS
macOS, watchOS, & tvOS are not officially supported by Shiny yet, but will be in the future

# Shiny Startup

Startup is the place where you wire up all of the necessary application depedencies you need


Out of the box, Shiny automatically pushes the following on to the service container

* IEnvironment
* IPowerManager
* IJobManager
* ISettings
