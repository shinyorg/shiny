# Adapter

The adapter is where everything begins and ends.  Unlike the platform implementations of the adapter scan, the BLE plugin Scan()
method will scan continuously (or restart the scan when the cycle completes) until you dispose of the Scan() token.

## General

**Monitor and read status of adapter**
```csharp
// current status
CrossBleAdapter.Current.Status

// monitor status changes
CrossBleAdapter.Current.WhenStatusChanged().Subscribe(status => {});
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

**Open Device Settings**

_Currently support by iOS8, iOS9, and Android only_
```csharp
if (CrossBleAdapter.Current.CanOpenSettings)
    CrossBleAdapter.Current.OpenSettings();
```

**Change Adapter State (Power on/off)**

_Supported by Android only_
```csharp
if (CrossBleAdapter.Current.CanChangeAdapterState)
    CrossBleAdapter.Current.SetAdapterState(true); // or false to disable
```

**Listen to scans for decoupled components**

_With the use of observables everywhere, the option to hook up to the scan result events were taken away.  There are good cases to have listening options without actually starting a scan.  This is that option!__
```csharp
BleAdapter.Current.ScanListen().Subscribe(scanResult => {});
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

## Open Settings

```csharp
CrossBleAdapter.Current.OpenSettings();
```

## Get Known Device

_Allows you to get a known device by the device ID (GUID/UUID).  Note that this ID will be different platform to platform_
```csharp
var device = CrossBleAdapter.Current.GetKnownDevice(Guid);
```
