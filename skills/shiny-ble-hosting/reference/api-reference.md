# API Reference

## Installation

```bash
dotnet add package Shiny.BluetoothLE.Hosting
```

## Namespaces

```csharp
using Shiny;                                // AccessState, ServiceCollectionExtensions
using Shiny.BluetoothLE;                    // CharacteristicProperties
using Shiny.BluetoothLE.Hosting;            // All hosting interfaces, enums, records, and extensions
```

> The `[BleService]` / `[L2CapService]` source generator (`Shiny.BluetoothLE.Hosting.SourceGenerators`), its
> attributes, `BleServiceContext`, `BleContextStore`, `BleHostingRuntime`, `BleL2CapContext`,
> `BleHostedServiceSession`, `BleSubscription`, the generated `AddBleHostedServices()` /
> `AttachBleHostedServices(...)` members and the `SBH0xx` diagnostics were **removed in v6**. Register services
> with `IBleHostingManager.AddService(...)` and listeners with `OpenL2Cap(...)` directly.

## IBleHostingManager Interface

The primary service for BLE peripheral hosting. Injected via DI as a singleton.

```csharp
// Manages BLE peripheral hosting including advertising and GATT server services
public interface IBleHostingManager
{
    // Requests access for BLE hosting operations
    // advertise: whether to request advertising access
    // connect: whether to request GATT connection access
    Task<AccessState> RequestAccess(bool advertise = true, bool connect = true);

    // Gets the current advertising access state
    AccessState AdvertisingAccessStatus { get; }

    // Gets the current GATT server access state
    AccessState GattAccessStatus { get; }

    // Gets whether the device is currently advertising
    bool IsAdvertising { get; }

    // Starts BLE advertising
    Task StartAdvertising(AdvertisementOptions? options = null);

    // Stops BLE advertising
    void StopAdvertising();

    // Publishes an L2CAP PSM and listens for incoming central connections.
    // Each accepted connection invokes `onOpen` with the opened channel.
    // Dispose the returned `L2CapInstance` to unpublish the PSM and stop accepting.
    // - secure: when true, the channel requires encryption/authentication (Android API 29+)
    Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen);

    // Advertises as an iBeacon
    // uuid: the beacon proximity UUID
    // major: the beacon major value
    // minor: the beacon minor value
    // txpower: optional transmit power
    Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null);

    // Adds a GATT service to the local GATT server
    // uuid: the service UUID
    // primary: whether this is a primary service
    // serviceBuilder: action to configure the service characteristics
    Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);

    // Removes a GATT service by UUID
    void RemoveService(string serviceUuid);

    // Removes all GATT services from the server
    void ClearServices();

    // Gets the list of active GATT services
    IReadOnlyList<IGattService> Services { get; }
}
```

## IGattService Interface

Represents a GATT service hosted on the local BLE peripheral.

```csharp
// Represents a GATT service hosted on the local BLE peripheral
public interface IGattService
{
    // Gets the service UUID
    string Uuid { get; }

    // Gets whether this is a primary service
    bool Primary { get; }

    // Gets the characteristics belonging to this service
    IReadOnlyList<IGattCharacteristic> Characteristics { get; }
}
```

## IGattServiceBuilder Interface

Builds a GATT service by adding characteristics.

```csharp
// Builds a GATT service by adding characteristics
public interface IGattServiceBuilder
{
    // Adds a characteristic to the service
    // uuid: the characteristic UUID
    // characteristicBuilder: action to configure the characteristic
    IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder);
}
```

## IGattCharacteristic Interface

Represents a GATT characteristic hosted on the local BLE peripheral.

```csharp
// Represents a GATT characteristic hosted on the local BLE peripheral
public interface IGattCharacteristic
{
    // Gets the characteristic UUID
    string Uuid { get; }

    // Gets the characteristic properties (read, write, notify, etc.)
    CharacteristicProperties Properties { get; }

    // Sends a notification to subscribed centrals
    // data: the data to send
    // centrals: specific centrals to notify, or all subscribed if empty
    Task Notify(byte[] data, params IPeripheral[] centrals);

    // Same, with a token bounding the platform back-pressure wait (Apple transmit queue, Android pacing).
    // The token goes BEFORE the params centrals
    Task Notify(byte[] data, CancellationToken cancellationToken, params IPeripheral[] centrals);

    // Gets the list of centrals currently subscribed to notifications
    IReadOnlyList<IPeripheral> SubscribedCentrals { get; }
}
```

