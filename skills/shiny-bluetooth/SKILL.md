---
name: shiny-bluetooth
description: Generate code using Shiny.Bluetooth for classic Bluetooth (BR/EDR) - RFCOMM / Serial Port Profile client connections and servers over a System.IO.Stream, inquiry discovery, paired device listing, pairing and discoverability on Android, Windows, macOS (AppKit) and Linux (BlueZ). Not for Bluetooth Low Energy and not available on iOS / Mac Catalyst.
auto_invoke: true
triggers:
  - classic bluetooth
  - bluetooth classic
  - BR/EDR
  - BREDR
  - RFCOMM
  - SPP
  - serial port profile
  - bluetooth serial
  - bluetooth SPP
  - bluetooth socket
  - BluetoothSocket
  - BluetoothServerSocket
  - createRfcommSocketToServiceRecord
  - listenUsingRfcommWithServiceRecord
  - RfcommDeviceService
  - RfcommServiceProvider
  - StreamSocketListener
  - IOBluetooth
  - IOBluetoothDevice
  - IOBluetoothRFCOMMChannel
  - ProfileManager1
  - RegisterProfile
  - Profile1
  - ConnectProfile
  - SDP
  - service discovery protocol
  - SDP record
  - bluetooth inquiry
  - discover bluetooth devices
  - bluetooth device discovery
  - startDiscovery
  - paired devices
  - bonded devices
  - getBondedDevices
  - pair bluetooth device
  - createBond
  - bluetooth pairing
  - make device discoverable
  - ACTION_REQUEST_DISCOVERABLE
  - discoverable
  - class of device
  - bluetooth printer
  - bluetooth barcode scanner
  - OBD bluetooth classic
  - HC-05
  - HC-06
  - ELM327 bluetooth
  - IBluetoothManager
  - IBluetoothDevice
  - BluetoothDiscoveryResult
  - GetDevice
  - BluetoothConnection
  - BluetoothListener
  - BluetoothCapabilities
  - BluetoothServiceUuids
  - BluetoothMajorDeviceClass
  - BluetoothException
  - BluetoothNotSupportedException
  - BluetoothPermissionException
  - BluetoothConnectionException
  - AddBluetooth
  - AddLinuxBluetooth
  - Shiny.Bluetooth
  - Shiny.Bluetooth.Linux
---

# Shiny.Bluetooth Skill

You are an expert in Shiny.Bluetooth, the **classic Bluetooth (BR/EDR)** library: RFCOMM client and
server connections exposed as a plain `System.IO.Stream`, inquiry discovery, the paired device list,
pairing, and making the device discoverable.

## When to Use This Skill

Invoke this skill when the user wants to:
- Talk to a Bluetooth serial device - SPP adapters (HC-05/HC-06), receipt/label printers, barcode
  scanners, ELM327 OBD dongles, industrial or medical devices that predate BLE
- Open an RFCOMM connection to a service UUID, or host one for other devices to connect to
- Find nearby classic devices, list paired devices, or pair with one from code
- Make the device discoverable

## ⚠️ Read this before writing anything

1. **This is not BLE.** GATT, characteristics, advertising, beacons and L2CAP CoC are Shiny.BluetoothLE /
   Shiny.BluetoothLE.Hosting. If the device's documentation talks about services and characteristics,
   it is BLE - use the `shiny-bluetoothle` skill instead. A "BLE serial" module (HM-10, nRF UART) is BLE too.
2. **There is no iOS or Mac Catalyst target.** Classic Bluetooth on iOS is ExternalAccessory, which only
   reaches MFi-certified accessories with a protocol string Apple approved for the app. Never generate
   Shiny.Bluetooth code for iOS; tell the user the device needs BLE (or MFi + ExternalAccessory) there.
3. **RFCOMM is a byte stream, like TCP.** Writes are not delivered as messages - one 100-byte write can
   arrive as several reads. Always frame the protocol (line endings, length prefixes) and loop on reads.
4. **Check `Capabilities` before offering an operation** - `RequestDiscoverable` throws
   `BluetoothNotSupportedException` on Windows and macOS.

## Library Overview

| Item       | Value |
|------------|-------|
| NuGet      | `Shiny.Bluetooth`, plus `Shiny.Bluetooth.Linux` on Linux |
| Namespace  | `Shiny.Bluetooth` (types); `Shiny` (registration extensions) |
| Platforms  | Android, Windows, macOS (AppKit), Linux (BlueZ). **No iOS, Mac Catalyst or tvOS.** |

| Platform | Backed by |
|----------|-----------|
| Android | `BluetoothAdapter`, `BluetoothSocket`, `BluetoothServerSocket` |
| Windows | `Windows.Devices.Bluetooth.Rfcomm` (`RfcommDeviceService`, `RfcommServiceProvider`, `StreamSocket`) and a `DeviceWatcher` |
| macOS | IOBluetooth through the Objective-C runtime (the .NET macOS SDK has no binding) - needs an AppKit main loop |
| Linux | BlueZ over D-Bus: `Adapter1`/`Device1`, and `ProfileManager1.RegisterProfile` for every RFCOMM connection in both directions |

