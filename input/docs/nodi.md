Title: Using Shiny without Dependency Injection
---

Using Shiny without dependency injection is not only possible, it is quite easy to do.  Do also you like the old plugin style of CrossPlugin.Current?  Than you've come to the right document

 Shiny offer a couple different ways of accomplishing this

1. Using the service locator: Shiny.ShinyHost.Resolve<IShinyService>();
2. Using the supplied shim for each library (ie. Shiny.CrossGeofences.Current )


Also, if you aren't a fan of the boilerplate DI stuff - there are a couple of different ways to accomplish this outside of the standard Shiny startup class

1. Assembly Level Attributes
2. Auto Registrations


## Attribute Registrations

Attribute registration is pretty simple and allows you to bypass the necessity of creating a Shiny Startup class.  It should be noted that this doesn't save you a lot of code, but it does allow you to put registration in assembly level attributes in essentially any file.  This is very similar to how Xamarin Forms registers things against its DependencyService.
This will perform at pretty much the same speed as the standard Startup class.

To use attribute binding, using the platform version of ShinyHost, you can do the following:

```csharp
// example with iOS and Android

Shiny.iOSShinyHost.Init(ShinyStartup.FromAssemblyAttributes(typeof(YourXamFormsApp).Assembly));
```


```csharp
// NOTE: that some of the delegate registrations are optional
[assembly: ShinyNotifications(typeof(YourNotificationDelegate), true)]
[assembly: ShinyBeacons(typeof(YourBeaconDelegate))]
[assembly: ShinyBleCentral(typeof(YourBleCentralDelegate))]
[assembly: ShinyGps(typeof(YourGpsDelegate))]
[assembly: ShinyGeofences(typeof(LocationDelegates))]
[assembly: ShinyMotionActivity]
[assembly: ShinySensors]
[assembly: ShinyHttpTransfers(typeof(YourHttpTransferDelegate))]
[assembly: ShinySpeechRecognition]

[assembly: ShinySqliteIntegration(true, true, true, true, true)]
[assembly: ShinyAppCenterIntegration(Constants.AppCenterTokens, true, true)]
```

If you still need to register your services to be used within your delegates, you can do the following:

```csharp
[assembly: ShinyService(typeof(JobLoggerTask))]
[assembly: ShinyService(typeof(IAppSettings), typeof(AppSettings))]
```

and lastly, want to register a job that always runs:

```csharp
[assembly: ShinyJob(typeof(SampleJob), "MyIdentifier", BatteryNotLow = true, DeviceCharging = false, RequiredInternetAccess = Shiny.Jobs.InternetAccess.Any)]
```



## Auto Registration

This gets you up and running with almost minimal effort, however, it does come at a price of startup speed since it scans your assemblies for delegates relating to Shiny libraries you have included in your project.  Note that you will likely have to configure the linker to not delink some of your stuff as well as Shiny to get this to work.

To use auto registration, you can 
```csharp

Shiny.iOSShinyHost.Init(ShinyStartup.AutoRegister());

Shiny.AndroidShinyHost.Init(ShinyStartup.AutoRegister());
```

## Shims or Resolve

There are also shims setup for the more classic way of using plugins

```csharp


// Jobs
Shiny.CrossJobManager.Current
Shiny.ShinyHost.Resolve<Shiny.Jobs.IJobManager>();

// Environment
Shiny.CrossEnvironment.Current
Shiny.ShinyHost.Resolve<Shiny.IEnvironment>();

// Settings
Shiny.CrossSettings.Current

// Connectivity
Shiny.CrossConnectivity.Current

// Power/Battery
Shiny.CrossPower.Current

// File System
Shiny.CrossFileSystems.Current

// Central BLE - Install Shiny.BluetoothLE nuget
Shiny.CrossBleAdapter.Current
Shiny.ShinyHost.Resolve<Shiny.BluetoothLE.ICentralManager>();

// Beacons - Install Shiny.Beacons nuget
Shiny.CrossBeacons.Current
Shiny.ShinyHost.Resolve<Shiny.Beacons.IBeaconManager>


// Geofencing - Install Shiny.Locations nuget
Shiny.CrossGeofences.Current

// GPS - Install Shiny.Locations nuget
Shiny.CrossGps.Current

// Motion Activity - Install Shiny.Locations nuget
Shiny.CrossMotionActivity.Current

// HTTP Transfers - Install Shiny.Net.Http nuget
Shiny.CrossHttpTransfers.Current

// Notifications - Install Shiny.Notifications nuget
Shiny.CrossNotifications.Current

// Sensors - Install Shiny.Sensors nuget
Shiny.CrossSensors.Accelerometer
Shiny.ShinyHost.Resolve<IAccelerometer>();

Shiny.CrossSensors.AmbientLight
Shiny.ShinyHost.Resolve<IAmbientLight>();

Shiny.CrossSensors.Barometer
Shiny.ShinyHost.Resolve<IBarometer>();

Shiny.CrossSensors.Compass
Shiny.ShinyHost.Resolve<ICompass>();

Shiny.CrossSensors.HeartRate
Shiny.ShinyHost.Resolve<IHeartRateMonitor>();

Shiny.CrossSensors.Humidity
Shiny.ShinyHost.Resolve<IHumidity>();

Shiny.CrossSensors.Magnetometer
Shiny.ShinyHost.Resolve<IMagnetometer>();

Shiny.CrossSensors.Pedometer
Shiny.ShinyHost.Resolve<IPedometer>();

Shiny.CrossSensors.Proximity 
Shiny.ShinyHost.Resolve<IProximity>();

Shiny.CrossSensors.Temperature 
Shiny.ShinyHost.Resolve<ITemperature>();


```