## IGattCharacteristicBuilder Interface

Builds a GATT characteristic by configuring read, write, and notification handlers. Supports fluent chaining.

```csharp
// Builds a GATT characteristic by configuring read, write, and notification handlers
public interface IGattCharacteristicBuilder
{
    // Configures notification/indication support for the characteristic
    // onSubscribe: optional callback when a central subscribes or unsubscribes
    // options: the notification type (notify or indicate)
    IGattCharacteristicBuilder SetNotification(
        Func<CharacteristicSubscription, Task>? onSubscribe = null,
        NotificationOptions options = NotificationOptions.Notify
    );

    // Configures write support for the characteristic
    // request: the write request handler
    // options: the write type (write with response or write without response)
    IGattCharacteristicBuilder SetWrite(
        Func<WriteRequest, Task> request,
        WriteOptions options = WriteOptions.Write
    );

    // Configures read support for the characteristic
    // request: the read request handler
    // encrypted: whether the read requires encryption
    IGattCharacteristicBuilder SetRead(
        Func<ReadRequest, Task<GattResult>> request,
        bool encrypted = false
    );
}
```

## IPeripheral Interface

Represents a connected central device.

```csharp
public interface IPeripheral
{
    // The connection ID
    string Uuid { get; }

    // Usable payload per GATT operation = negotiated ATT MTU - 3 (ATT header).
    // Cap notification/read payloads at exactly this value - do NOT subtract 3 again. Starts at 20.
    int Mtu { get; }

    // You can set any data you want here (user context)
    object? Context { get; set; }
}
```

## Records

### AdvertisementOptions

Configuration for BLE advertising.

```csharp
public record AdvertisementOptions(
    // Set the local name of the advertisement
    string? LocalName = null,
    // GATT service UUIDs to advertise
    params string[] ServiceUuids
);
```

### CharacteristicSubscription

Fired when a central subscribes or unsubscribes from notifications.

```csharp
public record CharacteristicSubscription(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    bool IsSubscribing
);
```

### ReadRequest

Passed to read request handlers.

```csharp
public record ReadRequest(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    int Offset
);
```

### WriteRequest

Passed to write request handlers.

```csharp
public record WriteRequest(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    byte[] Data,
    int Offset,
    bool IsReplyNeeded,
    Action<GattState> Respond
);
```

### GattResult

Return type for read request handlers. Includes static factory methods.

```csharp
public record GattResult(
    GattState Status,
    byte[]? Data
)
{
    // Create a success result with data
    public static GattResult Success(byte[] data);

    // Create an error result with a status code
    public static GattResult Error(GattState status);
}
```

### L2CapChannel

An open L2CAP Connection-Oriented Channel. Lives in `Shiny.BluetoothLE.Common` (namespace `Shiny.BluetoothLE`) so the same record is shared with the client/central library.

```csharp
public record L2CapChannel(
    ushort Psm,                                    // PSM the channel was opened on
    string Identifier,                             // Identifier of the connecting central
    Func<byte[], IObservable<Unit>> Write,         // Returns an observable that completes when bytes are queued
    IObservable<byte[]> DataReceived,              // Hot; completes on remote close, OnError on I/O failure
    Action? OnDispose = null                       // Optional cleanup invoked by Dispose()
) : IDisposable
{
    public void Dispose();                         // Closes streams / disposes the socket
}
```

### L2CapInstance

Disposable handle bound to a listening PSM. Dispose to unpublish the PSM and stop accepting new connections.

```csharp
public struct L2CapInstance : IDisposable
{
    public L2CapInstance(ushort psm, Action onDispose);
    public ushort Psm { get; }
    public void Dispose();
}
```

Disposing the instance does **not** close already-open per-central channels — dispose each `L2CapChannel` explicitly if you need to terminate active connections. The one exception is the L2CAP file server below: its instance also cancels the per-channel serve loops, which closes their channels.

### L2CAP File Server (L2CapFileServerExtensions)

