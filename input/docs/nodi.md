Title: Using Shiny without Dependency Injection
---

Can it be done?  The answer is absolutely.  As with almost all DI based frameworks, there is a static variable with the container object somewhere.  For Shiny, it is as simple as the following:

'''csharp
Shiny.ShinyHost.Resolve
'''

There are also shims setup for the more classic way of using plugins

CrossBleAdapter.Current
CrossJobManager.Current
CrossEnvironment.Current
CrossSettings.Current
CrossConnectivity.Current
CrossPower.Current
CrossFileSystems.Current

CrossBeacons.Current
CrossGeofences.Current
CrossGps.Current
CrossMotionActivity.Current
CrossHttpTransfers.Current
CrossNotifications.Current

CrossSensors.Accerometer


Don't like startup, use attributes anywhere

[assembly: ShinyBleCentral(typeof(YourDelegate))]

Then in your register methods, instead of passing your Startup file, do the following

<Platform>ShinyHost.Init(new AttributeShinyStartup(typeof(YourApp).Assembly, ...);
You can pass in multiple assemblies where the attributes have been registered

If that still isn't enough, you can use auto register (warning: this comes with a minor startup cost since it scans assemblies).  You also need to be prepared to deal with linker issues.  This option comes with no support!

