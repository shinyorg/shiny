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
  - l2cap file transfer
  - ble file transfer
  - ble file server
  - OpenL2CapFileServer
  - HandleL2CapRequests
  - L2CapFileServerOptions
  - L2CapFileRequest
  - L2CapTransferOptions
  - TransferProgress
  - BleService
  - BleServiceAttribute
  - ReadCharacteristic
  - WriteCharacteristic
  - NotifyCharacteristic
  - RequestResponseCharacteristic
  - L2CapService
  - OnChannelOpened
  - BleServiceContext
  - BleSubscription
  - BleL2CapContext
  - BleHostedServiceSession
  - AddBleHostedServices
  - AttachBleHostedServices
  - StartBleHostedAdvertising
  - ble source generator
  - ble hosting source generator
  - generated gatt service
---

# Shiny.BluetoothLE.Hosting Skill

You are an expert in Shiny.BluetoothLE.Hosting, a .NET library for turning a device into a BLE peripheral. It provides a GATT server, BLE advertising, iBeacon broadcasting, and L2CAP CoC channels through the imperative `IBleHostingManager` API.

There are two ways to expose a GATT service, and both compile down to the same thing:

1. **Imperative** — inject `IBleHostingManager` and call `AddService(uuid, primary, sb => ...)`. Best for one-off or dynamically shaped services.
2. **Source generated** — put `[BleService]` / `[L2CapService]` on a `partial class` and let the bundled generator emit the `AddService(...)` calls, the `IsReplyNeeded`/offset handling, the notify push API, and the DI registration. Prefer this for anything with more than a characteristic or two.

> The old reflection-based managed pattern (`BleGattCharacteristic` base class, `[BleGattCharacteristic]` attribute, `AddBleHostedCharacteristic<T>`, `AttachRegisteredServices`) was **removed** for AOT compliance. The source generator replaces it and emits no reflection. Never generate code against those types.

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
- Serve file uploads/downloads to connected centrals over L2CAP, with progress and throughput metrics
- Declare a GATT service or L2CAP listener with attributes on a partial class instead of builder lambdas
- Keep per-connected-central state (a SignalR-style context) across requests on a hosted service

## Library Overview

- **NuGet**: `Shiny.BluetoothLE.Hosting` (Android, iOS/macOS, Mac Catalyst, Windows stub), `Shiny.BluetoothLE.Hosting.Linux` (Linux via BlueZ)
- **Namespaces**: `Shiny.BluetoothLE.Hosting`
- **Platforms**: iOS, Mac Catalyst, macOS (CoreBluetooth), Android, Linux (BlueZ). Windows throws `NotSupportedException` for advertising/GATT-server hosting; only the `OpenL2Cap` API is exposed and it also throws on Windows.
- **Dependencies**: `Shiny.Core`, `Shiny.BluetoothLE.Common`

Inject `IBleHostingManager` and call `AddService(uuid, primary, builder)` to register a GATT service inline, or declare it with `[BleService]` on a partial class and let the bundled source generator emit that call. The generator ships inside the same package under `analyzers/dotnet/cs` - no extra `PackageReference` needed.

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

### 2b. Source-Generated GATT Service

Put the attributes on a `partial class`. The generator emits the `AddService(...)` call, the
`GattResult` wrapping, the `IsReplyNeeded`/`Respond` handling, the notify push API, and the DI
registration. Every UUID is normalized to the full 128-bit form.