```csharp
public static class L2CapFileServerExtensions
{
    // Directory backed server - centrals upload into / download out of rootDirectory
    static Task<L2CapInstance> OpenL2CapFileServer(this IBleHostingManager hosting, string rootDirectory, bool secure = false, Action<L2CapFileServerOptions>? configure = null);
    static Task<L2CapInstance> OpenL2CapFileServer(this IBleHostingManager hosting, L2CapFileServerOptions options);

    // Bring your own handler - every inbound request is passed to onRequest, one at a time per channel
    static Task<L2CapInstance> HandleL2CapRequests(
        this IBleHostingManager hosting,
        bool secure,
        Func<L2CapFileRequest, CancellationToken, Task> onRequest,
        L2CapTransferOptions? options = null,
        Action<L2CapFileRequest?, Exception>? onError = null
    );
}
```

`onRequest` must answer every request (an accept overload or `Reject`) before returning — an unanswered
request is auto-rejected with `Unknown` so the peer is never left hanging.

### L2CapFileServerOptions

```csharp
public class L2CapFileServerOptions
{
    public L2CapFileServerOptions(string rootDirectory);

    string RootDirectory { get; }                   // created if missing; peer names are resolved under it
    bool Secure { get; set; }                       // false
    bool AllowUploads { get; set; }                 // true
    bool AllowDownloads { get; set; }               // true
    bool OverwriteExistingUploads { get; set; }     // true
    long? MaxUploadSize { get; set; }               // null = no limit; refused as TooLarge pre-body
    L2CapTransferOptions Transfer { get; set; }     // buffer size / progress interval / idle timeout

    Func<L2CapFileRequest, bool>? Authorize { get; set; }        // false => NotPermitted
    Action<L2CapFileTransferEvent>? OnProgress { get; set; }
    Action<L2CapFileServerResult>? OnCompleted { get; set; }
    Action<L2CapFileRequest?, Exception>? OnError { get; set; }
}

public record L2CapFileTransferEvent(string PeerIdentifier, L2CapTransferType Type, string FileName, TransferProgress Progress);
public record L2CapFileServerResult(string PeerIdentifier, string LocalFilePath, L2CapTransferResult Result);
```

Peer-supplied names are resolved with `Path.GetFullPath` under `RootDirectory`; absolute paths and
`../` traversal are refused with `NotPermitted` before any filesystem access.

### L2CapFileRequest (class, namespace `Shiny.BluetoothLE`)

Returned by `channel.ReadFileRequest(...)` and handed to `HandleL2CapRequests`. Exactly one accept or
reject call per request.

```csharp
public sealed class L2CapFileRequest
{
    L2CapTransferType Type { get; }          // Upload = central is sending you a file; Download = it wants one
    string FileName { get; }                 // untrusted - validate before using as a path
    long Size { get; }                       // bytes the peer will send; 0 for a download request
    string PeerIdentifier { get; }
    ushort Psm { get; }
    bool IsAnswered { get; }

    Task<L2CapTransferResult> AcceptUpload(string localFilePath, Action<TransferProgress>? onProgress = null, CancellationToken ct = default);
    Task<L2CapTransferResult> AcceptUpload(Stream destination, Action<TransferProgress>? onProgress = null, CancellationToken ct = default);
    Task<L2CapTransferResult> AcceptDownload(string localFilePath, Action<TransferProgress>? onProgress = null, CancellationToken ct = default);
    Task<L2CapTransferResult> AcceptDownload(Stream source, long length, Action<TransferProgress>? onProgress = null, CancellationToken ct = default);
    Task Reject(L2CapTransferError error = L2CapTransferError.NotPermitted, string? message = null, CancellationToken ct = default);
}
```

`L2CapTransferOptions`, `L2CapTransferResult`, `L2CapTransferError`, and `L2CapTransferException` live in
`Shiny.BluetoothLE.Common` and are shared with the client library — see the `shiny-bluetoothle` skill
reference for their shapes.

### L2CapChannelStream (class, namespace `Shiny.BluetoothLE`)

An open `L2CapChannel` as a `System.IO.Stream`. Lives in `Shiny.BluetoothLE.Common`, so it is the same type on
both sides of a channel.

