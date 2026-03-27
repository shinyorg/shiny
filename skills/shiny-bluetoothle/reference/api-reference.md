# Shiny BluetoothLE Client API Reference

## Installation

```xml
<PackageReference Include="Shiny.BluetoothLE" Version="4.*" />
```

## Namespaces

```csharp
using Shiny;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Managed;
```

## Registration

```csharp
// In MauiProgram.cs or host builder
services.AddBluetoothLE();

// With delegate for background events
services.AddBluetoothLE<MyBleDelegate>();

// iOS/macOS with Apple configuration
services.AddBluetoothLE<MyBleDelegate>(new AppleBleConfiguration(
    ShowPowerAlert: true,
    RestoreIdentifier: "my-ble-app"
));
```

---

## Enums

### AccessState (namespace: Shiny)

```csharp
public enum AccessState
{
    Unknown,         // Permission not yet checked or unknown OS state
    NotSupported,    // API not supported on current device or platform
    NotSetup,        // Missing manifest/plist/appx configuration
    Disabled,        // Service disabled by user
    Restricted,      // Permission granted in limited fashion (e.g. foreground only)
    Denied,          // User denied permission
    Available        // All necessary permissions granted
}
```

### ConnectionState

```csharp
public enum ConnectionState
{
    Disconnected,
    Disconnecting,
    Connected,
    Connecting
}
```

### CharacteristicProperties (Flags enum)

```csharp
[Flags]
public enum CharacteristicProperties
{
    Broadcast                  = 1,
    Read                       = 2,
    WriteWithoutResponse       = 4,
    Write                      = 8,
    Notify                     = 16,
    Indicate                   = 32,
    AuthenticatedSignedWrites  = 64,
    ExtendedProperties         = 128,
    NotifyEncryptionRequired   = 256,
    IndicateEncryptionRequired = 512
}
```

### BleCharacteristicEvent

```csharp
public enum BleCharacteristicEvent
{
    Read,
    Write,
    WriteWithoutResponse,
    Notification
}
```

### PairingState

```csharp
public enum PairingState
{
    NotPaired,
    Paired
}
```

### TransactionState

```csharp
public enum TransactionState
{
    Active,
    Committing,
    Committed,
    Aborted
}
```

### ManagedScanListAction

```csharp
public enum ManagedScanListAction
{
    Add,
    Update,
    Remove,
    Clear
}
```

---

## Records / Models

### ScanConfig

```csharp
// Filters scan to peripherals that advertise specified service UUIDs
// iOS: you must set this to initiate a background scan
public record ScanConfig(params string[] ServiceUuids);
```

### AndroidScanConfig (Android only, extends ScanConfig)

```csharp
public record AndroidScanConfig(
    ScanMode ScanMode = ScanMode.Balanced,
    bool UseScanBatching = false,          // Enable scan batching if supported
    params string[] ServiceUuids
) : ScanConfig(ServiceUuids);
```

### ScanResult

```csharp
public record ScanResult(
    IPeripheral Peripheral,
    int Rssi,
    IAdvertisementData AdvertisementData
);
```

### ConnectionConfig

```csharp
// Android: false disables auto-reconnect but speeds up initial connection
// iOS: controls whether to reconnect automatically
public record ConnectionConfig(bool AutoConnect = true);
```

### AndroidConnectionConfig (Android only, extends ConnectionConfig)

```csharp
public record AndroidConnectionConfig(
    bool AutoConnect = true,
    GattConnectionPriority ConnectionPriority = GattConnectionPriority.Balanced
) : ConnectionConfig(AutoConnect);
```

### AppleBleConfiguration (iOS/macOS only)

```csharp
public record AppleBleConfiguration(
    bool ShowPowerAlert = false,           // Show alert when Bluetooth is powered off
    string? RestoreIdentifier = null,      // Restoration key for background restoration
    DispatchQueue? DispatchQueue = null     // Dispatch queue for CBCentralManager
);
```

