Title: Geofences
---

A cross platform library for Xamarin & Windows that allows for easy geofence detection


## PLATFORMS

|Platform|Version|
| ------------------- |:------------------: |
|Xamarin.iOS|iOS 6+|
|Xamarin.Android|API 10+|
|Windows 10 UWP|10+|

## SETUP

Be sure to install the Plugin.Geofencing nuget package in all of your main platform projects as well as your core/NETStandard project

[![NuGet](https://img.shields.io/nuget/v/Plugin.Geofencing.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Geofencing/)

**Android**

Add the following to your AndroidManifest.xml

```xml
<!--this is necessary for Android v6+ to get the device name and address-->
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

**iOS**

Add the following to your Info.plist

```xml
<key>NSLocationAlwaysUsageDescription</key>
<string>Your message</string>
```

**UWP**

Add location to your app manifest capabilities section

```xml
<Capabilities>
    <DeviceCapability Name="location" />
</Capabilities>
```

## HOW TO USE

### To start monitoring

    CrossGeofences.Current.StartMonitoring(new GeofenceRegion( 
        "My House", // identifier - must be unique per registered geofence
        Center = new Position(LATITUDE, LONGITUDE), // center point    
        Distance.FromKilometers(1) // radius of fence
    ));

### Wire up to notifications

    CrossGeofences.Current.RegionStatusChanged += (sender, args) => 
    {
        args.State // entered or exited
        args.Region // Identifier & details
    };

### Stop monitoring a region
    
    CrossGeofences.Current.StopMonitoring(GeofenceRegion);

    or

    CrossGeofences.Current.StopAllMonitoring();