```csharp
public sealed class L2CapChannelStream : Stream
{
    public const int DefaultMaxWriteSize = 4096;

    public L2CapChannelStream(L2CapChannel channel, int maxWriteSize = DefaultMaxWriteSize, bool leaveOpen = false);

    public L2CapChannel Channel { get; }
    public int MaxWriteSize { get; set; }       // larger writes are split; settable after creation
    public long BytesRead { get; }
    public long BytesWritten { get; }

    // ReadAsync / WriteAsync only - Read / Write throw NotSupportedException
    // CanRead = CanWrite = true, CanSeek = false; Length / Position / Seek / SetLength throw
    // Dispose closes the channel unless leaveOpen
}

public static class L2CapChannelStreamExtensions
{
    static L2CapChannelStream AsStream(this L2CapChannel channel, int maxWriteSize = L2CapChannelStream.DefaultMaxWriteSize, bool leaveOpen = false);
}
```

The stream subscribes to `DataReceived` when it is created - create it as soon as the channel opens, or bytes
the peer sends first are lost. It reads through the same buffered reader as the file-transfer helpers, so the two
can take turns on one channel. A read returns 0 once the peer closes the channel.

### L2CapTicketBroker (class, namespace `Shiny.BluetoothLE.Hosting`)

One L2CAP listener shared by every bulk transfer, each channel claimed with a single-use ticket.

```csharp
public sealed class L2CapTicketBroker : IDisposable
{
    public L2CapTicketBroker(IBleHostingManager hosting, L2CapTicketBrokerOptions? options = null, ILogger<L2CapTicketBroker>? logger = null);

    public ushort Psm { get; }                  // 0 until the first Reserve opens the listener
    public int PendingTickets { get; }          // issued, not yet finished / released / expired

    // opens the listener on first use; cancellationToken cancels opening it, not the transfer
    public Task<L2CapTicket> Reserve(
        string label,                                               // for logs
        TimeSpan lifetime,                                          // must be positive; unclaimed token expires after it
        Func<L2CapChannelStream, CancellationToken, Task> handler,  // stream closed when it returns; token cancels on Release/Dispose
        int? maxWriteSize = null,                                   // defaults to options.MaxWriteSize
        CancellationToken cancellationToken = default
    );

    public void Release(string token);          // withdraws the ticket AND cancels a running handler
    public void Dispose();                      // cancels every ticket, closes the listener
}

public class L2CapTicketBrokerOptions
{
    public bool Secure { get; set; }                    // true - the central must open with the same setting
    public TimeSpan HandshakeTimeout { get; set; }      // 15s - how long an arriving channel has to present its ticket
    public int MaxWriteSize { get; set; }               // 4096
}

public sealed class L2CapTicket : IDisposable
{
    public string Token { get; }                // 32 chars, single use - give it only to the central the transfer is for
    public string Label { get; }
    public DateTimeOffset ExpiresUtc { get; }
    public ushort Psm { get; }
    public int MaxWriteSize { get; }            // worth passing to the central too
    public bool IsClaimed { get; }
    public bool HasExpired { get; }             // says nothing about a claimed transfer still running
    public CancellationToken CancellationToken { get; }
    public void Cancel();
}

public static class L2CapTicketBrokerServiceCollectionExtensions
{
    // singleton; needs AddBluetoothLeHosting()
    static IServiceCollection AddL2CapTicketBroker(this IServiceCollection services, Action<L2CapTicketBrokerOptions>? configure = null);
}
```

A channel that presents an unknown or expired token is answered `UnknownTicket`, one that presents a claimed token
`AlreadyClaimed`, anything that is not a hello `Malformed` (or `VersionMismatch`), and a channel silent past
`HandshakeTimeout` is closed - none of them reach a handler. Handler exceptions and `OperationCanceledException`
are logged, not rethrown.

### L2CapTickets (static class, namespace `Shiny.BluetoothLE`)

The handshake, in `Shiny.BluetoothLE.Common`. The broker and the central's `ClaimTicket` /
`OpenL2CapTicketChannel` use it for you - call it directly only to speak the protocol from something else.

```
central -> host   hello   "SL2T" [version:1] [length:1] [token:32 ASCII]
host -> central   accept  "SL2T" [version:1] [status:1]
```