### BleServiceInfo

```csharp
public record BleServiceInfo(string Uuid);
```

### BleCharacteristicInfo

```csharp
public record BleCharacteristicInfo(
    BleServiceInfo Service,
    string Uuid,
    bool IsNotifying,
    CharacteristicProperties Properties
);
```

### BleDescriptorInfo

```csharp
public record BleDescriptorInfo(
    BleCharacteristicInfo Characteristic,
    string Uuid
);
```

### BleCharacteristicResult

```csharp
public record BleCharacteristicResult(
    BleCharacteristicInfo Characteristic,
    BleCharacteristicEvent Event,
    byte[]? Data
);
```

### BleDescriptorResult

```csharp
public record BleDescriptorResult(
    BleDescriptorInfo Descriptor,
    byte[]? Data
);
```

### AdvertisementServiceData

```csharp
public record AdvertisementServiceData(
    string Uuid,    // The service UUID
    byte[] Data     // The service data bytes
);
```

### ManufacturerData

```csharp
public record ManufacturerData(
    ushort CompanyId,  // Bluetooth SIG assigned company identifier
    byte[] Data        // Manufacturer data bytes
);
```

### BleWriteSegment

```csharp
public record BleWriteSegment(
    byte[] Chunk,
    int Position,
    int TotalLength
);
```

### DeviceInfo

```csharp
public record DeviceInfo(
    string? SystemId = null,
    string? ManufacturerName = null,
    string? ModelNumber = null,
    string? SerialNumber = null,
    string? FirmwareRevision = null,
    string? HardwareRevision = null,
    string? SoftwareRevision = null
);
```

---

## Exceptions

### BleException

```csharp
// Base exception for BLE operations
public class BleException : Exception
{
    public BleException(string message);
    public BleException(string message, Exception exception);
}
```

### BleOperationException

```csharp
// Exception with GATT status code details
public class BleOperationException : BleException
{
    public BleOperationException(string message, int gattStatusCode);
    public int GattStatusCode { get; }
}
```

### GattReliableWriteTransactionException

```csharp
public class GattReliableWriteTransactionException : Exception
{
    public GattReliableWriteTransactionException(string msg);
}
```

---

## Interfaces

### IBleManager

```csharp
// Main entry point for BLE client operations. Inject via DI.
public interface IBleManager
{
    // Gets current access/permission state
    AccessState CurrentAccess { get; }

    // Requests necessary permissions for BLE usage
    IObservable<AccessState> RequestAccess();

    // Get a known peripheral from the last scan result by its UUID
    IPeripheral? GetKnownPeripheral(string peripheralUuid);

    // Gets whether a scan is currently active
    bool IsScanning { get; }

    // Stop any current scan
    void StopScan();

    // Gets peripherals currently connected by your app
    IEnumerable<IPeripheral> GetConnectedPeripherals();

    // Start scanning for BLE peripherals (only one scan at a time)
    IObservable<ScanResult> Scan(ScanConfig? scanConfig = null);
}
```

### IPeripheral

```csharp
// Represents a discovered BLE peripheral
public interface IPeripheral
{
    string Uuid { get; }
    string? Name { get; }
    int Mtu { get; }
    ConnectionState Status { get; }

    // Connect to the peripheral
    void Connect(ConnectionConfig? config);

    // Cancel connection - YOU MUST call this to clean up connections
    void CancelConnection();

    // Monitor connection status changes
    IObservable<ConnectionState> WhenStatusChanged();

    // Monitor connection failures
    IObservable<BleException> WhenConnectionFailed();

    // Fires when services on the peripheral change
    IObservable<Unit> WhenServicesChanged();

    // Read the peripheral RSSI
    IObservable<int> ReadRssi();

    // Get a known service (pulls from cache, discovers if not found)
    IObservable<BleServiceInfo> GetService(string serviceUuid);

    // Discover all services (forces fresh cache rebuild)
    IObservable<IReadOnlyList<BleServiceInfo>> GetServices();

    // Get a characteristic (throws BleException if not found)
    IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid);

    // Discover characteristics for a specific service
    IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid);

    // Subscribe to characteristic notifications (auto-reconnect if observable kept alive)
    IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true);

    // Watch for subscription state changes on a characteristic
    IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid);

    // Read a characteristic value
    IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid);

    // Write to a characteristic (with or without response)
    IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true);

    // Get a specific descriptor (throws if not found)
    IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);

    // Get all descriptors for a characteristic
    IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid);

    // Read a descriptor value
    IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);

    // Write a descriptor value
    IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data);
}
```

