# SETUP

## Android

Add the following to your AndroidManifest.xml

```xml
<uses-permission android:name="android.permission.BLUETOOTH"/>
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>
```

## iOS

If you want to use background BLE periperhals, add the following to your Info.plist

```xml
<array>
<string>bluetooth-central</string>
</array>
```

To add a description to the Bluetooth request message (on iOS 10 this is required!)

```xml
<key>NSBluetoothPeripheralUsageDescription</key>
<string>YOUR CUSTOM MESSAGE</string>
```


## Windows

Add to your app manifest file
```xml
<Capabilities>
    <Capability Name="internetClient" />
    <DeviceCapability Name="bluetooth" />
</Capabilities>
```