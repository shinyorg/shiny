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
  - MTU
  - mtu
  - BleConstants
  - AttHeaderSize
  - Shiny.BluetoothLE.Hosting
  - ibeacon advertise
  - ble notify
  - ble notify cancellation
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
  - Framed
  - MaxMessageBytes
  - ble message framing
  - large gatt message
  - BleMessageFraming
  - BleMessageReassembler
  - BleMessageFrameResult
  - GattMessageExtensions
  - NotifyMessage
  - GetMessageReassembler
  - L2CapTicketBroker
  - L2CapTicket
  - L2CapTicketBrokerOptions
  - AddL2CapTicketBroker
  - L2CapTickets
  - L2CapTicketStatus
  - l2cap ticket
  - shared psm
  - L2CapChannelStream
  - AsStream
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
- Exchange requests, replies or notifications longer than one GATT operation (message framing)
- Share one L2CAP PSM between many authorised transfers with single-use tickets, or use a channel as a `Stream`

## Library Overview

- **NuGet**: `Shiny.BluetoothLE.Hosting` (Android, iOS/macOS, Mac Catalyst, Windows stub), `Shiny.BluetoothLE.Hosting.Linux` (Linux via BlueZ)
- **Namespaces**: `Shiny.BluetoothLE.Hosting`
- **Platforms**: iOS, Mac Catalyst, macOS (CoreBluetooth), Android, Linux (BlueZ). Windows throws `NotSupportedException` for advertising/GATT-server hosting; only the `OpenL2Cap` API is exposed and it also throws on Windows. **There is no tvOS target and there cannot be one** — `CBMutableService` and `CBMutableCharacteristic` have no constructors on tvOS, which is Apple's way of saying an Apple TV cannot act as a GATT peripheral. If asked to build a GATT server or advertise from tvOS, say it is impossible rather than generating code; the central role (`Shiny.BluetoothLE`) does support tvOS.
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

#### Requests and replies longer than one GATT operation

A write or notification carries at most `Mtu` bytes - 20 on a link that never negotiated more. When a
request or reply can be longer (JSON, a certificate, a scan result), set `Framed = true` rather than
inventing a chunking protocol:

```csharp
[RequestResponseCharacteristic("2A3B", Name = "Command", Framed = true, MaxMessageBytes = 16 * 1024)]
async Task<byte[]> Exchange(byte[] message, HeartRateServiceContext context, CancellationToken cancellationToken)
{
    var command = JsonSerializer.Deserialize(message, AppJsonContext.Default.Command)!;
    return JsonSerializer.SerializeToUtf8Bytes(await Run(command, cancellationToken), AppJsonContext.Default.Result);
}
```