### IAdvertisementData

```csharp
// Data contained in a BLE advertisement packet
public interface IAdvertisementData
{
    string? LocalName { get; }                      // Local name broadcast by the peripheral
    bool? IsConnectable { get; }                    // Whether the peripheral is connectable
    AdvertisementServiceData[]? ServiceData { get; } // Service data entries
    ManufacturerData? ManufacturerData { get; }     // Manufacturer-specific data
    string[]? ServiceUuids { get; }                  // Advertised service UUIDs
    int? TxPower { get; }                            // Transmit power level in dBm
}
```

### IBleDelegate

```csharp
// Delegate for background BLE events
public interface IBleDelegate
{
    // Fires when the adapter state changes (foreground or background)
    Task OnAdapterStateChanged(AccessState state);

    // Fires when a device connects (foreground or background)
    Task OnPeripheralStateChanged(IPeripheral peripheral);
}

// Convenience base class with no-op implementations
public abstract class BleDelegate : IBleDelegate
{
    public virtual Task OnAdapterStateChanged(AccessState state) => Task.CompletedTask;
    public virtual Task OnPeripheralStateChanged(IPeripheral peripheral) => Task.CompletedTask;
}
```

### IManagedScan

```csharp
// Managed BLE scan maintaining an observable collection of discovered peripherals
public interface IManagedScan : IDisposable
{
    // Time after which peripherals are removed if not re-detected
    TimeSpan? ClearTime { get; }

    // Buffer time span for batching scan results
    TimeSpan BufferTimeSpan { get; }

    // Whether the scan is currently running
    bool IsScanning { get; }

    // Observable collection of discovered peripherals (bindable)
    INotifyReadOnlyCollection<ManagedScanResult> Peripherals { get; }

    // Current scan configuration
    ScanConfig? ScanConfig { get; }

    // Scheduler used for collection updates
    IScheduler? Scheduler { get; }

    // Gets all currently connected peripherals from scan results
    IEnumerable<IPeripheral> GetConnectedPeripherals();

    // Starts the managed BLE scan
    Task Start(
        ScanConfig? scanConfig = null,
        Func<ScanResult, bool>? predicate = null,
        IScheduler? scheduler = null,
        TimeSpan? bufferTime = null,
        TimeSpan? clearTime = null
    );

    // Stops the managed BLE scan
    void Stop();

    // Observable that emits scan list change events
    IObservable<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> WhenScan();
}
```

### ManagedScanResult

```csharp
// Result item in the managed scan observable collection
// Implements INotifyPropertyChanged and IAdvertisementData
public class ManagedScanResult : NotifyPropertyChanged, IAdvertisementData
{
    public IPeripheral Peripheral { get; }
    public IAdvertisementData? FullAdvertisementData { get; }
    public bool IsConnected { get; }               // Convenience: Peripheral.Status == Connected
    public string Uuid { get; }                     // Convenience: Peripheral.Uuid
    public bool? IsConnectable { get; }
    public string? Name { get; }
    public int Rssi { get; }
    public int? TxPower { get; }
    public ManufacturerData? ManufacturerData { get; }
    public DateTimeOffset LastSeen { get; }
    public string? LocalName { get; }
    public string[]? ServiceUuids { get; }
    public AdvertisementServiceData[]? ServiceData { get; }
}
```

