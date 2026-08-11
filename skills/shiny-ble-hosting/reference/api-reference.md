# API Reference

## Installation

```bash
dotnet add package Shiny.BluetoothLE.Hosting
```

## Namespaces

```csharp
using Shiny;                                // AccessState, ServiceCollectionExtensions
using Shiny.BluetoothLE;                    // CharacteristicProperties
using Shiny.BluetoothLE.Hosting;            // All hosting interfaces, enums, records, and the
                                            // [BleService] source generator attributes
```

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

    // The current MTU
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

## Source Generator Attributes

Shipped inside `Shiny.BluetoothLE.Hosting` (namespace `Shiny.BluetoothLE.Hosting`). Applied to a
`partial class`, they make the generator emit the `AddService(...)` / `OpenL2Cap(...)` plumbing.
Nothing reflective is emitted, so it stays AOT-safe.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class BleServiceAttribute(string uuid) : Attribute
{
    public string Uuid { get; }
    public bool Primary { get; set; } = true;   // default true
    public bool Advertise { get; set; }         // collected into StartBleHostedAdvertising
    public string? Name { get; set; }           // names the generated Add{Name} extension
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class ReadCharacteristicAttribute(string uuid) : Attribute
{
    public string Uuid { get; }
    public bool Encrypted { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class WriteCharacteristicAttribute(string uuid) : Attribute
{
    public string Uuid { get; }
    public bool WriteWithoutResponse { get; set; }
    public bool AuthenticatedSignedWrites { get; set; }
    public bool EncryptionRequired { get; set; }
    public bool ManualRespond { get; set; }     // suppress the generated Respond call
}

// also valid on the class (AllowMultiple) for the push API without a subscription hook,
// where Name is required
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class NotifyCharacteristicAttribute(string uuid) : Attribute
{
    public string Uuid { get; }
    public bool Indicate { get; set; }
    public bool EncryptionRequired { get; set; }
    public string? Name { get; set; }
}

// write + notify: the handler result is pushed back to the writing central as a notification
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequestResponseCharacteristicAttribute(string uuid) : Attribute
{
    public string Uuid { get; }
    public bool Indicate { get; set; }
    public bool EncryptionRequired { get; set; }
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class L2CapServiceAttribute : Attribute
{
    public bool Secure { get; set; }
    public string? PsmService { get; set; }        // GATT service UUID to publish the PSM on
    public string? PsmCharacteristic { get; set; } // read characteristic serving the PSM
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class OnChannelOpenedAttribute : Attribute;
```

### Generator runtime types

```csharp
// base of every generated {ServiceClass}Context - one per connected central, held for as long as
// Shiny caches that IPeripheral. The derived type is partial: add your own properties to it.
public abstract class BleServiceContext
{
    public IPeripheral Peripheral { get; }
    public string ConnectionId { get; }              // Peripheral.Uuid
    public int Mtu { get; }
    public string ServiceUuid { get; }               // always available
    public IGattService? Service { get; }            // null until AddService returns
    public IDictionary<string, object?> Items { get; }
}

// passed to notify hooks; CharacteristicSubscription also binds
public record BleSubscription(IGattCharacteristic Characteristic, IPeripheral Peripheral, bool IsSubscribing);

// passed to [OnChannelOpened] handlers
public sealed class BleL2CapContext
{
    public L2CapChannel Channel { get; }
    public ushort Psm { get; }
    public string PeerIdentifier { get; }
    public IDictionary<string, object?> Items { get; }
}

// returned by AttachBleHostedServices - dispose to cancel handlers, close listeners, remove services
public sealed class BleHostedServiceSession : IAsyncDisposable
{
    public IReadOnlyList<IGattService> Services { get; }
    public IReadOnlyList<ushort> Psms { get; }
}

// IAsyncEnumerable convenience over L2CapChannel.DataReceived
public static IAsyncEnumerable<byte[]> ReadAll(this L2CapChannel channel, CancellationToken ct = default);
```

### Generated members

On each `[BleService]` partial class:

```csharp
public const string BleServiceUuid;
public CancellationToken BleHostToken { get; }   // cancelled on teardown
public IGattService? BleService { get; }

// per notify-capable characteristic named "Ticker"
public Task NotifyTicker(byte[] data, params IPeripheral[] centrals);
public IReadOnlyList<IPeripheral> TickerSubscribers { get; }
public bool HasTickerSubscribers { get; }

// opt-in hooks - implement in your half of the class, or the compiler drops the call
partial void OnBleHandlerError(string characteristicUuid, Exception exception);
partial void OnBleResponseDropped(string characteristicUuid, IPeripheral peripheral, byte[] data);
```

On each `[L2CapService]` partial class:

```csharp
public ushort Psm { get; }        // zero while closed
public bool IsListening { get; }
partial void OnL2CapChannelError(L2CapChannel channel, Exception exception);
```

Assembly level, in the project's `$(RootNamespace)`:

```csharp
public static IServiceCollection AddBleHostedServices(this IServiceCollection services);
public static Task<BleHostedServiceSession> AttachBleHostedServices(this IBleHostingManager m, IServiceProvider sp);
public static Task StartBleHostedAdvertising(this IBleHostingManager m, string? localName = null);
public static Task<IGattService> Add{Name}(this IBleHostingManager m, ...);       // per merge group
public static Task<L2CapInstance> Add{Name}(this IBleHostingManager m, ...);      // per L2CAP listener
```

The DI pieces are only emitted when the compilation references
`Microsoft.Extensions.DependencyInjection.Abstractions`.

### Handler signature binding

Parameters bind by type, in any order, any subset - none are required. Every return may be wrapped
in `Task<>` or `ValueTask<>`.

| Kind | Bindable parameters | Allowed returns |
|---|---|---|
| Read | `ReadRequest`, `{Service}Context`, `IPeripheral`, `IGattCharacteristic`, `int` (offset), `CancellationToken` | `byte[]`, `GattResult` |
| Write | `byte[]` (data), `WriteRequest`, `{Service}Context`, `IPeripheral`, `IGattCharacteristic`, `int` (offset), `bool` (IsReplyNeeded), `CancellationToken` | `void`, `GattState` |
| RequestResponse | same as Write | `byte[]`, `GattResult` |
| Notify hook | `BleSubscription`, `CharacteristicSubscription`, `{Service}Context`, `IPeripheral`, `IGattCharacteristic`, `bool` (IsSubscribing), `CancellationToken` | `void` |
| `[OnChannelOpened]` | `L2CapChannel`, `BleL2CapContext`, `CancellationToken` | `void` |

- Read returning `byte[]` is wrapped in `GattResult.Success`; returning `GattResult` passes through.
  An unhandled exception becomes `GattResult.Error(GattState.Failure)`.
- Write returning nothing responds `GattState.Success` (or `Failure` on a throw), **only when
  `WriteRequest.IsReplyNeeded`**. Returning `GattState` responds that value. Set `ManualRespond` and
  take a `WriteRequest` to answer the central yourself.
- Every UUID is normalized to the full 128-bit form. Short forms work on Apple (`CBUUID.FromString`)
  but throw on Android (`java.util.UUID.fromString`), so never hand a short UUID to `AddService`
  directly - only the generator normalizes for you.

### Diagnostics

`SBH001` non-partial/nested/generic/static type · `SBH002` invalid UUID · `SBH003` handler outside a
`[BleService]` · `SBH004` two handlers of the same kind on one characteristic · `SBH005`
request/response colliding with write or notify · `SBH006` unsupported signature · `SBH007` static or
generic handler · `SBH008` invalid/dangling PSM publication · `SBH009` wrong `[OnChannelOpened]`
count · `SBH010` one characteristic declared by two merged classes · `SBH011` (warning) merged
services disagree on `Primary` · `SBH012` (warning) option combination not expressible ·
`SBH013` `ManualRespond` misuse · `SBH014` class-level `[NotifyCharacteristic]` without a `Name`.

Read + notify on one UUID is legal - `SBH004` is per handler *kind*, not per UUID.

`SBH012` exists because `WriteOptions` and `NotificationOptions` are `[Flags]` enums declared without
explicit values (`Write = 0`, `Notify = 0`), so only one member can be selected. The generator picks
the security flag when a combination is requested.

## Extension Methods

### ServiceCollectionExtensions

DI registration methods in the `Shiny` namespace.

```csharp
public static class ServiceCollectionExtensions
{
    // Registers IBleHostingManager in the DI container
    public static IServiceCollection AddBluetoothLeHosting(this IServiceCollection services);
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

### Source-Generated Service (preferred for anything non-trivial)

```csharp
// HeartRateService.cs
using Shiny.BluetoothLE.Hosting;

[BleService("180D", Advertise = true, Name = "HeartRate")]
public partial class HeartRateService(IHeartRateSensor sensor)
{
    [ReadCharacteristic("2A37")]
    Task<byte[]> ReadMeasurement(HeartRateServiceContext context)
        => Task.FromResult(new byte[] { 0x00, sensor.Read(context.User) });

    // hook is optional - NotifyMeasurement/MeasurementSubscribers are generated either way
    [NotifyCharacteristic("2A37", Name = "Measurement", Indicate = true)]
    Task OnMeasurementSubscription(BleSubscription subscription, HeartRateServiceContext context)
        => Task.CompletedTask;

    // the returned status is what gets responded, and only when the central asked for a reply
    [WriteCharacteristic("2A39")]
    Task<GattState> ControlPoint(byte[] data, int offset, HeartRateServiceContext context)
        => Task.FromResult(offset == 0 ? GattState.Success : GattState.InvalidOffset);

    // write + notify: the result comes back to the writing central as a notification
    [RequestResponseCharacteristic("2A3B", Name = "Command")]
    Task<byte[]> Exchange(byte[] request, CancellationToken cancellationToken) => Handle(request);

    partial void OnBleHandlerError(string characteristicUuid, Exception ex)
        => Console.WriteLine($"{characteristicUuid}: {ex.Message}");
}

// your half of the generated context - stamp anything onto the connected central
public partial class HeartRateServiceContext
{
    public AuthUser? User { get; set; }
}
```

Register in MauiProgram.cs:
```csharp
builder.Services.AddBluetoothLeHosting();
builder.Services.AddBleHostedServices();   // generated
```

Start it:
```csharp
await using var session = await hostingManager.AttachBleHostedServices(serviceProvider);
await hostingManager.StartBleHostedAdvertising("MyDevice");
```

Several classes may declare the same service UUID - the generator merges them into one
`AddService` call, which matters because `BleHostingManager` keys its services by UUID and would
throw on a second registration. Declaring the same characteristic UUID in two merged classes is
`SBH010`.

### Source-Generated L2CAP Listener

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

`PsmService` + `PsmCharacteristic` publish the assigned PSM as a read characteristic (two
little-endian bytes) on a `[BleService]` in the same compilation - a central has no other in-band
way to learn it. Listeners open before `AddService`, so an immediate read returns a live value.
The channel is disposed once the handler returns.

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
// Battery service
await hostingManager.AddService("180F", true, sb =>
{
    sb.AddCharacteristic("2A19", cb =>
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

    // Register managed characteristics
    builder.Services.AddBluetoothLeHosting();
    builder.Services.AddBleHostedServices();   // generated - every [BleService]/[L2CapService]

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

### Generated services not starting
- Ensure the class is a top level, non-generic `partial class` carrying `[BleService]` (SBH001)
- Ensure `AddBleHostedServices()` is called in DI registration
- Call `AttachBleHostedServices(serviceProvider)` at runtime, and keep the returned session alive -
  disposing it removes the services and closes the L2CAP listeners
- A `PackageReference` to `Shiny.BluetoothLE.Hosting` carries the generator; a `ProjectReference`
  does not, so in-repo consumers must reference the generator project with `OutputItemType="Analyzer"`

### Request/response reply never arrives
- The writing central must be subscribed to the characteristic before it writes - a GATT write
  response cannot carry a payload, so the reply travels as a notification. Implement
  `OnBleResponseDropped` to observe the case where it was not subscribed