The generator treats each write as one fragment, reassembles per central (the reassembler lives on that
central's context, so two centrals never interleave), and runs the handler **once** with the whole
message - `byte[]` and `WriteRequest.Data` are both the full message. Intermediate fragments are answered
`GattState.Success` without running the handler. The reply is split to the writing central's MTU with
`NotifyMessage`. A message over `MaxMessageBytes` (default 64 KB), or one with a dropped or reordered
fragment, is discarded, answered `GattState.Failure`, and reported to `OnBleHandlerError` as an
`InvalidDataException`.

The central **must** speak the same format - `WriteCharacteristicMessageAsync` to send and
`NotifyCharacteristicMessages` to receive, in `Shiny.BluetoothLE` (see the `shiny-bluetoothle` skill). A
plain `WriteCharacteristicAsync` against a framed characteristic is not a valid fragment. As with any
request/response characteristic, the central subscribes before it writes.

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

`IPeripheral.Mtu` (and `BleServiceContext.Mtu`) is the usable payload -- the negotiated ATT MTU
already minus the 3-byte ATT header. Cap a notification at `peripheral.Mtu` directly; do not
subtract the header again. Anything larger is silently truncated by the platform.

On iOS, Mac Catalyst and macOS `Notify` applies CoreBluetooth's back-pressure: when the transmit queue
is full it waits for `peripheralManagerIsReadyToUpdateSubscribers` and retries, so the task completes
only once the value is actually queued. Await each `Notify` before sending the next one -- do not fire
many in parallel with `Task.WhenAll`, and do not add your own delay or retry loop around it.

Pass a `CancellationToken` to bound that wait - it goes **before** the `params` centrals:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await characteristic.Notify(data, cts.Token);                  // all subscribers
await characteristic.Notify(data, cts.Token, context.Peripheral); // one central
```

If Bluetooth powers off while an Apple `Notify` is waiting, the task faults with `InvalidOperationException`.
Generated `[BleService]` classes get a matching `NotifyX(data, cancellationToken, params centrals)` overload,
and generated request/response replies already pass `BleHostToken`. An empty centrals list means every
subscriber on all platforms; a named list goes only to those centrals. `SubscribedCentrals` is tracked whether
or not `SetNotification` was given a subscribe hook, so never register a no-op hook just to populate it.

**Android** paces `Notify` per central - it waits for `onNotificationSent` before sending that central the next
value and throws if Android refuses the notification or reports a failed status. Do not add delays between
notifications. A central that enabled indications receives indications.

**Linux (BlueZ)** has two limits the code you generate must respect. BlueZ only tells the app whether *any*
central has notifications enabled, so while one is subscribed every connected central appears in
`SubscribedCentrals`. And every `Notify` reaches all subscribed centrals - the `centrals` argument cannot narrow
it (the send is skipped only when none of the named centrals is subscribed). Never put per-central data on a
notify characteristic that several centrals subscribe to on Linux; generated request/response replies fan out
to every subscriber there. Adding or removing a service re-registers the whole GATT application with BlueZ.

```csharp
[Notify]
public async Task Push(BleServiceContext context, byte[] payload)
{
    var max = context.Mtu;                       // NOT context.Mtu - 3
    await this.Characteristic.Notify(payload.Take(max).ToArray(), context.Peripheral);
}
```

#### Messages longer than one notification

Do not truncate a payload that has to arrive whole, and do not hand-roll chunking. `NotifyMessage` (in
`GattMessageExtensions`) splits it with `BleMessageFraming` - a one-byte header per fragment: bit 7 START,
bit 6 END, bits 0-5 a sequence number - sized to that central's MTU, and awaits each notification in turn:

```csharp
await characteristic.NotifyMessage(bigPayload, context.Peripheral, cancellationToken);
```

It addresses **one** central, because fragments are sized per central. Never send two messages to the same
central on the same characteristic concurrently - the fragments interleave and the central discards both.
The central receives with `NotifyCharacteristicMessages` (`Shiny.BluetoothLE`).

For framed **writes** outside `[RequestResponseCharacteristic(Framed = true)]` - a `[WriteCharacteristic]`
handler, say - feed each write into the reassembler kept on the central's context:

```csharp
[WriteCharacteristic("2A3C")]
async Task<GattState> Upload(WriteRequest request, HeartRateServiceContext context)
{
    var reassembler = context.GetMessageReassembler("2A3C", maxMessageBytes: 32 * 1024);

    BleMessageFrameResult frame;
    byte[]? message;
    lock (reassembler)
        frame = reassembler.Push(request.Data, out message);

    if (frame == BleMessageFrameResult.Partial)
        return GattState.Success;

    if (frame != BleMessageFrameResult.Complete)
        return GattState.Failure;          // Malformed / OutOfSequence / TooLarge - already reset

    await this.Store(message!);
    return GattState.Success;
}
```

`GetMessageReassembler` is one instance per context and characteristic UUID (case-insensitive), created on
first use and gone with the central's context. With the imperative `SetWrite` API there is no context - keep
a `BleMessageReassembler` per central yourself (keyed by `request.Peripheral.Uuid`), never one shared across
centrals.

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

#### A channel as a `Stream`

`channel.AsStream()` (`L2CapChannelStream`, namespace `Shiny.BluetoothLE`) wraps an open channel as a
`System.IO.Stream` for anything that reads or writes streams - `CopyToAsync`, `JsonSerializer`, a hash:

```csharp
await using var stream = channel.AsStream(maxWriteSize: 4096);   // disposing closes the channel unless leaveOpen: true
await source.CopyToAsync(stream, cancellationToken);
```

- **Async only** - synchronous `Read`/`Write` throw `NotSupportedException`. Not seekable.
- Create it **as soon as the channel opens**: it subscribes to `DataReceived` on construction, and bytes the
  peer sends before anything is subscribed are lost.
- Writes larger than `MaxWriteSize` (default 4096, settable after creation) are split. A read returns 0 when
  the peer closes the channel.
- It shares the buffered reader the file-transfer helpers use, so a stream and `UploadFile`/`ReadFileRequest`
  can take turns on one channel without losing bytes.

#### One PSM, many transfers (`L2CapTicketBroker`)

Do **not** open a PSM per transfer - PSMs come from a small dynamic range, and a released one can be handed
straight to another process, so a PSM you gave a central can go stale before it connects. When a device offers
several bulk transfers (a camera frame, a log dump, a firmware image) use the ticket broker: one listener, and
each channel claims its transfer with a single-use token.

```csharp
builder.Services.AddBluetoothLeHosting();
builder.Services.AddL2CapTicketBroker(o =>
{
    o.Secure = true;                                  // default; the central must open with the same setting
    o.HandshakeTimeout = TimeSpan.FromSeconds(15);    // default; a silent channel is closed after this
    o.MaxWriteSize = 4096;                            // default per-write size on claimed channels
});
```

Reserve a ticket from an **authenticated** route - typically a GATT command - and return `ticket.Psm` and
`ticket.Token` to that central only:

```csharp
[BleService("7a5e0000-5c2d-4f5e-9b1a-3c6d2e8f1a00")]
public partial class CameraService(L2CapTicketBroker broker, ICamera camera)
{
    [RequestResponseCharacteristic("7a5e0001-5c2d-4f5e-9b1a-3c6d2e8f1a00", Name = "Capture", Framed = true)]
    async Task<byte[]> RequestCapture(CameraServiceContext context, CancellationToken ct)
    {
        if (!context.IsAuthenticated)                     // your own property on the partial context
            return Error("not authorised");                 // Error/Encode: your reply format

        var ticket = await broker.Reserve(
            label: "capture",
            lifetime: TimeSpan.FromSeconds(30),               // unclaimed token expires after this
            handler: async (stream, token) =>
            {
                // stream is an L2CapChannelStream; it is closed when this returns, which ends the transfer
                await using var jpeg = await camera.Capture(token);
                await jpeg.CopyToAsync(stream, token);
            },
            maxWriteSize: 8192                                // optional, overrides the broker default
        );

        return Encode(ticket.Psm, ticket.Token);
    }
}
```

- `Reserve` opens the listener on first use; `broker.Psm` is 0 until then. Every ticket shares the same PSM.
- The first channel to present the token gets the handler. A wrong, expired or already-claimed token - or a
  channel that sends nothing within `HandshakeTimeout` - is answered (`UnknownTicket`, `AlreadyClaimed`,
  `Malformed`, `VersionMismatch`) and closed without reaching any handler.
- `broker.Release(token)` withdraws an unclaimed ticket **and** cancels the handler's token if one is running -
  call it when the central abandons the transfer (a cancel command, a disconnect).
- Handler exceptions and cancellations are logged by the broker, not rethrown - report failures to the central
  yourself before returning.
- `PendingTickets` counts issued tickets not yet finished, released or expired. Disposing the broker cancels
  every ticket and closes the listener.
- The token is the only thing that authorises the channel. Never advertise it or put it on a readable
  characteristic.

The central claims it with `peripheral.OpenL2CapTicketChannel(psm, token)` (see the `shiny-bluetoothle`
skill). The wire format is `L2CapTickets`: the central sends `"SL2T" [version:1] [length:1] [token:32 ASCII]`,
the host answers `"SL2T" [version:1] [status:1]`, and only after `Accepted` does the channel carry the transfer.

### 8. File Organization

- Group hosting services in a `BleHosting/` folder, one class per GATT service
- Or by feature: `Features/{Feature}/{Name}HostingService.cs`

## Namespace Ambiguities

- **`IPeripheral`**: Both `Shiny.BluetoothLE` (client) and `Shiny.BluetoothLE.Hosting` define an `IPeripheral` interface with different members. If both packages are referenced in the same project, do NOT add both namespaces as global usings. Use file-level `using` directives or FQN (`Shiny.BluetoothLE.Hosting.IPeripheral`) to disambiguate.

## Best Practices

1. **Always request access first** -- call `RequestAccess()` and check the result before any hosting operations, so a denied permission or a switched-off adapter reaches the user rather than surfacing as a thrown `InvalidOperationException` deeper in. On Apple platforms it is no longer required for correctness: as of 5.6, `AddService(...)` and `StartAdvertising(...)` wait out the `CBPeripheralManager` power-on handshake themselves. Before that they issued the native call against a manager still reporting `Unknown`, which CoreBluetooth drops without ever invoking the completion delegate both methods await -- so a call made at app startup hung indefinitely instead of failing. Do not work around it with your own `await RequestAccess()` retry loop or a `Task.Delay` before advertising
2. **Reach for `[BleService]` first** -- the generator emits the same builder calls plus the response/offset handling, subscriber tracking, and per-central context. Fall back to `AddService(uuid, primary, sb => ...)` lambdas when the service shape is only known at runtime
3. **Always write the full 128-bit UUID when calling `AddService` by hand** -- short forms like `"180D"` work on Apple (`CBUUID.FromString`) but throw on Android (`java.util.UUID.fromString`). The generator normalizes for you; the imperative API does not
4. **Respond to writes when needed** -- always check `WriteRequest.IsReplyNeeded` and call `Respond` with the appropriate `GattState`
5. **Return GattResult.Error on failures** -- use `GattResult.Error(GattState.Failure)` in read handlers when an error occurs
6. **Stop advertising before cleanup** -- call `StopAdvertising()` and `ClearServices()` when done
7. **Check IsAdvertising** -- avoid calling `StartAdvertising` if already advertising
8. **Dispose `L2CapInstance` and per-central `L2CapChannel`s explicitly** -- disposing the instance closes the listener but does not auto-close already-open channels. With `[L2CapService]` the generator disposes each channel when the handler returns, and the `BleHostedServiceSession` closes the listener
9. **Keep the `BleHostedServiceSession` alive** -- `AttachBleHostedServices` returns it, and disposing it cancels in-flight handlers, closes L2CAP listeners, and removes the GATT services
10. **Size notifications with `Mtu` as-is** -- `IPeripheral.Mtu`/`BleServiceContext.Mtu` is the payload (ATT MTU minus `BleConstants.AttHeaderSize`), not the ATT MTU. Add the header back with `+ BleConstants.AttHeaderSize` only when feeding an API that genuinely wants an ATT MTU
11. **Never register the same service UUID twice** -- `BleHostingManager` keys services by UUID. Several `[BleService]` classes may share one UUID; the generator merges them into a single `AddService` call
12. **Frame anything that may not fit in `Mtu`** -- `[RequestResponseCharacteristic(Framed = true)]`, `NotifyMessage`, or `GetMessageReassembler`, paired with `WriteCharacteristicMessageAsync` / `NotifyCharacteristicMessages` on the central. Never truncate, and never hand-roll a chunk counter
13. **Share one PSM through `L2CapTicketBroker`** -- one `Reserve` per transfer, the token handed out over an authenticated GATT route, instead of an `OpenL2Cap` per transfer

## Reference Files

For detailed API signatures and examples, see:
- `reference/api-reference.md` - Full API surface, interfaces, enums, records, and usage examples
