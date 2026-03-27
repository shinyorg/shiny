---
name: shiny-ble-hosting
description: Generate code using Shiny.BluetoothLE.Hosting, a BLE peripheral hosting library for .NET with GATT server, advertising, and managed characteristic patterns
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
  - BleGattCharacteristic
  - BleGattCharacteristicAttribute
  - AddBluetoothLeHosting
  - AddBleHostedCharacteristic
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
  - AttachRegisteredServices
  - DetachRegisteredServices
  - ibeacon advertise
  - ble notify
  - ble indicate
  - ble read characteristic
  - ble write characteristic
---

# Shiny.BluetoothLE.Hosting Skill

You are an expert in Shiny.BluetoothLE.Hosting, a .NET library for turning a mobile device into a BLE peripheral. It provides a GATT server, BLE advertising, iBeacon broadcasting, and a managed characteristic pattern using dependency injection and attribute-based registration.

## When to Use This Skill

Invoke this skill when the user wants to:
- Set up a BLE GATT server on a mobile device (iOS, Android)
- Advertise as a BLE peripheral with custom service UUIDs or a local name
- Broadcast as an iBeacon
- Create GATT services with read, write, and notify characteristics
- Handle read requests from connected centrals
- Handle write requests from connected centrals
- Send notifications or indications to subscribed centrals
- Use the managed `BleGattCharacteristic` pattern with DI registration
- Configure characteristic properties (read, write, notify, indicate, encryption)
- React to central subscribe/unsubscribe events
- Build a MAUI app that acts as a BLE peripheral

## Library Overview

- **NuGet**: `Shiny.BluetoothLE.Hosting`
- **Namespaces**: `Shiny.BluetoothLE.Hosting`, `Shiny.BluetoothLE.Hosting.Managed`
- **Platforms**: iOS, Android (no Windows support for full hosting)
- **Dependencies**: `Shiny.Core`, `Shiny.BluetoothLE.Common`

The library has two usage patterns:
1. **Imperative API** -- inject `IBleHostingManager`, call `AddService` with a builder lambda to configure characteristics inline
2. **Managed pattern** -- create `BleGattCharacteristic` subclasses decorated with `[BleGattCharacteristic]`, register them via `AddBleHostedCharacteristic<T>()`, and attach/detach them at runtime

## Setup

### 1. Install NuGet Package
```bash
dotnet add package Shiny.BluetoothLE.Hosting
```

### 2. Register in MauiProgram.cs

**Imperative (no managed characteristics):**
```csharp
builder.Services.AddBluetoothLeHosting();
```

**Managed pattern (preferred for structured services):**
```csharp
builder.Services.AddBleHostedCharacteristic<MyReadCharacteristic>();
builder.Services.AddBleHostedCharacteristic<MyWriteCharacteristic>();
// AddBluetoothLeHosting() is called automatically by AddBleHostedCharacteristic
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

### 3. Managed Characteristic Pattern

Create a class that extends `BleGattCharacteristic` and decorate it with `[BleGattCharacteristic]`:

```csharp
[BleGattCharacteristic("12345678-1234-1234-1234-123456789abc", "12345678-1234-1234-1234-123456789ab1")]
public class MyCharacteristic : BleGattCharacteristic
{
    public override Task OnStart()
    {
        // Called when the service is attached
        return Task.CompletedTask;
    }

    public override void OnStop()
    {
        // Called when the service is detached
    }

    public override Task<GattResult> OnRead(ReadRequest request)
    {
        var data = System.Text.Encoding.UTF8.GetBytes("Hello");
        return Task.FromResult(GattResult.Success(data));
    }

    public override Task OnWrite(WriteRequest request)
    {
        var received = request.Data;
        if (request.IsReplyNeeded)
            request.Respond(GattState.Success);
        return Task.CompletedTask;
    }

    public override Task OnSubscriptionChanged(IPeripheral peripheral, bool subscribed)
    {
        // React to central subscribing/unsubscribing
        return Task.CompletedTask;
    }
}
```

Register in DI:
```csharp
builder.Services.AddBleHostedCharacteristic<MyCharacteristic>();
```

Attach at runtime:
```csharp
await hostingManager.AttachRegisteredServices();
await hostingManager.StartAdvertising(new AdvertisementOptions("MyDevice", "12345678-1234-1234-1234-123456789abc"));
```

### 4. Advertising

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

### 5. iBeacon Broadcasting

```csharp
await hostingManager.AdvertiseBeacon(
    uuid: Guid.Parse("12345678-1234-1234-1234-123456789abc"),
    major: 1,
    minor: 100,
    txpower: -59
);
```

### 6. Sending Notifications

```csharp
// From an IGattCharacteristic reference
var data = System.Text.Encoding.UTF8.GetBytes("Updated value");

// Notify all subscribed centrals
await characteristic.Notify(data);

// Notify specific centrals
await characteristic.Notify(data, specificPeripheral1, specificPeripheral2);
```

### 7. Request Pattern (Write + Notify Response)

In the managed pattern, override `Request` instead of `OnWrite` to receive a write, process it, and automatically respond via notification:

```csharp
[BleGattCharacteristic("service-uuid", "char-uuid")]
public class MyRequestCharacteristic : BleGattCharacteristic
{
    public override Task<GattResult> Request(WriteRequest request)
    {
        // Process incoming data
        var received = System.Text.Encoding.UTF8.GetString(request.Data);

        // Return result -- this is sent back as a notification to the requesting central
        var response = System.Text.Encoding.UTF8.GetBytes($"Echo: {received}");
        return Task.FromResult(GattResult.Success(response));
    }
}
```

Note: `Request` and `OnWrite` cannot both be overridden on the same characteristic.

### 8. Responding to Write Requests

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

### 9. File Organization

- Managed characteristics: `BleHosting/{Name}Characteristic.cs`
- Or by feature: `Features/{Feature}/{Name}Characteristic.cs`

## Namespace Ambiguities

- **`IPeripheral`**: Both `Shiny.BluetoothLE` (client) and `Shiny.BluetoothLE.Hosting` define an `IPeripheral` interface with different members. If both packages are referenced in the same project, do NOT add both namespaces as global usings. Use file-level `using` directives or FQN (`Shiny.BluetoothLE.Hosting.IPeripheral`) to disambiguate.

## Best Practices

1. **Always request access first** -- call `RequestAccess()` and check the result before any hosting operations
2. **Use the managed pattern for structured services** -- `BleGattCharacteristic` subclasses are easier to test and maintain
3. **Use valid UUIDs** -- standard 128-bit UUID format (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`) or short 16-bit UUIDs for standard Bluetooth SIG services
4. **Respond to writes when needed** -- always check `WriteRequest.IsReplyNeeded` and call `Respond` with the appropriate `GattState`
5. **Return GattResult.Error on failures** -- use `GattResult.Error(GattState.Failure)` in read handlers when an error occurs
6. **Stop advertising before cleanup** -- call `StopAdvertising()` and `ClearServices()` when done
7. **Do not override both Request and OnWrite** -- they are mutually exclusive on a managed characteristic
8. **Use AttachRegisteredServices for managed setup** -- this wires up all DI-registered `BleGattCharacteristic` instances to the GATT server
9. **DetachRegisteredServices to tear down** -- cleanly removes managed services and calls `OnStop` on each characteristic
10. **Check IsAdvertising** -- avoid calling `StartAdvertising` if already advertising

## Reference Files

For detailed API signatures and examples, see:
- `reference/api-reference.md` - Full API surface, interfaces, enums, records, and usage examples