```csharp
public static class L2CapTickets
{
    public const byte ProtocolVersion = 1;
    public const int TokenLength = 32;
    public const int HelloLength = 38;
    public const int AcceptLength = 6;

    static string CreateToken();
    static byte[] CreateHello(string token);
    static L2CapTicketStatus ReadHello(ReadOnlySpan<byte> frame, out string token);
    static byte[] CreateAccept(L2CapTicketStatus status);
    static L2CapTicketStatus ReadAccept(ReadOnlySpan<byte> frame);

    // central side - see the shiny-bluetoothle skill
    static Task<L2CapChannelStream> ClaimTicket(this L2CapChannel channel, string token, int maxWriteSize = 4096, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}

public enum L2CapTicketStatus : byte
{
    Accepted = 0, UnknownTicket = 1, VersionMismatch = 2, Malformed = 3, AlreadyClaimed = 4, Busy = 5
}
```

### Message framing (BleMessageFraming / BleMessageReassembler, namespace `Shiny.BluetoothLE`)

Carries messages longer than one GATT operation. Lives in `Shiny.BluetoothLE.Common`; the central side is
`WriteCharacteristicMessageAsync` / `NotifyCharacteristicMessages` in `Shiny.BluetoothLE`.

Every fragment starts with one header byte - bit 7 START, bit 6 END, bits 0-5 a sequence number wrapping at 64.
A message that fits in one fragment has both flags set, so a short message costs one byte.

```csharp
public static class BleMessageFraming
{
    public const int MinimumPayload = 20;
    public const int HeaderSize = 1;
    public const int DefaultMaxMessageBytes = 64 * 1024;

    static int PayloadPerFragment(int mtu);                                 // max(20, mtu) - 1; mtu is IPeripheral.Mtu
    static IReadOnlyList<byte[]> Encode(ReadOnlySpan<byte> message, int mtu); // empty message -> one fragment
}

// one per link, per characteristic, per direction - never shared. Not thread-safe; lock around Push
public sealed class BleMessageReassembler
{
    public BleMessageReassembler(int maxMessageBytes = BleMessageFraming.DefaultMaxMessageBytes);
    public bool IsInMessage { get; }
    public void Reset();
    public BleMessageFrameResult Push(ReadOnlySpan<byte> fragment, out byte[]? message);
}

public enum BleMessageFrameResult { Partial, Complete, Malformed, OutOfSequence, TooLarge }
```

Every error result has already reset the reassembler, so the next START fragment begins cleanly. A START that
arrives mid-message discards the partial one.

### GattMessageExtensions (namespace `Shiny.BluetoothLE.Hosting`)

```csharp
public static class GattMessageExtensions
{
    // split to the central's MTU, each notification awaited in turn. One central; never two concurrent
    // messages to the same central on the same characteristic
    static Task NotifyMessage(this IGattCharacteristic characteristic, byte[] message, IPeripheral central, CancellationToken cancellationToken = default);
}
```

There is no hosting-side helper for framed **writes**. Keep a `BleMessageReassembler` per central yourself - e.g. a
`ConcurrentDictionary<string, BleMessageReassembler>` keyed by `IPeripheral.Uuid`, removed when the central
unsubscribes or disconnects - and `Push` each `WriteRequest.Data` fragment into it (see the framed
request/response example below). Never share one reassembler across centrals.

### L2CapChannelAsyncExtensions (namespace `Shiny.BluetoothLE.Hosting`)

```csharp
public static class L2CapChannelAsyncExtensions
{
    // IAsyncEnumerable over L2CapChannel.DataReceived - completes when the peer closes the channel,
    // throws on an I/O error. Subscribes when enumeration starts
    static IAsyncEnumerable<byte[]> ReadAll(this L2CapChannel channel, CancellationToken cancellationToken = default);
}
```

### TransferProgress (record, namespace `Shiny.BluetoothLE`)

Reports transfer metrics for the L2CAP file server and `L2CapChannelExtensions.SendFile(...)`. Intentionally identical in shape to `Shiny.Net.Http.TransferProgress`.