---

## Feature Interfaces (Optional Capabilities)

### ICanRequestMtu

```csharp
// Cast check: peripheral is ICanRequestMtu
// Prefer using TryRequestMtu() extension instead
public interface ICanRequestMtu : IPeripheral
{
    // Negotiate a requested MTU with the peripheral
    IObservable<int> RequestMtu(int requestValue);
}
```

### ICanPairPeripherals

```csharp
// Cast check: peripheral is ICanPairPeripherals
// Prefer using TryPairingRequest() extension instead
public interface ICanPairPeripherals : IPeripheral
{
    // Send a pairing request
    IObservable<bool> PairingRequest(string? pin = null);

    // Current pairing status
    PairingState PairingStatus { get; }
}
```

### ICanViewPairedPeripherals

```csharp
// Cast check: bleManager is ICanViewPairedPeripherals
// Prefer using TryGetPairedPeripherals() extension instead
public interface ICanViewPairedPeripherals : IBleManager
{
    // Get list of paired peripherals
    IReadOnlyList<IPeripheral> GetPairedPeripherals();
}
```

### ICanDoTransactions

```csharp
// Cast check: peripheral is ICanDoTransactions
// Prefer using TryBeginTransaction() extension instead
public interface ICanDoTransactions : IPeripheral
{
    IGattReliableWriteTransaction BeginReliableWriteTransaction();
}
```

### IGattReliableWriteTransaction

```csharp
public interface IGattReliableWriteTransaction : IDisposable
{
    TransactionState Status { get; }
    IObservable<BleCharacteristicResult> Write(IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data);
    IObservable<Unit> Commit();
    void Abort();
}
```

---

## Extension Methods

### IBleManager Extensions (BleManagerExtensions)

```csharp
public static class BleManagerExtensions
{
    // Scan until a peripheral with a specific name is found
    static IObservable<IPeripheral> ScanUntilPeripheralFound(this IBleManager bleManager, string peripheralName);

    // Scan until first peripheral advertising a specific service UUID is found
    static IObservable<IPeripheral> ScanUntilFirstPeripheralFound(this IBleManager bleManager, string serviceUuid);

    // Scan for distinct peripherals only (no duplicate scan responses)
    static IObservable<IPeripheral> ScanForUniquePeripherals(this IBleManager bleManager, ScanConfig? config = null);
}
```

### IBleManager Extensions (ManagedExtensions)

```csharp
public static class ManagedExtensions
{
    // Create a managed scanner instance
    static IManagedScan CreateManagedScanner(this IBleManager bleManager);
}
```

### IPeripheral Extensions (PeripheralExtensions)

```csharp
public static class PeripheralExtensions
{
    // Check if peripheral is currently connected
    static bool IsConnected(this IPeripheral peripheral);

    // Connect if not already connected (completing observable)
    static IObservable<IPeripheral> WithConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null);

    // Observe connected state changes
    static IObservable<IPeripheral> WhenConnected(this IPeripheral peripheral);

    // Observe disconnected state changes
    static IObservable<IPeripheral> WhenDisconnected(this IPeripheral peripheral);

    // Overloads accepting BleCharacteristicInfo/BleDescriptorInfo instead of UUIDs:
    static IObservable<BleCharacteristicResult> NotifyCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, bool useIndicationsIfAvailable = true);
    static IObservable<BleCharacteristicResult> ReadCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info);
    static IObservable<BleCharacteristicResult> WriteCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, byte[] data, bool withResponse = true);
    static IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(this IPeripheral peripheral, BleCharacteristicInfo info);
    static IObservable<BleDescriptorResult> WriteDescriptor(this IPeripheral peripheral, BleDescriptorInfo info, byte[] data);
    static IObservable<BleDescriptorResult> ReadDescriptor(this IPeripheral peripheral, BleDescriptorInfo info);
}
```

