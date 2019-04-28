Title: BluetoothLE
---

# Bluetooth LE

Bluetooth LE is divided into 2 separate categories - the central manager (client) and the peripheral manager (server). 

## NuGet
[![NuGet](https://buildstats.info/nuget/Shiny.BluetoothLE)](https://nuget.org/packages/Shiny.BluetoothLE)

[![MyGet Badge](https://buildstats.info/myget/acrfeed/Shiny.BluetoothLE/Shiny.BluetoothLE?includePreReleases=true)](https://www.myget.org/feed/acrfeed/package/nuget/Shiny.BluetoothLE)


## Support Platforms

|Platform|Version|Docs|
|--------|-------|----|
|Android|5+ (API21)|[Setup](platforms/android)|
|iOS|9+|[Setup](platforms/ios)|
|UWP|16299 - Limited Beta|[Setup](platforms/uwp)|



# FAQ

> Q. On iOS, when scanning for iBeacons - I'm getting missing scan result data
> A. Yup - that's expected.  Apple controls these scan results and has native implementation that deals with this