```csharp
public record TransferProgress(
    long BytesPerSecond,
    long? BytesToTransfer,        // null when length is unknown
    long BytesTransferred
)
{
    public static TransferProgress Empty { get; }
    public bool IsDeterministic { get; }                // BytesToTransfer != null
    public double PercentComplete { get; }              // 0.0–1.0, or -1 when not deterministic
    public TimeSpan EstimatedTimeRemaining { get; }     // Zero when unknown
}
```

### L2CapChannel File Transfer (L2CapChannelExtensions)

Helpers for streaming a file over a connected `L2CapChannel`, with progress callbacks emitting `TransferProgress` snapshots roughly every two seconds plus one final emission on completion.

```csharp
public static class L2CapChannelExtensions
{
    // Send a file by path. Length is read from the file and used as BytesToTransfer.
    static Task SendFile(
        this L2CapChannel channel,
        string filePath,
        int bufferSize = 4096,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    );

    // Send an arbitrary stream. Pass totalBytes to enable percent-complete and ETA.
    static Task SendFile(
        this L2CapChannel channel,
        Stream source,
        long? totalBytes = null,
        int bufferSize = 4096,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    );
}
```

## Enums

### GattState

Status codes for GATT operations.

```csharp
public enum GattState
{
    Success = 0,
    ReadNotPermitted = 2,
    WriteNotPermitted = 3,
    InsufficientAuthentication = 5,
    RequestNotSupported = 6,
    InvalidOffset = 7,
    InvalidAttributeLength = 13,
    InsufficientEncryption = 15,
    InsufficientResources = 143,
    Failure = 257
}
```

### NotificationOptions

Flags enum for notification configuration.

```csharp
[Flags]
public enum NotificationOptions
{
    Notify,
    Indicate,
    EncryptionRequired
}
```

### WriteOptions

Flags enum for write configuration.

```csharp
[Flags]
public enum WriteOptions
{
    Write,
    WriteWithoutResponse,
    AuthenticatedSignedWrites,
    EncryptionRequired
}
```

### CharacteristicProperties

Flags enum for characteristic capabilities. Defined in `Shiny.BluetoothLE` (shared with the BLE client library).

```csharp
[Flags]
public enum CharacteristicProperties
{
    Broadcast = 1,
    Read = 2,
    WriteWithoutResponse = 4,
    Write = 8,
    Notify = 16,
    Indicate = 32,
    AuthenticatedSignedWrites = 64,
    ExtendedProperties = 128,
    NotifyEncryptionRequired = 256,
    IndicateEncryptionRequired = 512
}
```

### AccessState

Permission states for BLE operations. Defined in `Shiny` namespace (from Shiny.Core).

```csharp
public enum AccessState
{
    Unknown,
    NotSupported,
    NotSetup,
    Disabled,
    Restricted,
    Denied,
    Available
}
```

## Extension Methods

### ServiceCollectionExtensions

DI registration methods in the `Shiny` namespace.

```csharp
public static class ServiceCollectionExtensions
{
    // Registers IBleHostingManager in the DI container
    public static IServiceCollection AddBluetoothLeHosting(this IServiceCollection services);
}

// namespace Shiny.BluetoothLE.Hosting
public static class L2CapTicketBrokerServiceCollectionExtensions
{
    // Registers a singleton L2CapTicketBroker; needs AddBluetoothLeHosting()
    public static IServiceCollection AddL2CapTicketBroker(this IServiceCollection services, Action<L2CapTicketBrokerOptions>? configure = null);
}
```

## Usage Examples

### Basic GATT Server with Read + Write + Notify

