# iOS

Apple has some very interesting limitations on Bluetooth especially when it comes to background scanning

## Setup
Make sure to add the following to your Info.plist

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

## Backgrounding
* Device names are not available at all
* When scanning in the background, pass a ScanConfig argument with a service UUID you wish to scan for.  Without this, you will get nothing

```csharp
BleAdapter.Current.Scan(
    new ScanSettings 
    {
        ServiceUUID = new Guid("<your guid here>")
    }
)
.Subscribe(scanResult => 
{
})
```


## Background Restoration

_This feature is only provided on iOS.  It allows for a CBCentralManager to be restored upon a device reconnection (and in turn, get the device that reconnected)_
_There is additional configuration required to use this event_

```csharp

// then in your shared code (somewhere near your initialization)
BleAdapter.Current.WhenDeviceStateRestored().Subscribe(device => 
{
    // will return the device(s) that are reconnecting
});
```