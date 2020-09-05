Title: Using Shiny without the boilerplate code
---


Using Shiny without dependency injection is not only possible, it is quite easy to do.  Do also you like the old plugin style of CrossPlugin.Current?  Then you've come to the right document

Under the hood, you are still using dependency injection and you still need a startup file, BUT, you can have all of the static versions of each service generated in a project of your choice by simply adding the following to your assembly attributes:

[assembly: Shiny.GenerateStaticClasses]


After adding this attribute, perform a build.  The Shiny source generator will now scan for all Shiny nuget packages that are referenced in this project and generate corresponding static classes for the main services.  

```csharp
// BEFORE
Shiny.ShinyHost.Resolve<IJobManager>().Run(...);

// AFTER
JobManager.Run(...);
```


## Startup Genration
Also, if you aren't a fan of the boilerplate startup files, Shiny can also generate them at build time for you. 

**CAUTION: this comes at a cost of customization to your startup**



## Attribute Registration 

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

In auto registration mode, Shiny will look for any delegate (if the service has one) implementations in your application.  If a delegate is required in order to function (ie. Geofencing) and Shiny can't find one, it will throw an exception on startup with a error message stating what is missing.

To use auto registration, you can 
```csharp

Shiny.iOSShinyHost.Init(ShinyStartup.AutoRegister());

Shiny.AndroidShinyHost.Init(ShinyStartup.AutoRegister());
```

## Shims or Resolve

There are also shims setup for the more classic way of using plugins

```csharp


// Jobs
Shiny.CrossJobManager
Shiny.ShinyHost.Resolve<Shiny.Jobs.IJobManager>();

// Environment
Shiny.CrossEnvironment
Shiny.ShinyHost.Resolve<Shiny.IEnvironment>();

// Settings
Shiny.CrossSettings
Shiny.ShinyHost.Resolve<Shiny.Settings.ISettings>();

// Connectivity
Shiny.CrossConnectivity
Shiny.ShinyHost.Resolve<Shiny.Net.IConnectivity>();

// Power/Battery
Shiny.CrossPower
Shiny.ShinyHost.Resolve<Shiny.Power.IPowerManager>();

// File System
Shiny.CrossFileSystems
Shiny.ShinyHost.Resolve<Shiny.IO.IFileSystem>();

// Central BLE - Install Shiny.BluetoothLE nuget
Shiny.CrossBle.Central
Shiny.CrossBle.Peripheral

Shiny.ShinyHost.Resolve<Shiny.BluetoothLE.Central.ICentralManager>();
Shiny.ShinyHost.Resolve<Shiny.BluetoothLE.Peripherals.IPeripheralManager>();

// Beacons - Install Shiny.Beacons nuget
Shiny.CrossBeacons
Shiny.ShinyHost.Resolve<Shiny.Beacons.IBeaconManager>

// Geofencing - Install Shiny.Locations nuget
Shiny.CrossGeofences
Shiny.ShinyHost.Resolve<Shiny.Locations.IGeofenceManager>();

// GPS - Install Shiny.Locations nuget
Shiny.CrossGps
Shiny.ShinyHost.Resolve<Shiny.Locations.IGpsManager>();

// Motion Activity - Install Shiny.Locations nuget
Shiny.CrossMotionActivity
Shiny.ShinyHost.Resolve<Shiny.Locations.IMotionActivityManager>();

// HTTP Transfers - Install Shiny.Net.Http nuget
Shiny.CrossHttpTransfers
Shiny.ShinyHost.Resolve<Shiny.Net.Http.IHttpTransferManager>();

// Notifications - Install Shiny.Notifications nuget
Shiny.CrossNotifications
Shiny.ShinyHost.Resolve<Shiny.Notifications.INotificationManager>();

// Sensors - Install Shiny.Sensors nuget
Shiny.CrossSensors.Accelerometer
Shiny.ShinyHost.Resolve<Shiny.Sensors.IAccelerometer>();

Shiny.CrossSensors.AmbientLight
Shiny.ShinyHost.Resolve<Shiny.Sensors.IAmbientLight>();

Shiny.CrossSensors.Barometer
Shiny.ShinyHost.Resolve<Shiny.Sensors.IBarometer>();

Shiny.CrossSensors.Compass
Shiny.ShinyHost.Resolve<Shiny.Sensors.ICompass>();

Shiny.CrossSensors.HeartRate
Shiny.ShinyHost.Resolve<Shiny.Sensors.IHeartRateMonitor>();

Shiny.CrossSensors.Humidity
Shiny.ShinyHost.Resolve<Shiny.Sensors.IHumidity>();

Shiny.CrossSensors.Magnetometer
Shiny.ShinyHost.Resolve<Shiny.Sensors.IMagnetometer>();

Shiny.CrossSensors.Pedometer
Shiny.ShinyHost.Resolve<Shiny.Sensors.IPedometer>();

Shiny.CrossSensors.Proximity 
Shiny.ShinyHost.Resolve<Shiny.Sensors.IProximity>();

Shiny.CrossSensors.Temperature 
Shiny.ShinyHost.Resolve<Shiny.Sensors.ITemperature>();


```