```csharp
public class BleHostViewModel(IBleHostingManager hostingManager)
{
    public async Task StartServer()
    {
        var access = await hostingManager.RequestAccess();
        if (access != AccessState.Available)
            return;

        await hostingManager.AddService(
            "12345678-1234-1234-1234-123456789abc",
            true,
            sb =>
            {
                sb.AddCharacteristic("12345678-1234-1234-1234-123456789ab1", cb =>
                {
                    cb.SetRead(request =>
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes("Hello from peripheral");
                        return Task.FromResult(GattResult.Success(bytes));
                    });

                    cb.SetWrite(request =>
                    {
                        var text = System.Text.Encoding.UTF8.GetString(request.Data);
                        Console.WriteLine($"Received: {text}");
                        if (request.IsReplyNeeded)
                            request.Respond(GattState.Success);
                        return Task.CompletedTask;
                    }, WriteOptions.Write);

                    cb.SetNotification(sub =>
                    {
                        Console.WriteLine(sub.IsSubscribing
                            ? $"Central {sub.Peripheral.Uuid} subscribed"
                            : $"Central {sub.Peripheral.Uuid} unsubscribed");
                        return Task.CompletedTask;
                    }, NotificationOptions.Notify);
                });
            }
        );

        await hostingManager.StartAdvertising(new AdvertisementOptions(
            LocalName: "MyPeripheral",
            ServiceUuids: "12345678-1234-1234-1234-123456789abc"
        ));
    }

    public void StopServer()
    {
        hostingManager.StopAdvertising();
        hostingManager.ClearServices();
    }
}
```

### Request/Response Characteristic with Per-Central State

A GATT write response cannot carry a payload, so the reply travels back as a notification on the same
characteristic. Per-central state goes on `IPeripheral.Context` (or your own dictionary keyed by
`IPeripheral.Uuid`).

```csharp
public class CommandService(IBleHostingManager hosting)
{
    const string ServiceUuid = "12345678-1234-1234-1234-123456789abc";
    const string CommandUuid = "12345678-1234-1234-1234-123456789ab3";

    readonly ConcurrentDictionary<string, BleMessageReassembler> reassemblers = new();

    public Task Start() => hosting.AddService(ServiceUuid, true, sb =>
        sb.AddCharacteristic(CommandUuid, cb => cb
            .SetNotification(sub =>
            {
                if (!sub.IsSubscribing)
                    this.reassemblers.TryRemove(sub.Peripheral.Uuid, out _);
                return Task.CompletedTask;
            })
            .SetWrite(async request =>
            {
                // framed: one reassembler per central, never shared
                var reassembler = this.reassemblers.GetOrAdd(request.Peripheral.Uuid, _ => new BleMessageReassembler());

                BleMessageFrameResult frame;
                byte[]? message;
                lock (reassembler)
                    frame = reassembler.Push(request.Data, out message);

                var complete = frame == BleMessageFrameResult.Complete;
                if (request.IsReplyNeeded)
                    request.Respond(complete || frame == BleMessageFrameResult.Partial ? GattState.Success : GattState.Failure);

                if (complete)
                {
                    request.Peripheral.Context ??= new CommandSession();     // your own per-central state
                    var reply = await this.Handle((CommandSession)request.Peripheral.Context, message!);
                    await request.Characteristic.NotifyMessage(reply, request.Peripheral);
                }
            })
        )
    );
}
```

The central pairs this with `NotifyCharacteristicMessages` (subscribe first) and `WriteCharacteristicMessageAsync`.
For an unframed exchange, drop the reassembler and reply with `request.Characteristic.Notify(reply, request.Peripheral)`.

### L2CAP Listener with the PSM Published over GATT

```csharp
var instance = await hostingManager.OpenL2Cap(secure: false, onOpen: channel => _ = Echo(channel));

// a central has no other in-band way to learn the PSM - serve it as two little-endian bytes
await hostingManager.AddService("12345678-1234-1234-1234-123456789abd", true, sb =>
    sb.AddCharacteristic("12345678-1234-1234-1234-123456789abe", cb => cb.SetRead(_ =>
    {
        var psm = instance.Psm;
        return Task.FromResult(GattResult.Success(new[] { (byte)(psm & 0xFF), (byte)(psm >> 8) }));
    }))
);

static async Task Echo(L2CapChannel channel)
{
    using (channel)
    {
        await foreach (var buffer in channel.ReadAll())
            await channel.Write(buffer).ToTask();
    }
}
```

Open the listener before `AddService` so an immediate read returns a live PSM. Dispose `instance` to stop
listening and `RemoveService(...)` to withdraw the characteristic.

### iBeacon Broadcasting

```csharp
var access = await hostingManager.RequestAccess(advertise: true, connect: false);
if (access == AccessState.Available)
{
    await hostingManager.AdvertiseBeacon(
        uuid: Guid.Parse("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0"),
        major: 1,
        minor: 100,
        txpower: -59
    );
}
```