### Async Extensions (AsyncExtensions)

```csharp
public static class AsyncExtensions
{
    // Request BLE access/permissions
    static Task<AccessState> RequestAccessAsync(this IBleManager manager);

    // Connect and wait for success (default 30s timeout)
    static Task ConnectAsync(this IPeripheral peripheral, ConnectionConfig? config = null, CancellationToken cancelToken = default, TimeSpan? timeout = null);

    // Disconnect and wait for success (default 30s timeout)
    static Task DisconnectAsync(this IPeripheral peripheral, CancellationToken cancelToken = default, TimeSpan? timeout = null);

    // Wait for a characteristic notification subscription to be established
    static Task WaitForCharacteristicSubscriptionAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancellationToken = default);

    // Get all services
    static Task<IReadOnlyList<BleServiceInfo>> GetServicesAsync(this IPeripheral peripheral, CancellationToken cancelToken = default);

    // Get a specific service
    static Task<BleServiceInfo> GetServiceAsync(this IPeripheral peripheral, string serviceUuid, CancellationToken cancelToken = default);

    // Get all characteristics across all services (default 10s timeout)
    static Task<IReadOnlyList<BleCharacteristicInfo>> GetAllCharacteristicsAsync(this IPeripheral peripheral, CancellationToken cancelToken = default, TimeSpan? timeout = null);

    // Get characteristics for a specific service (default 10s timeout)
    static Task<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristicsAsync(this IPeripheral peripheral, string serviceUuid, CancellationToken cancelToken = default, TimeSpan? timeout = null);

    // Get a specific characteristic
    static Task<BleCharacteristicInfo> GetCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default);

    // Get descriptors for a characteristic
    static Task<IReadOnlyList<BleDescriptorInfo>> GetDescriptorsAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default);

    // Write to a characteristic (default 3s timeout)
    static Task<BleCharacteristicResult> WriteCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true, CancellationToken cancelToken = default, int timeoutMs = 3000);

    // Read a characteristic (default 3s timeout)
    static Task<BleCharacteristicResult> ReadCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default, int timeoutMs = 3000);

    // Read a descriptor (default 3s timeout)
    static Task<BleDescriptorResult> ReadDescriptorAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, string descriptorUuid, CancellationToken cancelToken = default, int timeoutMs = 3000);

    // Write a descriptor (default 3s timeout)
    static Task<BleDescriptorResult> WriteDescriptorAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data, CancellationToken cancelToken = default, int timeoutMs = 3000);

    // Read device information service (default 3s timeout)
    static Task<DeviceInfo> ReadDeviceInformationAsync(this IPeripheral peripheral, CancellationToken cancelToken = default, int timeoutMs = 3000);

    // Read RSSI (default 3s timeout)
    static Task<int> ReadRssiAsync(this IPeripheral peripheral, CancellationToken cancelToken = default, int timeoutMs = 3000);

    // Overloads accepting BleCharacteristicInfo/BleDescriptorInfo:
    static Task<BleCharacteristicResult> ReadCharacteristicAsync(this IPeripheral peripheral, BleCharacteristicInfo info, CancellationToken cancelToken = default, int timeoutMs = 3000);
    static Task<BleCharacteristicResult> WriteCharacteristicAsync(this IPeripheral peripheral, BleCharacteristicInfo info, byte[] data, bool withResponse = true, CancellationToken cancelToken = default, int timeoutMs = 3000);
    static Task<IReadOnlyList<BleDescriptorInfo>> GetDescriptorsAsync(this IPeripheral peripheral, BleCharacteristicInfo info, CancellationToken cancelToken = default);
    static Task<BleDescriptorResult> WriteDescriptorAsync(this IPeripheral peripheral, BleDescriptorInfo info, byte[] data, CancellationToken cancelToken = default, int timeoutMs = 3000);
    static Task<BleDescriptorResult> ReadDescriptorAsync(this IPeripheral peripheral, BleDescriptorInfo info, CancellationToken cancelToken = default, int timeoutMs = 3000);
}
```

