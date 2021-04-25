Title: Getting Started
Order: 1
---

# DESCRIPTION

Bluetooth LE is divided into 2 separate categories - the central manager (client) and the peripheral manager (server). 


## PLATFORMS

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|8|
|UWP|16299|

## USAGE

|Area|Info|
|----|----|
|NuGet| [![BleNugetShield]][BleNuget] |
|Shiny Startup|services.UseBleClient|
|Main Service|Shiny.BluetoothLE.IBleManager|
|Shiny Delegate|Shiny.BluetoothLE.IBleDelegate|
|Static Generated|ShinyBle|
|Manual Resolve|ShinyHost.Resolve<Shiny.BluetoothLE.IBleManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.BluetoothLE.IBleManager>>()|


## SETUP

#### Android
Make sure to add the following to your application node in AndroidManifest.xml
```xml
<uses-permission android:name="android.permission.BLUETOOTH"/>
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>
```


#### iOS
Add the following to your Info.plist
```xml
<key>NSBluetoothPeripheralUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
<key>NSBluetoothAlwaysUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
```

#### UWP
Add to your app manifest file
```xml
<Capabilities>
    <Capability Name="internetClient" />
    <DeviceCapability Name="bluetooth" />
</Capabilities>
```

