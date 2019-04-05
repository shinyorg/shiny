Title: Getting Started
Order: 1
---

## PLATFORMS
|Platform|Version|
|--------|-------|
iOS|7+
Android|4.3+
Windows UWP (including core)|16299+
.NET Standard|2.0


## SUPPORTED SENSORS

* Accelerometer
* Ambient Light
* Barometer
* Compass
* Device Orientation
* Gyroscope
* Magnetometer
* Pedometer
* Proximity


## SETUP

Be sure to install the Plugin.Sensors nuget package in all of your main platform projects as well as your core/PCL project

[![NuGet](https://img.shields.io/nuget/v/Plugin.Sensors.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Sensors/)

### iOS

If you plan to use the pedometer on iOS, you need to add the following to your Info.plist

```xml
<dict>
	<key>NSMotionUsageDescription</key>
	<string>Using some motion</string>
</dict>
```


## HOW TO USE BASICS

```csharp

// discover some devices
CrossSensors.Accelerometer
CrossSensors.Gyroscope
CrossSensors.Magnetometer.WhenReadingTaken().Subscribe(reading => {});

```
