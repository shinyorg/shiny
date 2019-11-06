Title: Getting Started
Order: 1
---

# Beacons

This library deals specifically with iBeacons (Apple's beacon technology).  iBeacons are used to provide contextual location information to a device.  They are built upon BluetoothLE advertising.  They are basically radio'ing out a small piece of data every 700-1400ms that other devices can "hear" if they are listening properly. 

## SETUP

First - install the NuGet package into your shared code project: [![NuGet](https://img.shields.io/nuget/v/Shiny.Beacons.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Beacons/)

### Android & UWP
Android & UWP piggyback their functionality with Shiny.BluetoothLE.  Use the following links to ensure your setup for those:
* [Android](/docs/ble/platforms/android)
* [UWP](/docs/ble/platforms/uwp)

### iOS

iOS has its own official iBeacon API that of course uses BLE under the hood, but all of its permission set is based on Locations.  

In your Info.plist, add the following:

```xml
<key>NSLocationAlwaysUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
```

If you also want to monitoring (background searching for beacons), you'll also need to add the following node to your Info.plist UIBackground modes

```xml
<key>UIBackgroundModes</key>
<array>
    <string>location</string>
</array>
```

## Terminology
|Term|Description|
|----|-----------|
|UUID|Universally Unique Identification - this is usually a single value that you use across your entire organization or application
|Major|Major is the second part of the addressing system
|Minor|
|Identifier|This is a string of your own choosing.  You can use this to identify the scan set you are using
|[Ranging](ranging)|Ranging is a foreground only operation.  You can use this to find all beacons within a filter set.  If you provide only the UUID to filter by, all beacons under that UUID will be scanned for.  The major & minor values will also be provided when they are found.
|[Monitoring](monitoring)|Monitoring is a background operation.  Monitoring will not provide you with an identity of the beacon.  For instance, if you scan by UUID+major - it will not provide the minor (to lock down specific beacons) for you.  Think of this like knowing the person is in a building, but not which floor.
|Region vs Beacon|Region is a filter set of beacons where as the beacon itself is an individual/specific entity

## Gotcha
* Scanning for any beacons always works down to specific filter sets meaning you are adding (not mixing & matching)
    1. UUID
    2. Major
    3. Minor
* You must always have an initial filter of a UUID for ranging or monitoring
* iOS limits the amount of beacons you are allowed to monitor to 20.  This value is also shared with your geofences, so you need to think about your strategy when using beacons