## Capability matrix

| Operation | Android | Windows | macOS | Linux |
|-----------|---------|---------|-------|-------|
| `GetPairedDevices` | ✅ | ✅ | ✅ | ✅ |
| `Discover` (~12 s inquiry) | ✅ | ✅ | ✅ | ✅ |
| `Pair` | ✅ system dialog | ✅ system UI | ✅ system prompt | ✅ needs a BlueZ agent for PIN/confirm |
| `Connect` | ✅ | ✅ | ✅ (`secure` ignored) | ✅ |
| `Listen` | ✅ | ✅ | ✅ (`secure` ignored) | ✅ |
| `RequestDiscoverable` | ✅ system dialog | ❌ | ❌ | ✅ |

## Registration

```csharp
// Android, Windows, macOS
builder.Services.AddBluetooth();

// Linux - reference Shiny.Bluetooth.Linux; AddBluetooth does not exist on plain net10.0
services.AddLinuxBluetooth();
```

Both register `IBluetoothManager` as a singleton. iOS/Mac Catalyst apps cannot reference the package
at all - in a multi-targeted MAUI app, condition the PackageReference and the registration:

```xml
<PackageReference Include="Shiny.Bluetooth" Condition="!$(TargetFramework.Contains('-ios')) AND !$(TargetFramework.Contains('maccatalyst'))" />
```
```csharp
#if ANDROID || WINDOWS
builder.Services.AddBluetooth();
#endif
```

## API

```csharp
public interface IBluetoothManager
{
    BluetoothCapabilities Capabilities { get; }
    Task<AccessState> RequestAccess(CancellationToken ct = default);
    Task<IReadOnlyList<IBluetoothDevice>> GetPairedDevices(CancellationToken ct = default);
    Task<IBluetoothDevice> GetDevice(string address, CancellationToken ct = default);   // a known address, no discovery
    IAsyncEnumerable<BluetoothDiscoveryResult> Discover(CancellationToken ct = default);
    Task<BluetoothListener> Listen(Guid serviceUuid, string serviceName, bool secure = true, CancellationToken ct = default);
    Task<bool> RequestDiscoverable(TimeSpan duration, CancellationToken ct = default);
}

public interface IBluetoothDevice   // equality is by Address only
{
    string Address { get; }                 // always "AA:BB:CC:DD:EE:FF"
    string? Name { get; }
    bool IsPaired { get; }                  // as read, and kept current by Pair
    uint? ClassOfDevice { get; }
    BluetoothMajorDeviceClass? MajorDeviceClass { get; }   // decoded from ClassOfDevice

    Task<bool> Pair(CancellationToken ct = default);
    Task<BluetoothConnection> Connect(Guid serviceUuid, bool secure = true, CancellationToken ct = default);
}

public record BluetoothDiscoveryResult(IBluetoothDevice Device, int? Rssi);   // Rssi in dBm from the inquiry

public sealed class BluetoothConnection : IAsyncDisposable, IDisposable
{
    IBluetoothDevice Device; Guid ServiceUuid; Stream Stream;   // dispose either to disconnect
}

public sealed class BluetoothListener : IAsyncDisposable
{
    Guid ServiceUuid; string ServiceName;
    Task<BluetoothConnection> Accept(CancellationToken ct = default);   // dispose to withdraw the SDP record
}

BluetoothServiceUuids.SerialPort           // 00001101-0000-1000-8000-00805F9B34FB
BluetoothServiceUuids.FromShortId(0x1105)  // any SIG 16/32-bit id on the base UUID
```

Exceptions: `BluetoothException` (base), `BluetoothNotSupportedException`, `BluetoothPermissionException`,
`BluetoothConnectionException` (service not found, out of range, refused).

## Patterns

### Connect to a serial (SPP) device and exchange lines

```csharp
public class PrinterService(IBluetoothManager bluetooth)
{
    public async Task<string> SendCommand(string address, string command, CancellationToken ct)
    {
        var access = await bluetooth.RequestAccess(ct);
        if (access != AccessState.Available)
            throw new InvalidOperationException($"Bluetooth is {access}");

        var device = await bluetooth.GetDevice(address, ct);
        await using var connection = await device.Connect(BluetoothServiceUuids.SerialPort, ct: ct);

        // RFCOMM is a stream - frame it. Here: newline-terminated text.
        var writer = new StreamWriter(connection.Stream, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        var reader = new StreamReader(connection.Stream, leaveOpen: true);

        await writer.WriteLineAsync(command.AsMemory(), ct);
        return await reader.ReadLineAsync(ct) ?? "";
    }
}
```

### Let the user pick a device

