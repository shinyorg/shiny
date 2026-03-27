---
name: shiny-bluetoothle
description: Shiny BluetoothLE client/central operations for scanning, connecting, and communicating with BLE peripherals
auto_invoke: true
triggers:
  - bluetooth
  - ble
  - bluetoothle
  - bluetooth le
  - bluetooth low energy
  - peripheral
  - gatt
  - characteristic
  - scan ble
  - ble scan
  - ble connect
  - IBleManager
  - IPeripheral
  - managed scan
  - ble notification
  - ble write
  - ble read
  - ble descriptor
  - advertisement
---

# Shiny BluetoothLE (Client/Central)

## When to Use This Skill

Use this skill when the user needs to:
- Scan for BLE peripherals
- Connect to and communicate with BLE devices
- Read, write, or subscribe to GATT characteristics
- Read or write GATT descriptors
- Implement managed scans with automatic peripheral list management
- Request MTU changes, pair with devices, or perform reliable write transactions
- Read standard BLE services (device information, battery, heart rate)
- Work with BLE advertisement data

Do NOT use this skill for BLE hosting/peripheral mode (advertising, GATT server). That is a separate library (`Shiny.BluetoothLE.Hosting`).

## Library Overview

- **NuGet Package**: `Shiny.BluetoothLE`
- **Primary Namespace**: `Shiny.BluetoothLE`
- **Managed Scan Namespace**: `Shiny.BluetoothLE.Managed`
- **Platforms**: Android, iOS/macOS (Apple), Windows, WebAssembly

## Setup

Register in your `MauiProgram.cs` or host builder:

```csharp
// Basic registration
services.AddBluetoothLE();

// With a delegate for background events (adapter state changes, peripheral connections)
services.AddBluetoothLE<MyBleDelegate>();

// iOS/macOS only - with Apple-specific configuration
services.AddBluetoothLE<MyBleDelegate>(new AppleBleConfiguration(
    ShowPowerAlert: true,
    RestoreIdentifier: "my-ble-app"
));
```

The delegate class:

```csharp
public class MyBleDelegate : BleDelegate
{
    public override Task OnAdapterStateChanged(AccessState state)
    {
        // Handle adapter state changes (foreground or background)
        return Task.CompletedTask;
    }

    public override Task OnPeripheralStateChanged(IPeripheral peripheral)
    {
        // Handle peripheral connection state changes (foreground or background)
        return Task.CompletedTask;
    }
}
```

## Code Generation Instructions

When generating BLE client code, follow these conventions:

1. **Always request access before scanning**: Call `IBleManager.RequestAccess()` or `RequestAccessAsync()` and verify `AccessState.Available` before starting a scan.

2. **Use reactive (IObservable) APIs as the primary pattern**: The library is built on System.Reactive. Use the `Async` extension methods only when you need Task-based patterns.

3. **Dispose scan subscriptions**: Only one scan can be active at a time. Always dispose the scan subscription or call `StopScan()` when done.

4. **Use string-based UUIDs for services and characteristics**: The API uses string UUIDs throughout (e.g., `"180D"` or `"0000180d-0000-1000-8000-00805f9b34fb"`).

5. **Prefer `ConnectAsync` for simple connection flows**: It handles waiting for the connected state and has a default 30-second timeout.

6. **Always call `CancelConnection()` or `DisconnectAsync()` when done**: Connections are not automatically cleaned up.

7. **Use `IManagedScan` for UI-bound scanning**: It provides an `INotifyReadOnlyCollection` that works with MVVM bindings and handles peripheral deduplication, buffering, and stale removal.

8. **Feature detection via interface checks**: Optional capabilities (MTU request, pairing, reliable transactions) use feature interfaces. Always use the `Try*` or `Can*` extension methods rather than casting directly.

9. **Handle `BleException` and `BleOperationException`**: GATT operations can throw these. `BleOperationException` includes a `GattStatusCode`.

10. **Connection auto-reconnect**: `ConnectionConfig.AutoConnect = true` (default) enables automatic reconnection. Set to `false` for faster initial connections.

## Namespace Ambiguities

- **`IPeripheral`**: Both `Shiny.BluetoothLE` and `Shiny.BluetoothLE.Hosting` define an `IPeripheral` interface. If both packages are referenced, do NOT add `Shiny.BluetoothLE.Hosting` as a global using. Use file-level `using` or FQN (`Shiny.BluetoothLE.IPeripheral`) to disambiguate.
- **`DeviceInfo`**: `Shiny.BluetoothLE` has a `DeviceInfo` class that conflicts with `Microsoft.Maui.Devices.DeviceInfo` in MAUI apps. Use FQN when needed.

## Best Practices

- Use `ScanConfig` with `ServiceUuids` to filter scans, especially on iOS where background scanning requires a service UUID filter.
- For Android, consider `AndroidScanConfig` for scan mode and batching options.
- For Android, consider `AndroidConnectionConfig` for connection priority settings.
- Always check `CharacteristicProperties` before attempting read/write/notify operations using the convenience extensions (`CanRead()`, `CanWrite()`, `CanNotify()`, etc.).
- Use `WriteCharacteristicBlob()` for writing large data streams that exceed MTU size.
- Use `NotifyCharacteristic()` for real-time data streaming from a peripheral -- it handles subscription lifecycle and auto-reconnection.
- Buffer or throttle scan results in UI scenarios to avoid performance issues.
- Use `WhenConnected()` and `WhenDisconnected()` convenience extensions for cleaner connection state handling.

## Reference Files

- [API Reference](reference/api-reference.md)
