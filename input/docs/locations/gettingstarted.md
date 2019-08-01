Title: Getting Started
Order: 1
---
A cross platform library for Xamarin & Windows that allows for easy geofence detection


## PLATFORMS

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|5|
|UWP|16299|

## SETUP

Be sure to install the Shiny.Geofencing nuget package in all of your main platform projects as well as your core/NETStandard project

[![NuGet](https://img.shields.io/nuget/v/Shiny.Locations.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Locations/)

**Android**

Add the following to your AndroidManifest.xml

```xml
<!--this is necessary for Android v6+ to get the device name and address-->
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
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