```csharp
[BleService("180D", Advertise = true, Name = "HeartRate")]
public partial class HeartRateService(IHeartRateSensor sensor)
{
    // byte[] is wrapped in GattResult.Success; return GattResult to pick the status yourself
    [ReadCharacteristic("2A37")]
    Task<byte[]> ReadMeasurement(HeartRateServiceContext context)
        => Task.FromResult(new byte[] { 0x00, sensor.Read(context.User) });

    // the hook is optional - NotifyMeasurement / MeasurementSubscribers / HasMeasurementSubscribers
    // are generated either way. Put [NotifyCharacteristic] on the class (with Name) to skip the hook
    [NotifyCharacteristic("2A37", Name = "Measurement", Indicate = true)]
    Task OnMeasurementSubscription(BleSubscription subscription, HeartRateServiceContext context)
        => Task.CompletedTask;

    // returning GattState responds that value, and only when the central asked for a reply.
    // returning void/Task responds Success, or Failure if the handler throws
    [WriteCharacteristic("2A39")]
    Task<GattState> ControlPoint(byte[] data, int offset, HeartRateServiceContext context)
        => Task.FromResult(offset == 0 ? GattState.Success : GattState.InvalidOffset);

    // write + notify - the result is pushed back to the writing central, which must be subscribed
    [RequestResponseCharacteristic("2A3B", Name = "Command")]
    Task<byte[]> Exchange(byte[] request, CancellationToken cancellationToken) => Handle(request);

    // opt-in hooks; the compiler drops the generated call when you do not implement them
    partial void OnBleHandlerError(string characteristicUuid, Exception ex) => Log(ex);
}

// your half of the generated context - one instance per connected central, held across requests
public partial class HeartRateServiceContext
{
    public AuthUser? User { get; set; }
}
```

Handler parameters bind **by type, in any order, any subset** - none are required. See
`reference/api-reference.md` for the full binding table and the `SBH001`-`SBH014` diagnostics.

Wire it up:

```csharp
builder.Services.AddBluetoothLeHosting();
builder.Services.AddBleHostedServices();   // generated

await using var session = await hostingManager.AttachBleHostedServices(serviceProvider);
await hostingManager.StartBleHostedAdvertising("MyDevice");
```

An `[L2CapService]` class works the same way - one `[OnChannelOpened]` handler per accepted central,
and `PsmService`/`PsmCharacteristic` publish the assigned PSM as a read characteristic so centrals
can discover it:

```csharp
[L2CapService(Secure = false, PsmService = "180D", PsmCharacteristic = "2ABC", Name = "EchoStream")]
public partial class StreamService
{
    [OnChannelOpened]
    async Task Echo(L2CapChannel channel, BleL2CapContext context, CancellationToken cancellationToken)
    {
        await foreach (var buffer in channel.ReadAll(cancellationToken))
            await channel.Write(buffer).ToTask(cancellationToken);
    }
}
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

#### File Transfer (serving uploads & downloads)

`OpenL2CapFileServer(...)` publishes a PSM backed by a directory: connected centrals can push files to
it and pull files from it, using `IPeripheral.UploadFile` / `IPeripheral.DownloadFile` on the client
side (see the `shiny-bluetoothle` skill). This is the API to reach for — do **not** hand-roll a
protocol over `DataReceived`.

```csharp
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

var instance = await hostingManager.OpenL2CapFileServer(
    rootDirectory: Path.Combine(FileSystem.AppDataDirectory, "ble-share"),
    secure: false,
    configure: o =>
    {
        o.AllowUploads = true;
        o.AllowDownloads = true;
        o.MaxUploadSize = 10 * 1024 * 1024;       // refused as TooLarge before any body byte moves
        o.OverwriteExistingUploads = false;
        o.Authorize = req => req.FileName.EndsWith(".bin");
        o.OnProgress = e => Console.WriteLine($"{e.PeerIdentifier} {e.FileName} {e.Progress.PercentComplete:P0}");
        o.OnCompleted = r => Console.WriteLine($"{r.LocalFilePath} <- {r.Result.BytesTransferred} bytes in {r.Result.Elapsed}");
        o.OnError = (req, ex) => Console.WriteLine($"{req?.FileName}: {ex.Message}");
    }
);

