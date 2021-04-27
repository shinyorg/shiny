Title: BLE Manager
Order: 2
---
# BleManager

The adapter is where everything begins and ends.  Unlike the platform implementations of the adapter scan, the BLE plugin Scan()
method will scan continuously (or restart the scan when the cycle completes) until you dispose of the Scan() token.


## General

**Monitor and read status of adapter**
```csharp
// current status
BleManager.Status

// monitor status changes
BleManager.WhenStatusChanged().Subscribe(status => {});
```

**Scan for Devices**

```csharp
var scanner = CrossBleAdapter.Current.Scan().Subscribe(scanResult => 
{
    // do something with it
    // the scanresult contains the device, RSSI, and advertisement packet
        
});

scanner.Dispose(); // to stop scanning
```


**Scan for Devices - Advanced**
```csharp
CrossBleAdapter.Current.Scan(
    new ScanConfig 
    {
        ServiceUuids = { new Guid("<your guid here>") }
    }
)
.Subscribe(scanResult => 
{
})
```

## General Scan

```csharp
var scan = CentralManager.Scan().Subscribe(scanResult => {});

// to stop scan 
scan.Dispose();

```

## Specific Scans

_Find a named peripheral_

```csharp
// this will return a single result and complete - you can await it if you want
var peripheral = CentralManager.ScanForPeripheral("YourDevice");

var peripheral = CentralManager.ScanForPeripheral(YourDevice);
```


**Change Adapter State (Power on/off)**

_Supported by Android only_
```csharp
if (CrossBleAdapter.Current.CanChangeAdapterState)
    CrossBleAdapter.Current.SetAdapterState(true); // or false to disable
```

**Get Connected Devices**

```csharp
var devices = CrossBleAdapter.Current.GetConnectedDevices();
foreach (var device in devices)
{
    // do something
}
```

## Extensions
```csharp
// this essentially recreates the scan cycles like on Android
CrossBleAdapter.Current.ScanInterval(TimeSpan).Subscribe(scanResult => {});

```

## Toggle State of Adapter

```csharp
// returns true if successful
CrossBleAdapter.Current.ToggleAdapterState();
```