### Multiple Services with Multiple Characteristics

```csharp
// Battery service - always the full 128-bit form; "180F" throws on Android
await hostingManager.AddService("0000180f-0000-1000-8000-00805f9b34fb", true, sb =>
{
    sb.AddCharacteristic("00002a19-0000-1000-8000-00805f9b34fb", cb =>
    {
        cb.SetRead(request =>
        {
            var level = new byte[] { 85 }; // 85%
            return Task.FromResult(GattResult.Success(level));
        });

        cb.SetNotification();
    });
});

// Custom data service
await hostingManager.AddService("12345678-1234-1234-1234-123456789abc", true, sb =>
{
    // Command characteristic (write-only)
    sb.AddCharacteristic("12345678-1234-1234-1234-123456789ab1", cb =>
    {
        cb.SetWrite(request =>
        {
            ProcessCommand(request.Data);
            if (request.IsReplyNeeded)
                request.Respond(GattState.Success);
            return Task.CompletedTask;
        }, WriteOptions.WriteWithoutResponse);
    });

    // Data characteristic (notify-only)
    var dataChar = sb.AddCharacteristic("12345678-1234-1234-1234-123456789ab2", cb =>
    {
        cb.SetNotification(options: NotificationOptions.Indicate);
    });
});
```

### Full MAUI Setup

```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        });

    builder.Services.AddBluetoothLeHosting();
    builder.Services.AddSingleton<HeartRateService>();   // your own class that calls AddService(...)

    return builder.Build();
}
```

## Troubleshooting

### RequestAccess returns NotSupported
- BLE hosting requires a physical device; simulators may not support it
- Ensure the device hardware supports BLE peripheral mode

### RequestAccess returns NotSetup
- **iOS**: Add `NSBluetoothAlwaysUsageDescription` and `NSBluetoothPeripheralUsageDescription` to Info.plist
- **Android**: Add `android.permission.BLUETOOTH_ADVERTISE`, `android.permission.BLUETOOTH_CONNECT` to AndroidManifest.xml (Android 12+)

### RequestAccess returns Disabled
- Bluetooth is turned off on the device; prompt the user to enable it

### RequestAccess returns Denied
- The user denied the Bluetooth permission; guide them to Settings to re-enable

### Advertising does not start
- Call `RequestAccess()` first and verify it returns `AccessState.Available`
- Check `IsAdvertising` to see if already advertising
- On Android, advertising payload size is limited; reduce local name length or number of service UUIDs

### Write handler not called
- Ensure `SetWrite` is called on the characteristic builder
- Check the `WriteOptions` match the central's write type (write with response vs write without response)

### Notifications not received by central
- Ensure `SetNotification` is called on the characteristic builder
- The central must subscribe to the characteristic first
- Check `SubscribedCentrals` count before sending

### Request/response reply never arrives
- The writing central must be subscribed to the characteristic before it writes - a GATT write
  response cannot carry a payload, so the reply travels as a notification. Check
  `request.Characteristic.SubscribedCentrals` before notifying
- Respond to the write (`request.Respond(...)` when `IsReplyNeeded`) before doing slow work, or the
  central's write times out

### Framed reply or request never completes
- The central must use `WriteCharacteristicMessageAsync` / `NotifyCharacteristicMessages` - a plain
  `WriteCharacteristicAsync` payload is not a valid fragment, and a plain `NotifyCharacteristic` subscriber
  sees raw fragments with header bytes
- `BleMessageReassembler.Push` returning `Malformed`, `OutOfSequence` or `TooLarge` means the message was
  discarded (the reassembler has already reset). Pass a larger `maxMessageBytes` to the constructor if the
  request is legitimately bigger than 64 KB
- A reassembler shared between centrals interleaves their fragments - keep one per `IPeripheral.Uuid`

### Ticketed L2CAP channel refused
- `UnknownTicket` - the token expired (`lifetime` passed) or was released; reserve a new one
- `AlreadyClaimed` - a second channel presented a token that is single-use
- The central's `secure` flag must match `L2CapTicketBrokerOptions.Secure` (default `true`), or the connect
  itself fails before any handshake
