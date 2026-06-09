---
name: shiny-ble-hosting
description: Generate code using Shiny.BluetoothLE.Hosting, a BLE peripheral hosting library for .NET with GATT server, advertising, and L2CAP CoC channels
auto_invoke: true
triggers:
  - ble hosting
  - ble peripheral
  - ble advertise
  - ble advertising
  - gatt server
  - gatt service
  - gatt characteristic
  - ble host
  - bluetooth hosting
  - bluetooth peripheral
  - bluetooth advertise
  - IBleHostingManager
  - IGattService
  - IGattServiceBuilder
  - IGattCharacteristic
  - IGattCharacteristicBuilder
  - AddBluetoothLeHosting
  - StartAdvertising
  - StopAdvertising
  - AdvertiseBeacon
  - AdvertisementOptions
  - CharacteristicSubscription
  - GattResult
  - GattState
  - WriteRequest
  - ReadRequest
  - WriteOptions
  - NotificationOptions
  - IPeripheral
  - Shiny.BluetoothLE.Hosting
  - ibeacon advertise
  - ble notify
  - ble indicate
  - ble read characteristic
  - ble write characteristic
  - L2CAP
  - L2Cap
  - L2CapChannel
  - L2CapInstance
  - OpenL2Cap
  - PSM
---

# Shiny.BluetoothLE.Hosting Skill

You are an expert in Shiny.BluetoothLE.Hosting, a .NET library for turning a device into a BLE peripheral. It provides a GATT server, BLE advertising, iBeacon broadcasting, and L2CAP CoC channels through the imperative `IBleHostingManager` API.

> The attribute-based managed characteristic pattern (`BleGattCharacteristic` base class, `[BleGattCharacteristic]` attribute, `AddBleHostedCharacteristic<T>`, `AttachRegisteredServices`) was removed for AOT compliance. Use the imperative `AddService(...)` builder pattern below — it is the single supported way to expose a GATT service.

## When to Use This Skill

Invoke this skill when the user wants to:
- Set up a BLE GATT server on a device (iOS, macOS, Mac Catalyst, Android, Linux)
- Advertise as a BLE peripheral with custom service UUIDs or a local name
- Broadcast as an iBeacon
- Create GATT services with read, write, and notify characteristics
- Handle read requests from connected centrals
- Handle write requests from connected centrals
- Send notifications or indications to subscribed centrals
- Configure characteristic properties (read, write, notify, indicate, encryption)
- React to central subscribe/unsubscribe events
- Build a MAUI app that acts as a BLE peripheral
- Publish an L2CAP PSM for centrals to open streaming channels against (iOS/macOS, Android API 29+, Linux)

## Library Overview

- **NuGet**: `Shiny.BluetoothLE.Hosting` (Android, iOS/macOS, Mac Catalyst, Windows stub), `Shiny.BluetoothLE.Hosting.Linux` (Linux via BlueZ)
- **Namespaces**: `Shiny.BluetoothLE.Hosting`
- **Platforms**: iOS, Mac Catalyst, macOS (CoreBluetooth), Android, Linux (BlueZ). Windows throws `NotSupportedException` for advertising/GATT-server hosting; only the `OpenL2Cap` API is exposed and it also throws on Windows.
- **Dependencies**: `Shiny.Core`, `Shiny.BluetoothLE.Common`

The library exposes a single imperative API: inject `IBleHostingManager`, call `AddService(uuid, primary, builder)` to register a GATT service inline.

## Setup

### 1. Install NuGet Package
```bash
dotnet add package Shiny.BluetoothLE.Hosting
```

### 2. Register in MauiProgram.cs

```csharp
builder.Services.AddBluetoothLeHosting();
```

## Code Generation Instructions

When generating code for Shiny.BluetoothLE.Hosting projects, follow these conventions:

### 1. Requesting Access

Always request access before advertising or adding services:

```csharp
var access = await hostingManager.RequestAccess();
if (access != AccessState.Available)
{
    // Handle denied/disabled/not supported
    return;
}
```

### 2. Imperative GATT Service Setup

Use the builder pattern to add services and characteristics inline:

```csharp
var service = await hostingManager.AddService("12345678-1234-1234-1234-123456789abc", true, sb =>
{
    sb.AddCharacteristic("12345678-1234-1234-1234-123456789ab1", cb =>
    {
        cb.SetRead(request =>
        {
            var data = System.Text.Encoding.UTF8.GetBytes("Hello");
            return Task.FromResult(GattResult.Success(data));
        });

        cb.SetWrite(request =>
        {
            var received = request.Data;
            if (request.IsReplyNeeded)
                request.Respond(GattState.Success);
            return Task.CompletedTask;
        }, WriteOptions.Write);

        cb.SetNotification(sub =>
        {
            // sub.IsSubscribing tells you if subscribing or unsubscribing
            // sub.Peripheral is the central device
            return Task.CompletedTask;
        }, NotificationOptions.Notify);
    });
});
```

### 3. Advertising

```csharp
// Advertise with local name and service UUIDs
await hostingManager.StartAdvertising(new AdvertisementOptions(
    LocalName: "MyDevice",
    ServiceUuids: "12345678-1234-1234-1234-123456789abc"
));

// Advertise with defaults (no name, no service UUIDs)
await hostingManager.StartAdvertising();

// Stop advertising
hostingManager.StopAdvertising();
```

### 4. iBeacon Broadcasting

```csharp
await hostingManager.AdvertiseBeacon(
    uuid: Guid.Parse("12345678-1234-1234-1234-123456789abc"),
    major: 1,
    minor: 100,
    txpower: -59
);
```