Console.WriteLine($"File server on PSM {instance.Psm}");
instance.Dispose();   // unpublish and drop connected peers
```

Peer-supplied file names are resolved **under** `RootDirectory`; absolute paths and anything traversing
out (`../`) are refused with `NotPermitted` and never touch the filesystem.

For anything the directory server does not cover, handle requests yourself — this is also how you serve
from a database, generate content on the fly, or route by peer:

```csharp
var instance = await hostingManager.HandleL2CapRequests(
    secure: false,
    onRequest: async (request, ct) =>
    {
        // request.Type (Upload/Download), .FileName, .Size, .PeerIdentifier, .Psm
        if (request.Type == L2CapTransferType.Download && request.FileName == "config.json")
        {
            var bytes = Encoding.UTF8.GetBytes(BuildConfigJson());
            await request.AcceptDownload(new MemoryStream(bytes), bytes.Length, cancellationToken: ct);
        }
        else if (request.Type == L2CapTransferType.Upload && request.Size < 1_000_000)
        {
            await request.AcceptUpload(Path.Combine(inbox, Guid.NewGuid() + ".bin"), cancellationToken: ct);
        }
        else
        {
            await request.Reject(L2CapTransferError.NotPermitted, "nope", ct);
        }
    }
);
```

Every request must be answered with an accept or `Reject` before returning; the peer is blocked waiting
on the answer. Requests are served one at a time per channel. A refusal keeps the channel alive for the
next request.

Progress on the hosting side uses the same `TransferProgress` shape as the client
(`PercentComplete`, `BytesPerSecond`, `BytesTransferred`, `BytesToTransfer`, `EstimatedTimeRemaining`).

**Raw streaming**: `channel.SendFile(...)` remains available as the protocol-less primitive — bytes with
progress, no handshake, receiver must already know the length and framing. Use the file server unless
you are talking to a non-Shiny central.

### 8. File Organization

- Group hosting services in a `BleHosting/` folder, one class per GATT service
- Or by feature: `Features/{Feature}/{Name}HostingService.cs`

## Namespace Ambiguities

- **`IPeripheral`**: Both `Shiny.BluetoothLE` (client) and `Shiny.BluetoothLE.Hosting` define an `IPeripheral` interface with different members. If both packages are referenced in the same project, do NOT add both namespaces as global usings. Use file-level `using` directives or FQN (`Shiny.BluetoothLE.Hosting.IPeripheral`) to disambiguate.

## Best Practices

1. **Always request access first** -- call `RequestAccess()` and check the result before any hosting operations
2. **Reach for `[BleService]` first** -- the generator emits the same builder calls plus the response/offset handling, subscriber tracking, and per-central context. Fall back to `AddService(uuid, primary, sb => ...)` lambdas when the service shape is only known at runtime
3. **Always write the full 128-bit UUID when calling `AddService` by hand** -- short forms like `"180D"` work on Apple (`CBUUID.FromString`) but throw on Android (`java.util.UUID.fromString`). The generator normalizes for you; the imperative API does not
4. **Respond to writes when needed** -- always check `WriteRequest.IsReplyNeeded` and call `Respond` with the appropriate `GattState`
5. **Return GattResult.Error on failures** -- use `GattResult.Error(GattState.Failure)` in read handlers when an error occurs
6. **Stop advertising before cleanup** -- call `StopAdvertising()` and `ClearServices()` when done
7. **Check IsAdvertising** -- avoid calling `StartAdvertising` if already advertising
8. **Dispose `L2CapInstance` and per-central `L2CapChannel`s explicitly** -- disposing the instance closes the listener but does not auto-close already-open channels. With `[L2CapService]` the generator disposes each channel when the handler returns, and the `BleHostedServiceSession` closes the listener
9. **Keep the `BleHostedServiceSession` alive** -- `AttachBleHostedServices` returns it, and disposing it cancels in-flight handlers, closes L2CAP listeners, and removes the GATT services
10. **Never register the same service UUID twice** -- `BleHostingManager` keys services by UUID. Several `[BleService]` classes may share one UUID; the generator merges them into a single `AddService` call

## Reference Files

For detailed API signatures and examples, see:
- `reference/api-reference.md` - Full API surface, interfaces, enums, records, and usage examples