```csharp
var devices = new List<IBluetoothDevice>(await bluetooth.GetPairedDevices());

// Discover runs ~12 seconds; cancel to stop sooner. Finish it before connecting - an inquiry
// slows connections on the same radio to a crawl.
await foreach (var result in bluetooth.Discover(ct))
{
    Console.WriteLine($"{result.Device} {result.Rssi} dBm");
    if (!devices.Contains(result.Device))   // equality is by address
        devices.Add(result.Device);
}
```

Filter by class when it helps: `device.MajorDeviceClass == BluetoothMajorDeviceClass.Imaging` for printers.
`Name` can be null for a device the OS has never resolved the name of. RSSI belongs to the sighting, so it
is on `BluetoothDiscoveryResult`, not on the device - paired devices from `GetPairedDevices()` have none.

`IBluetoothDevice` is not constructible - get one from `GetPairedDevices()`, `Discover()`, `GetDevice(address)`
or an accepted `BluetoothConnection.Device`. Do not write a class implementing it.

### Pair first, then connect

```csharp
if (!device.IsPaired && !await device.Pair(ct))
    return; // the user declined, or the PIN was wrong
```

`device.Connect(..., secure: true)` (the default) pairs on the way in anyway on Android/Windows; call `Pair`
explicitly when you want the prompt to happen at a predictable moment. Pass `secure: false` for
devices that cannot pair at all.

### Host a service (server)

```csharp
var serviceId = Guid.Parse("8ce255c0-200a-11e0-ac64-0800200c9a66"); // your own UUID
await using var listener = await bluetooth.Listen(serviceId, "My App Sync", ct: ct);

while (!ct.IsCancellationRequested)
{
    var connection = await listener.Accept(ct);
    _ = HandleClient(connection, ct); // one task per connection
}

static async Task HandleClient(BluetoothConnection connection, CancellationToken ct)
{
    await using (connection)
    {
        var buffer = new byte[1024];
        int read;
        while ((read = await connection.Stream.ReadAsync(buffer, ct)) > 0)
            await connection.Stream.WriteAsync(buffer.AsMemory(0, read), ct); // echo
    }
}
```

On an accepted connection `connection.Device` is a full `IBluetoothDevice` - you can `Pair` or `Connect` back to it - but
its `Name` is only filled in where the platform already knew it.
For other devices to find an unpaired server, make it discoverable (Android/Linux):

```csharp
if (bluetooth.Capabilities.HasFlag(BluetoothCapabilities.Discoverable))
    await bluetooth.RequestDiscoverable(TimeSpan.FromMinutes(2), ct);
```

### Read until the remote disconnects

A remote disconnect is a read returning `0` (or an `IOException`). There is no separate
"disconnected" event - the stream is the connection.

## Platform setup

### Android (AndroidManifest.xml)

```xml
<!-- API 31+ -->
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" android:usesPermissionFlags="neverForLocation" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-permission android:name="android.permission.BLUETOOTH_ADVERTISE" /> <!-- only for RequestDiscoverable -->

<!-- API 30 and below -->
<uses-permission android:name="android.permission.BLUETOOTH" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" android:maxSdkVersion="30" />
```

`RequestAccess` returns `NotSetup` when any of these is missing from the manifest. `Discover` and
`Pair` use broadcast receivers registered by the library; nothing else to declare.

### Windows

Packaged apps declare `<DeviceCapability Name="bluetooth" />` and `<DeviceCapability Name="radios" />`.
Unpackaged apps need nothing.

### macOS

`NSBluetoothAlwaysUsageDescription` in Info.plist; `com.apple.security.device.bluetooth` in the
entitlements when sandboxed. IOBluetooth delivers every callback on the main run loop, so this works
in an AppKit app (including Shiny's macOS hosting) - not in a console app with no NSApplication loop.

### Linux

`bluetoothd` running and the process allowed on the system bus for `org.bluez` (root, or the
`bluetooth` group under the default policy). Pairing a device that asks for a PIN or confirmation
needs a BlueZ agent - a desktop session has one; on a headless device run `bluetoothctl` with
`agent on` / `default-agent` or pair once by hand. `Pair` returns false when no agent answers.
`Connect` needs BlueZ to already know the device (paired, or seen by `Discover`). Only `hci0` is used.

## Common mistakes

- Using Shiny.Bluetooth for a BLE device (HM-10, ESP32 in BLE mode, anything with "GATT"). Use Shiny.BluetoothLE.
- Generating iOS code. There is no target - the package will not restore for `net10.0-ios`.
- Treating one `ReadAsync` as one message. Frame the stream.
- Connecting while `Discover` is still enumerating - finish or cancel the inquiry first.
- Forgetting to dispose the `BluetoothConnection` / `BluetoothListener` - the radio link and SDP
  record stay up until you do.
- Calling `RequestDiscoverable` on Windows/macOS without checking `Capabilities`.
