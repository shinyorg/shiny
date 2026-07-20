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
  - L2CAP
  - L2Cap
  - L2CapChannel
  - ICanL2Cap
  - OpenL2CapChannel
  - PSM
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
- Open L2CAP CoC channels to a peripheral that has published a PSM

Do NOT use this skill for BLE hosting/peripheral mode (advertising, GATT server). That is a separate library (`Shiny.BluetoothLE.Hosting`).

## Library Overview

- **NuGet Package**: `Shiny.BluetoothLE` (Android, iOS/macOS, Windows), `Shiny.BluetoothLE.Linux` (Linux via BlueZ), `Shiny.BluetoothLE.Blazor` (Blazor WebAssembly via Web Bluetooth API)
- **Primary Namespace**: `Shiny.BluetoothLE`
- **Managed Scan Namespace**: `Shiny.BluetoothLE.Managed`
- **Platforms**: Android, iOS/macOS (Apple), Windows, Linux (BlueZ), WebAssembly (Web Bluetooth)

### Blazor WebAssembly / Web Bluetooth caveats

The Blazor implementation is built on the browser's Web Bluetooth API and inherits its limitations:

- **User-gesture gated.** Scans must be kicked off from a click handler. The browser shows a native chooser and Shiny only sees the peripheral(s) the user explicitly selects — there is no ambient/background scanning and no manufacturer data.
- **HTTPS or `http://localhost` required.** The API is unavailable on plain `http://`.
- **No background operation.** Scanning and connections stop when the tab is backgrounded or closed.
- **Browser support is Chromium-only and requires enabling in some cases.** When generating setup instructions or troubleshooting guidance, note the following:
    - **Chrome / Edge / Brave / Opera (desktop)**: enabled by default on Windows, macOS, Linux, ChromeOS. Fallback: `chrome://flags/#enable-web-bluetooth` (or `edge://flags`, etc.) → *Enabled* → restart. Linux also needs `experimental-web-platform-features` on and BlueZ 5.43+.
    - **Chrome / Edge (Android)**: Android 6.0+. OS location services must be on for the chooser prompt to appear.
    - **Samsung Internet**: enable `internet://flags` → *Web Bluetooth*.
    - **Safari (macOS / iOS / iPadOS)**: not supported. On iOS/iPadOS suggest third-party WKWebView-based browsers *Bluefy* or *WebBLE*. Stock macOS Safari has no workaround.
    - **Firefox**: not supported on any platform.

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

### Android Manifest (required for scanning)

Add the BLE permissions to `Platforms/Android/AndroidManifest.xml`. **Critical:** on Android 12+ (API 31+) Shiny requests only `BLUETOOTH_SCAN` / `BLUETOOTH_CONNECT` at runtime — it does NOT request `ACCESS_FINE_LOCATION`. If you declare `BLUETOOTH_SCAN` *without* the `neverForLocation` flag, Android silently withholds **all** scan results unless fine location is also granted, so scans appear to return nothing. Unless your app actually derives physical location from BLE, always add `neverForLocation`:

```xml
<!-- Android 12+ -->
<uses-permission android:name="android.permission.BLUETOOTH_SCAN"
                 android:usesPermissionFlags="neverForLocation" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />

<!-- Android 11 and below -->
<uses-permission android:name="android.permission.BLUETOOTH" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" android:maxSdkVersion="30" />
```

If you DO use BLE to infer location, omit `neverForLocation` and also request/grant `ACCESS_FINE_LOCATION` at runtime.

Scans discover both legacy and Bluetooth 5 extended advertisements automatically (when the chipset supports extended advertising); legacy advertisements that virtually all peripherals send are always included. To force a legacy-only scan, use `new AndroidScanConfig(IncludeExtendedAdvertisements: false)`.

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

9. **Handle `BleException` and `BleOperationException`**: GATT operations can throw these. `BleOperationException` includes a `GattStatusCode`. An in-flight operation that is interrupted by a disconnect faults with a `BleException` rather than hanging, so always have an `onError` handler (or `catch`) on read/write/discovery calls — with auto-reconnect enabled, retry once the peripheral reports `Connected` again.

10. **Connection auto-reconnect**: `ConnectionConfig.AutoConnect = true` (default) enables automatic reconnection. Set to `false` for faster initial connections.

## L2CAP Channels

Some platforms support L2CAP Connection-Oriented Channels for streaming data without going through GATT. This is exposed as an optional capability — `ICanL2Cap` — on the platform `Peripheral` types.

### Feature detection

```csharp
using Shiny.BluetoothLE;

if (peripheral.IsL2CapAvailable())
{
    // Backend supports L2CAP
}
```

### Opening a channel

```csharp
// Safe variant — returns an empty observable on unsupported platforms
peripheral
    .TryOpenL2CapChannel(psm: 0x0083, secure: false)
    .Subscribe(channel => { /* ... */ });

// Direct access when the cast succeeds
if (peripheral is ICanL2Cap l2cap)
{
    l2cap.OpenL2CapChannel(psm: 0x0083, secure: false).Subscribe(channel =>
    {
        // channel.Psm           — the PSM the channel was opened on
        // channel.Identifier    — the remote peer identifier
        // channel.DataReceived  — IObservable<byte[]> of incoming bytes
        // channel.Write(bytes)  — IObservable<Unit> that completes when bytes are queued
    });
}
```

`L2CapChannel` implements `IDisposable` — dispose it to close the underlying streams (Apple) or socket (Android).

### Reading and writing

```csharp
using System.Reactive.Threading.Tasks;

channel.DataReceived.Subscribe(
    payload => Console.WriteLine($"<- {payload.Length} bytes"),
    ex      => Console.WriteLine($"Channel error: {ex.Message}"),
    ()      => Console.WriteLine("Remote closed the channel")
);

await channel.Write(payload).ToTask();
```

`DataReceived` is hot, emits right-sized byte arrays per read, completes on remote close, and surfaces I/O errors via `OnError`.

### Platform notes

- **iOS / Mac Catalyst / macOS**: `CBPeripheral.OpenL2CapChannel`. The `secure` flag is ignored — security is set by how the peripheral published the channel.
- **Android**: `BluetoothDevice.CreateL2capChannel` / `CreateInsecureL2capChannel`. Requires API 29+. Throws `InvalidOperationException` on older versions.
- **Windows / Linux / Blazor**: not currently supported (`IsL2CapAvailable()` returns false).

### File Transfer

`L2CapChannelExtensions.SendFile(...)` streams a file over the channel with progress metrics (throughput, percent-complete, estimated time remaining) that match `Shiny.Net.Http.TransferProgress`:

```csharp
using Shiny.BluetoothLE;

await channel.SendFile(
    "/path/to/file.bin",
    bufferSize: 4096,
    onProgress: p => Console.WriteLine(
        $"{p.PercentComplete:P0} ({p.BytesTransferred}/{p.BytesToTransfer}) " +
        $"{p.BytesPerSecond / 1024} KB/s, ETA {p.EstimatedTimeRemaining}"
    ),
    cancellationToken: ct
);
```

- Progress emissions cadence ~2s plus a final 100% emission on completion.
- A `Stream` overload exists for non-file sources. Pass `totalBytes` to enable percent / ETA; pass `null` and `IsDeterministic` will be false, `PercentComplete` returns `-1`, `EstimatedTimeRemaining` returns `TimeSpan.Zero`.

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
