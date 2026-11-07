---
name: shiny-accessorysetup
description: Generate code using Shiny.BluetoothLE.AccessorySetup - the system accessory pairing sheet for BLE devices (AccessorySetupKit on iOS 18+, CompanionDeviceManager on Android) with no MFi, no location prompt and no general Bluetooth prompt on iOS; picked accessories open as Shiny.BluetoothLE peripherals, and Android adds background presence (device appeared / disappeared) events
auto_invoke: true
triggers:
  - AccessorySetupKit
  - accessory setup
  - ASAccessorySession
  - ASPickerDisplayItem
  - ASDiscoveryDescriptor
  - CompanionDeviceManager
  - companion device
  - CompanionDeviceService
  - AssociationRequest
  - BluetoothLeDeviceFilter
  - device association
  - observe device presence
  - AirPods style pairing
  - AirPods popup
  - pairing sheet
  - pair accessory
  - accessory picker
  - without MFi
  - IAccessorySetupManager
  - IAccessorySetupDelegate
  - AddAccessorySetup
  - AccessoryPickerItem
  - AccessoryDiscoveryFilter
  - AccessoryPickerOptions
  - AccessoryRange
  - SetupAccessory
  - AccessorySetupException
  - AccessorySetupErrorCode
  - StartObservingPresence
  - ShinyCompanionDeviceService
  - NSAccessorySetupSupports
  - NSAccessorySetupBluetoothServices
---

# Shiny Accessory Setup (`Shiny.BluetoothLE.AccessorySetup`)

The OS's own "set up your accessory" UI for a BLE device, behind one API:

| | iOS 18+ | Android 8+ |
|---|---|---|
| Framework | AccessorySetupKit | CompanionDeviceManager |
| UI | Half-sheet with product image | System dialog (no image) |
| Proximity (`AccessoryRange.Immediate`) | Yes | Ignored |
| Replaces | Bluetooth permission prompt (if the app never scans) | Location permission for scanning - **not** `BLUETOOTH_CONNECT` |
| Presence wake-ups | No | Android 12+ (`StartObservingPresence`) |
| MFi / registration | None | None |

Not AirPods' automatic popup (that is Apple-only proximity pairing; Android's equivalent is Google Fast Pair, which
needs firmware support and Google registration). The app must be in the foreground and call `ShowPicker`.

Elsewhere (`net10.0`, Windows, macOS, Mac Catalyst, Linux) nothing is registered - resolve
`IAccessorySetupManager?` and fall back to a normal `IBleManager.Scan`.

Namespaces: `using Shiny.BluetoothLE.AccessorySetup;` and `using Shiny.BluetoothLE;` (for `GetPeripheral`).

## Registration

```csharp
services.AddBluetoothLE();                         // to talk to the accessory once picked
services.AddAccessorySetup();                      // or AddAccessorySetup<MyAccessoryDelegate>()
```

## Pick an accessory, then use it as a peripheral

```csharp
public class PairingViewModel(IAccessorySetupManager setup, IBleManager ble)
{
    public async Task Pair()
    {
        if (!setup.IsSupported)
            return; // iOS < 18 or device without companion_device_setup: fall back to ble.Scan(...)

        var accessory = await setup.ShowPicker(new AccessoryPickerItem(
            "Shiny Sensor",
            new AccessoryDiscoveryFilter
            {
                ServiceUuid = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E",
                Range = AccessoryRange.Immediate        // iOS: only when held next to the phone
            })
        {
            ProductImage = "sensor.png",                // iOS: bundle image name or file path; transparent PNG
            AllowRename = true                          // iOS
        });

        if (accessory == null)
            return; // dismissed or nothing found - not an error

        var peripheral = ble.GetPeripheral(accessory);  // no scan needed
        await peripheral!.ConnectAsync();
    }
}
```

- `ShowPicker` returns `null` when the user dismisses the sheet or nothing is found. It throws
  `AccessorySetupException` for a real failure: `PickerAlreadyActive`, `Restricted`, `InvalidRequest` (bad filter, or a
  filter missing from Info.plist), `NotSupported`, `Failed`.
- Filters are validated up front: one of `ServiceUuid` / `CompanyIdentifier` is required; `ServiceData` needs
  `ServiceUuid`; `ManufacturerData` needs `CompanyIdentifier` (bytes after the 2 company-id bytes); masks match their
  data's length. `NameSubstring` narrows further. `RequiresPairing = true` makes the iOS sheet run LE bonding.
- Several items: iOS offers each match with its own name/image; Android lists every match.
  `new AccessoryPickerOptions { AndroidSingleDevice = true }` asks "pair with this device?" for the first match only.
  `DiscoveryTimeout` is iOS 26+.
- `GetAccessories()` lists current grants; `Remove(accessory)` revokes one.
- `SetupAccessory.Identifier` = iOS Bluetooth identifier / Android association id (MAC address before Android 13).
  `PeripheralUuid` is the Shiny peripheral id; `MacAddress` is Android only.

## Delegate (removals in Settings, renames, presence)

```csharp
public class MyAccessoryDelegate : IAccessorySetupDelegate
{
    // every method has a no-op default - override what you need
    public Task OnAccessoryRemoved(SetupAccessory accessory) => ...;           // incl. user removing it in Settings (iOS)
    public Task OnAccessoryChanged(SetupAccessory accessory) => ...;           // iOS rename / state change
    public Task OnPresenceChanged(SetupAccessory accessory, bool isNearby) => ...; // Android 12+, app woken if needed
}
```

Android presence:

```csharp
if (setup.IsPresenceSupported)
    await setup.StartObservingPresence(accessory);   // StopObservingPresence to undo
```

## Platform setup

### iOS - Info.plist (required, or the picker returns InvalidRequest)

```xml
<key>NSAccessorySetupSupports</key>
<array><string>Bluetooth</string></array>
<key>NSAccessorySetupBluetoothServices</key>
<array><string>6E400001-B5A3-F393-E0A9-E50E24DCCA9E</string></array>
<!-- when the filters use them: NSAccessorySetupBluetoothCompanyIdentifiers (each CompanyIdentifier) -->
<key>NSAccessorySetupBluetoothNames</key>
<array><string>Shiny</string></array>
```

Leave `NSBluetoothAlwaysUsageDescription` out (and never call `Scan`) to avoid the Bluetooth prompt entirely -
CoreBluetooth then only sees picked accessories. Add it back if the app must also scan for other devices.

### Android - AndroidManifest.xml

```xml
<uses-feature android:name="android.software.companion_device_setup" android:required="false" />
<!-- GATT on Android 12+ still needs this runtime permission -->
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<!-- only for StartObservingPresence -->
<uses-permission android:name="android.permission.REQUEST_OBSERVE_COMPANION_DEVICE_PRESENCE" />
```

`ShinyCompanionDeviceService` is declared by the package (merged into the manifest). The picker's result comes back
through the activity result, so the host must forward `OnActivityResult` (MAUI does; native hosts derive from
`ShinyAndroidActivity`).

## Do not

- Do not create your own `ASAccessorySession` or call `CompanionDeviceManager.Associate` alongside this package.
- Do not treat a null `ShowPicker` result as an error.
- Do not assume presence events on iOS - `IsPresenceSupported` is false there.