### Characteristic Extensions (CharacteristicExtensions)

```csharp
public static class CharacteristicExtensions
{
    // Discover all characteristics across all services on a peripheral
    static IObservable<IReadOnlyList<BleCharacteristicInfo>> GetAllCharacteristics(this IPeripheral peripheral);

    // Write a stream to a characteristic in MTU-sized chunks
    static IObservable<BleWriteSegment> WriteCharacteristicBlob(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, Stream stream, TimeSpan? packetSendTimeout = null);

    // Property check helpers on BleCharacteristicInfo:
    static bool CanRead(this BleCharacteristicInfo ch);
    static bool CanWriteWithResponse(this BleCharacteristicInfo ch);
    static bool CanWriteWithoutResponse(this BleCharacteristicInfo ch);
    static bool CanWrite(this BleCharacteristicInfo ch);
    static bool CanNotifyOrIndicate(this BleCharacteristicInfo ch);
    static bool CanNotify(this BleCharacteristicInfo ch);
    static bool CanIndicate(this BleCharacteristicInfo ch);

    // Assertion helpers (throw InvalidOperationException if capability missing):
    static void AssertWrite(this BleCharacteristicInfo characteristic, bool withResponse);
    static void AssertRead(this BleCharacteristicInfo characteristic);
    static void AssertNotify(this BleCharacteristicInfo characteristic);

    // Standard BLE service readers:
    static IObservable<DeviceInfo> ReadDeviceInformation(this IPeripheral peripheral);
    static IObservable<int> ReadBatteryInformation(this IPeripheral peripheral);
    static IObservable<int> HeartRateSensor(this IPeripheral peripheral);
}
```

### Feature Extensions (MTU)

```csharp
public static class FeatureMtu
{
    // Check if MTU requests are supported
    static bool CanRequestMtu(this IPeripheral peripheral);

    // Request MTU if supported, otherwise return current MTU
    static IObservable<int> TryRequestMtu(this IPeripheral peripheral, int requestedValue);

    // Async version (default 5s timeout)
    static Task<int> TryRequestMtuAsync(this IPeripheral peripheral, int requestedValue, int timeoutMillis = 5000, CancellationToken cancelToken = default);
}
```

### Feature Extensions (Pairing)

```csharp
public static class FeaturePairing
{
    // Check if viewing paired peripherals is supported on the manager
    static bool CanViewPairedPeripherals(this IBleManager centralManager);

    // Check if pairing requests are supported on the peripheral
    static bool IsPairingRequestsAvailable(this IPeripheral peripheral);

    // Get paired peripherals if supported, otherwise empty list
    static IReadOnlyList<IPeripheral> TryGetPairedPeripherals(this IBleManager centralManager);

    // Get pairing status if supported, otherwise null
    static PairingState? TryGetPairingStatus(this IPeripheral peripheral);

    // Request pairing if supported, otherwise null
    static IObservable<bool?> TryPairingRequest(this IPeripheral peripheral, string? pin = null);
}
```

### Feature Extensions (Reliable Write Transactions)

```csharp
public static class Feature_Transactions
{
    // Begin a reliable write transaction if supported, otherwise null
    static IGattReliableWriteTransaction? TryBeginTransaction(this IPeripheral peripheral);

    // Check if reliable transactions are available
    static bool IsReliableTransactionsAvailable(this IPeripheral peripheral);
}
```

---

## Standard UUIDs

```csharp
public static class StandardUuids
{
    public const string DeviceInformationServiceUuid = "180A";
    public static (string ServiceUuid, string CharacteristicUuid) HeartRateMeasurementSensor => ("180D", "2A37");
    public static (string ServiceUuid, string CharacteristicUuid) BatteryService => ("180F", "2A19");
}
```

