# API Reference

## Installation

```bash
dotnet add package Shiny.BluetoothLE.Hosting
```

## Namespaces

```csharp
using Shiny;                                // AccessState, ServiceCollectionExtensions
using Shiny.BluetoothLE;                    // CharacteristicProperties
using Shiny.BluetoothLE.Hosting;            // All hosting interfaces, enums, records
using Shiny.BluetoothLE.Hosting.Managed;    // BleGattCharacteristic, BleGattCharacteristicAttribute
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

    // Gets whether DI-registered GATT services are currently attached
    bool IsRegisteredServicesAttached { get; }

    // Attaches all GATT services registered via dependency injection
    Task AttachRegisteredServices();

    // Detaches all DI-registered GATT services
    void DetachRegisteredServices();

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

Represents an L2CAP channel connection.

```csharp
public record L2CapChannel(
    ushort Psm,
    string Identifier,
    Func<byte[], IObservable<Unit>> Write,
    IObservable<byte[]> DataReceived
);
```

### L2CapInstance

Disposable wrapper for an L2CAP listener.

```csharp
public struct L2CapInstance : IDisposable
{
    public L2CapInstance(ushort psm, Action onDispose);
    public ushort Psm { get; }
    public void Dispose();
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

## Managed Pattern Types

### BleGattCharacteristic (abstract class)

Base class for managed GATT characteristics registered via DI.

```csharp
// Namespace: Shiny.BluetoothLE.Hosting.Managed
public abstract class BleGattCharacteristic
{
    // The underlying IGattCharacteristic (set internally after service is built)
    public IGattCharacteristic Characteristic { get; }

    // Number of centrals currently subscribed to notifications
    protected int SubscriptionCount { get; }

    // Called when the managed service is attached
    public virtual Task OnStart();

    // Called when the managed service is detached
    public virtual void OnStop();

    // Write + notify response pattern: process write, return data to send as notification
    // Cannot be overridden together with OnWrite
    public virtual Task<GattResult> Request(WriteRequest request);

    // Handle a write request from a central
    // Cannot be overridden together with Request
    public virtual Task OnWrite(WriteRequest request);

    // Handle a read request from a central
    public virtual Task<GattResult> OnRead(ReadRequest request);

    // Called when a central subscribes or unsubscribes from notifications
    public virtual Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed);
}
```

### BleGattCharacteristicAttribute

Attribute to mark a `BleGattCharacteristic` subclass with its service and characteristic UUIDs.

```csharp
// Namespace: Shiny.BluetoothLE.Hosting.Managed
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class BleGattCharacteristicAttribute : Attribute
{
    public BleGattCharacteristicAttribute(string serviceUuid, string characteristicUuid);

    public string ServiceUuid { get; }
    public string CharacteristicUuid { get; }

    // Whether the read requires encryption (default: false)
    public bool IsReadSecure { get; set; }

    // Notification type (default: NotificationOptions.Notify)
    public NotificationOptions Notifications { get; set; }

    // Write type (default: WriteOptions.WriteWithoutResponse)
    public WriteOptions Write { get; set; }
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

    // Registers a managed BleGattCharacteristic in the DI container
    // Also calls AddBluetoothLeHosting automatically
    public static void AddBleHostedCharacteristic<TService>(this IServiceCollection services)
        where TService : BleGattCharacteristic;
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

### Managed Characteristic with DI

```csharp
// MyReadWriteCharacteristic.cs
using Shiny.BluetoothLE.Hosting;
using Shiny.BluetoothLE.Hosting.Managed;

[BleGattCharacteristic(
    "12345678-1234-1234-1234-123456789abc",
    "12345678-1234-1234-1234-123456789ab1",
    Write = WriteOptions.Write,
    Notifications = NotificationOptions.Notify
)]
public class MyReadWriteCharacteristic : BleGattCharacteristic
{
    string currentValue = "Initial";

    public override Task OnStart()
    {
        Console.WriteLine("Characteristic started");
        return Task.CompletedTask;
    }

    public override void OnStop()
    {
        Console.WriteLine("Characteristic stopped");
    }

    public override Task<GattResult> OnRead(ReadRequest request)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(currentValue);
        return Task.FromResult(GattResult.Success(data));
    }

    public override async Task OnWrite(WriteRequest request)
    {
        currentValue = System.Text.Encoding.UTF8.GetString(request.Data);
        if (request.IsReplyNeeded)
            request.Respond(GattState.Success);

        // Notify all subscribers of the new value
        if (SubscriptionCount > 0)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(currentValue);
            await Characteristic.Notify(data);
        }
    }

    public override Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed)
    {
        Console.WriteLine($"Peripheral {peripheral.Uuid} {(subscribed ? "subscribed" : "unsubscribed")} (MTU: {peripheral.Mtu})");
        return Task.CompletedTask;
    }
}
```

Register in MauiProgram.cs:
```csharp
builder.Services.AddBleHostedCharacteristic<MyReadWriteCharacteristic>();
```

Start the managed server:
```csharp
await hostingManager.AttachRegisteredServices();
await hostingManager.StartAdvertising(new AdvertisementOptions("MyDevice"));
```

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

### Managed Request Pattern (Write + Auto Notify)

```csharp
[BleGattCharacteristic(
    "12345678-1234-1234-1234-123456789abc",
    "12345678-1234-1234-1234-123456789ab1",
    Write = WriteOptions.Write,
    Notifications = NotificationOptions.Notify
)]
public class EchoCharacteristic : BleGattCharacteristic
{
    // Central must be subscribed to notifications before writing.
    // The returned GattResult data is automatically sent back as a notification.
    public override Task<GattResult> Request(WriteRequest request)
    {
        var input = System.Text.Encoding.UTF8.GetString(request.Data);
        var output = System.Text.Encoding.UTF8.GetBytes($"Echo: {input}");
        return Task.FromResult(GattResult.Success(output));
    }
}
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
    builder.Services.AddBleHostedCharacteristic<MyReadWriteCharacteristic>();
    builder.Services.AddBleHostedCharacteristic<EchoCharacteristic>();

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

### Managed services not starting
- Ensure `[BleGattCharacteristic]` attribute is applied to the class
- Ensure `AddBleHostedCharacteristic<T>()` is called in DI registration
- Call `AttachRegisteredServices()` at runtime to wire up the services
- Service and characteristic UUIDs must be valid

### Request pattern throws InvalidOperationException
- The central must be subscribed to notifications before writing to a `Request`-pattern characteristic
- `Request` and `OnWrite` cannot both be overridden on the same characteristic

### GattResult.Error returned from read
- Check the `GattState` value for the specific error reason
- Common causes: `ReadNotPermitted`, `InsufficientAuthentication`, `InsufficientEncryption`
