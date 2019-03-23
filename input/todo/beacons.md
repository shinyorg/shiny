# ACR Reactive Beacons Plugin for Xamarin & Windows
_Scans for iBeacons_

### [SUPPORT THIS PROJECT](https://github.com/aritchie/home)

[Change Log](changelog.md)

[![NuGet](https://img.shields.io/nuget/v/Plugin.Beacons.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Beacons/)
[![Build status](https://dev.azure.com/allanritchie/Plugins/_apis/build/status/Beacons)](https://dev.azure.com/allanritchie/Plugins/_build/latest?definitionId=0)

## PLATFORMS
|Platform|Version|
|--------|-------|
|iOS|8+|
|Android|4.3+|
|Windows UWP|16299+|

## FEATURES
* 100% Managed Code
* Range Beacons
* Monitor for beacon regions in the background
* Complete RX based API

## SETUP

Install the following nuget package to all of your platform code and PCL/Core libraries

[![NuGet](https://img.shields.io/nuget/v/Plugins.Beacons.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Beacons/)

**Android - add the following to your Android app manifest**
```xml
<uses-permission android:name="android.permission.BLUETOOTH"/>
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>

<!--this is necessary for Android v6+ to get the device name and address-->
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

**iOS - add the following to your Info.plist if monitoring**
_Add the following for iBeacon background scanning_
```xml 
<key>NSLocationAlwaysUsageDescription</key>
<string>The beacons always have you!</string>
```

```xml    
<array>
<string>bluetooth-central</string>
</array>

<!--To add a description to the Bluetooth request message (on iOS 10 this is required!)-->
  
<key>NSBluetoothPeripheralUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
```

## HOW TO USE

### Request Permissions
```csharp
var result = await CrossBeacons.Current.RequestPermission();
```

### Ranging for Beacons

```csharp

var scanner = CrossBeacons
    .Current
    .WhenBeaconRanged(new BeaconRegion 
    (
        Identifier = "Whatever",
        Uuid = <Valid Guid>,
        Major = 1-65535, // optional
        Minor = 1-65535  // optional
    ))
    .Subscribe(scanResult => 
    {
        // do something with it - FYI: this will not be on the main thread, so if you are displaying to the UI, make sure to invoke on it
    });
// NOTE: you can range multiple regions, but you will have to merge in another call to the BeaconManager

scanner.Dispose(); // to stop scanning    
```

### Background Monitoring for Beacons

```csharp
CrossBeacons
    .Current
    .WhenRegionStatusChanged()
    .Subscribe(regionArgs => 
    {
        regionArgs.IsEntering
        regionArgs.Region // your register
    });

CrossBeacons.Current.StartMonitoring(new BeaconRegion(...));

// To stop monitoring
CrossBeacons.Current.StopMonitoring(YourBeaconRegion);
// OR
CrossBeacons.Current.StopAllMonitoring();

```

## FAQ
Q) Why is everything reactive instead of events/async
> I wanted event streams as I was scanning devices.  I also wanted to throttle things like characteristic notification feeds.  Lastly, was the proper cleanup of events and resources.   

Q) Why can't I scan for all beacons (no uuid)
> Because this isn't really how beacons are intended to work, so I haven't exposed this functionality intentionally (nor will I take a FR/PR for it)!

Q) How many region configurations can I scan for at a time.
> On iOS, 20 max.  You shouldn't really ever go beyond this on other platforms either

Q) Can I scan for Eddystones with this library
> No, as the title of this library says, it is currently for iBeacons only!

## ROADMAP

* Beacon Advertising
* Beacon Region registration can setup prefilters for notifying on entry and/or exit