---

## Usage Examples

### Basic Scanning

```csharp
// Inject IBleManager via constructor
public class MyViewModel(IBleManager bleManager)
{
    IDisposable? scanSub;

    public void StartScan()
    {
        scanSub = bleManager
            .Scan(new ScanConfig("180D")) // Filter by heart rate service
            .Subscribe(result =>
            {
                var name = result.Peripheral.Name ?? result.AdvertisementData.LocalName;
                var rssi = result.Rssi;
                Console.WriteLine($"Found: {name} RSSI: {rssi}");
            });
    }

    public void StopScan()
    {
        scanSub?.Dispose();
        // OR: bleManager.StopScan();
    }
}
```

### Managed Scan (MVVM-Friendly)

```csharp
public class ScanViewModel : IDisposable
{
    readonly IManagedScan scanner;

    public ScanViewModel(IBleManager bleManager)
    {
        scanner = bleManager.CreateManagedScanner();
    }

    // Bind to this in XAML/MAUI - it is INotifyCollectionChanged
    public INotifyReadOnlyCollection<ManagedScanResult> Peripherals => scanner.Peripherals;

    public async Task StartScan()
    {
        await scanner.Start(
            scanConfig: new ScanConfig("180D"),
            bufferTime: TimeSpan.FromSeconds(2),
            clearTime: TimeSpan.FromSeconds(10) // Remove stale peripherals after 10s
        );
    }

    public void StopScan() => scanner.Stop();
    public void Dispose() => scanner.Dispose();
}
```

### Connecting and Reading a Characteristic

```csharp
public async Task ReadHeartRate(IPeripheral peripheral)
{
    // Connect with default 30s timeout
    await peripheral.ConnectAsync();

    try
    {
        // Read a characteristic
        var result = await peripheral.ReadCharacteristicAsync("180D", "2A37");
        var heartRate = result.Data![0];
        Console.WriteLine($"Heart rate: {heartRate}");
    }
    finally
    {
        // Always disconnect when done
        await peripheral.DisconnectAsync();
    }
}
```

### Subscribing to Notifications

```csharp
public IDisposable SubscribeToHeartRate(IPeripheral peripheral)
{
    return peripheral
        .NotifyCharacteristic("180D", "2A37")
        .Subscribe(result =>
        {
            if (result.Data != null)
            {
                var heartRate = result.Data[0];
                Console.WriteLine($"Heart rate: {heartRate}");
            }
        });
}
```

### Writing to a Characteristic

```csharp
public async Task WriteValue(IPeripheral peripheral)
{
    var data = new byte[] { 0x01, 0x02, 0x03 };

    // Write with response (default)
    await peripheral.WriteCharacteristicAsync("service-uuid", "char-uuid", data);

    // Write without response
    await peripheral.WriteCharacteristicAsync("service-uuid", "char-uuid", data, withResponse: false);
}
```

### Writing a Large Blob

```csharp
public void WriteLargeData(IPeripheral peripheral, Stream dataStream)
{
    peripheral
        .WriteCharacteristicBlob("service-uuid", "char-uuid", dataStream)
        .Subscribe(segment =>
        {
            var progress = (double)segment.Position / segment.TotalLength * 100;
            Console.WriteLine($"Progress: {progress:F1}%");
        });
}
```

### Feature Detection and MTU Request

```csharp
public async Task NegotiateMtu(IPeripheral peripheral)
{
    // Request larger MTU (works on Android, returns current MTU on other platforms)
    var mtu = await peripheral.TryRequestMtuAsync(512);
    Console.WriteLine($"Negotiated MTU: {mtu}");
}
```

### Pairing

