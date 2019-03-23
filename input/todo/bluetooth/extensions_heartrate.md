# Heart Rate

This is a small little plugin that checks and hooks to standard heart rate sensors

PLEASE NOTE THAT THIS DOES NOT NECESSARILY WORK WITH EVERY HEART SENSOR ON THE MARKET.

## Scan for Heart Rate sensors
```csharp
// using the adapter, use the filter extension
// FYI: a lot of heart rate sensors do not advertise the service UUID so they end up getting filtered out by this method
CrossBleAdapter
    .Current
    .ScanForHeartRateSensors()
    .Subscribe(scanResult => {});

```

## Check for Heart Rate Sensor on Device
```csharp
// once you have a device connection
var isHrs = await Device.HasHeartSensor();

```

## Hook to heartbeat!
 ```csharp
 // once you have a device connection
device.WhenHeartRateBpm().Subscribe(bpm => {});
 ```
