Title: Scanning
Order: 1
---

Once you have your ICentralManager, you have several options

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