```csharp
public async Task PairDevice(IPeripheral peripheral)
{
    if (peripheral.IsPairingRequestsAvailable())
    {
        var pairingStatus = peripheral.TryGetPairingStatus();
        if (pairingStatus == PairingState.NotPaired)
        {
            var paired = await peripheral
                .TryPairingRequest()
                .ToTask();

            Console.WriteLine(paired == true ? "Paired" : "Pairing failed or not supported");
        }
    }
}
```

### Reliable Write Transaction

```csharp
public void ReliableWrite(IPeripheral peripheral)
{
    var transaction = peripheral.TryBeginTransaction();
    if (transaction != null)
    {
        using (transaction)
        {
            transaction
                .Write(peripheral, "service-uuid", "char-uuid-1", new byte[] { 0x01 })
                .SelectMany(_ => transaction.Write(peripheral, "service-uuid", "char-uuid-2", new byte[] { 0x02 }))
                .SelectMany(_ => transaction.Commit())
                .Subscribe(
                    _ => Console.WriteLine("Transaction committed"),
                    ex =>
                    {
                        transaction.Abort();
                        Console.WriteLine($"Transaction failed: {ex.Message}");
                    }
                );
        }
    }
}
```

### Reading Device Information

```csharp
public async Task ReadDeviceInfo(IPeripheral peripheral)
{
    var info = await peripheral.ReadDeviceInformationAsync();
    Console.WriteLine($"Manufacturer: {info.ManufacturerName}");
    Console.WriteLine($"Model: {info.ModelNumber}");
    Console.WriteLine($"Firmware: {info.FirmwareRevision}");
}
```

### Scanning for a Specific Peripheral

```csharp
// Find a peripheral by name
var peripheral = await bleManager
    .ScanUntilPeripheralFound("MyDevice")
    .ToTask();

// Find first peripheral with a specific service
var peripheral2 = await bleManager
    .ScanUntilFirstPeripheralFound("180D")
    .ToTask();

// Scan for unique peripherals only
bleManager
    .ScanForUniquePeripherals()
    .Subscribe(p => Console.WriteLine($"New peripheral: {p.Name}"));
```

---

## Troubleshooting

### Common Issues

1. **Scan returns no results**
   - Verify `RequestAccess()` returns `AccessState.Available`.
   - On Android, ensure location permissions and Bluetooth permissions are granted.
   - On iOS, ensure `NSBluetoothAlwaysUsageDescription` is set in `Info.plist`.
   - Check that the BLE adapter is enabled on the device.

2. **Connection times out**
   - The default timeout for `ConnectAsync` is 30 seconds. Increase via the `timeout` parameter.
   - Set `ConnectionConfig.AutoConnect = false` for faster initial connections (disables auto-reconnect).
   - On Android, use `AndroidConnectionConfig` to set `GattConnectionPriority.High`.

3. **GATT operations fail with BleOperationException**
   - Check the `GattStatusCode` for platform-specific error details.
   - Ensure the peripheral is still connected before performing operations.
   - Some characteristics require bonding/pairing first.

4. **Notifications stop after disconnection**
   - `NotifyCharacteristic()` will attempt to auto-reconnect if the observable is kept alive.
   - Make sure not to dispose the subscription if you want reconnection behavior.

5. **Only one scan at a time**
   - `IBleManager.Scan()` only allows one active scan. Dispose the previous scan or call `StopScan()` before starting a new one.
   - Check `IsScanning` before starting a scan.

6. **iOS background scanning**
   - You MUST provide `ServiceUuids` in `ScanConfig` for background scanning on iOS.
   - Set a `RestoreIdentifier` in `AppleBleConfiguration` for state restoration.
   - Register an `IBleDelegate` for background event handling.

7. **MTU negotiation returns same value**
   - MTU requests are only supported on Android (`ICanRequestMtu`). On iOS/Windows, the OS handles MTU negotiation automatically.
   - Use `CanRequestMtu()` to check support, or `TryRequestMtu()` which gracefully falls back.