### 5. Sending Notifications

```csharp
// From an IGattCharacteristic reference
var data = System.Text.Encoding.UTF8.GetBytes("Updated value");

// Notify all subscribed centrals
await characteristic.Notify(data);

// Notify specific centrals
await characteristic.Notify(data, specificPeripheral1, specificPeripheral2);
```

### 6. Responding to Write Requests

When `WriteRequest.IsReplyNeeded` is true, you must call `Respond`:

```csharp
cb.SetWrite(request =>
{
    try
    {
        // Process data
        if (request.IsReplyNeeded)
            request.Respond(GattState.Success);
    }
    catch
    {
        if (request.IsReplyNeeded)
            request.Respond(GattState.Failure);
    }
    return Task.CompletedTask;
}, WriteOptions.Write);
```

### 7. L2CAP Channels

Publish an L2CAP PSM that centrals can connect to for streaming data without going through GATT. `OpenL2Cap` returns an `L2CapInstance` representing the listener; the `onOpen` callback fires for every accepted central connection. Each `L2CapChannel` is itself an `IDisposable` — dispose it to close that specific central's channel; dispose the `L2CapInstance` to stop accepting new connections and release the PSM.

```csharp
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

var instance = await hostingManager.OpenL2Cap(
    secure: false,
    onOpen: channel =>
    {
        Console.WriteLine($"Central {channel.Identifier} connected on PSM {channel.Psm}");

        channel.DataReceived.Subscribe(
            async payload =>
            {
                // Echo back
                await channel.Write(payload).ToTask();
            },
            ex => Console.WriteLine($"Channel error: {ex.Message}"),
            () => channel.Dispose()
        );
    }
);

Console.WriteLine($"Listening on PSM {instance.Psm}");

// Later, when shutting down:
instance.Dispose();
```

The platform-assigned PSM is on `instance.Psm` — advertise it to centrals out-of-band (typically through a GATT characteristic exposed by your service).

Platform notes:
- **iOS / Mac Catalyst / macOS**: `CBPeripheralManager.PublishL2CapChannel(encryptionRequired)`. The `secure` flag maps to encryption-required.
- **Android**: `BluetoothAdapter.ListenUsing[Insecure]L2capChannel`. Requires API 29+ — throws `InvalidOperationException` on older versions.
- **Linux**: `AF_BLUETOOTH` / `BTPROTO_L2CAP` / `SOCK_SEQPACKET` socket via `Shiny.BluetoothLE.Hosting.Linux`. PSM is kernel-assigned from the LE dynamic range (≥ `0x80`); `secure=true` maps to `BT_SECURITY_MEDIUM`, `secure=false` to `BT_SECURITY_LOW`. Independent of GATT-server / LE-advertisement hosting (still WIP on Linux) — centrals must learn the device address out-of-band.
- **Windows / Blazor WASM**: not supported. `OpenL2Cap` throws `NotSupportedException`.

#### File Transfer

`L2CapChannelExtensions.SendFile(...)` streams a file over a connected channel with progress metrics (throughput, percent-complete, ETA) matching the `Shiny.Net.Http.TransferProgress` shape. Useful for pushing large blobs to a connected central:

```csharp
using Shiny.BluetoothLE;

using var instance = await hostingManager.OpenL2Cap(secure: false, onOpen: async channel =>
{
    await channel.SendFile(
        "/path/to/firmware.bin",
        bufferSize: 4096,
        onProgress: p => Console.WriteLine(
            $"{p.PercentComplete:P0} {p.BytesPerSecond / 1024} KB/s ETA {p.EstimatedTimeRemaining}"
        )
    );
    channel.Dispose();
});
```

A `Stream` overload is available for non-file sources; pass `totalBytes` to enable percent / ETA computation.

### 8. File Organization

- Group hosting services in a `BleHosting/` folder, one class per GATT service
- Or by feature: `Features/{Feature}/{Name}HostingService.cs`

## Namespace Ambiguities

- **`IPeripheral`**: Both `Shiny.BluetoothLE` (client) and `Shiny.BluetoothLE.Hosting` define an `IPeripheral` interface with different members. If both packages are referenced in the same project, do NOT add both namespaces as global usings. Use file-level `using` directives or FQN (`Shiny.BluetoothLE.Hosting.IPeripheral`) to disambiguate.

## Best Practices

1. **Always request access first** -- call `RequestAccess()` and check the result before any hosting operations
2. **Compose services in code, not attributes** -- the managed `BleGattCharacteristic` pattern was removed for AOT compliance. Build services with `AddService(uuid, primary, sb => ...)` lambdas inside a service class registered in DI so they're easy to unit-test
3. **Use valid UUIDs** -- standard 128-bit UUID format (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`) or short 16-bit UUIDs for standard Bluetooth SIG services
4. **Respond to writes when needed** -- always check `WriteRequest.IsReplyNeeded` and call `Respond` with the appropriate `GattState`
5. **Return GattResult.Error on failures** -- use `GattResult.Error(GattState.Failure)` in read handlers when an error occurs
6. **Stop advertising before cleanup** -- call `StopAdvertising()` and `ClearServices()` when done
7. **Check IsAdvertising** -- avoid calling `StartAdvertising` if already advertising
8. **Dispose `L2CapInstance` and per-central `L2CapChannel`s explicitly** -- disposing the instance closes the listener but does not auto-close already-open channels

## Reference Files

For detailed API signatures and examples, see:
- `reference/api-reference.md` - Full API surface, interfaces, enums, records, and usage examples
