---
name: shiny-mcumgr
description: Generate code using Shiny.BluetoothLE.McuMgr - an MCUmgr (SMP) client over Bluetooth LE for Zephyr RTOS, nRF Connect SDK, Mynewt and NuttX devices that boot through MCUboot - over-the-air firmware updates (upload, test, confirm, reset, multi-image nRF5340, dfu_application.zip), plus OS, image, file system, shell and stats management
auto_invoke: true
triggers:
  - mcumgr
  - MCUmgr
  - SMP
  - simple management protocol
  - mcuboot
  - MCUboot
  - zephyr
  - Zephyr RTOS
  - nRF Connect SDK
  - nrf connect device manager
  - nordic dfu
  - nRF52
  - nRF5340
  - nRF54
  - firmware update
  - firmware upgrade
  - OTA update
  - FOTA
  - DFU
  - dfu_application.zip
  - app_update.bin
  - zephyr.signed.bin
  - image upload
  - test and confirm
  - McuMgrClient
  - CreateMcuMgrClient
  - HasMcuMgrAsync
  - BleSmpTransport
  - BleSmpTransportOptions
  - ISmpTransport
  - UpgradeFirmwareAsync
  - FirmwareImage
  - FromDfuPackageAsync
  - FirmwareUpgradeOptions
  - FirmwareUpgradeMode
  - FirmwareUpgradeProgress
  - FirmwareUpgradeResult
  - McuBootImage
  - McuBootVersion
  - McuMgrImageSlot
  - McuMgrException
  - McuMgrError
  - McuMgrGroups
  - McuMgrUploadOptions
  - SmpHeader
  - SmpCbor
---

# Shiny MCUmgr (`Shiny.BluetoothLE.McuMgr`)

MCUmgr's **SMP** (Simple Management Protocol) is how Zephyr / nRF Connect SDK / Mynewt / NuttX devices running
**MCUboot** take firmware updates and management commands. Over BLE it is one GATT service with one characteristic:

| | UUID |
|---|---|
| SMP service | `8d53dc1d-1db7-4cd3-868b-8a527460aa84` (`BleSmpTransport.ServiceUuid`) |
| SMP characteristic (write without response + notify) | `da2e7828-fbce-4e01-ae9e-261174997c48` (`BleSmpTransport.CharacteristicUuid`) |

The package is a plain `net10.0` library on top of `Shiny.BluetoothLE`'s `IPeripheral` - **no DI registration and no
platform code**. It works wherever `Shiny.BluetoothLE` (iOS, Mac Catalyst, macOS, Android, Windows, Blazor Web
Bluetooth) or `Shiny.BluetoothLE.Linux` provides peripherals. Register BLE as usual (`AddBluetoothLE()` /
`AddLinuxBluetoothLE()`), scan, and hand the `IPeripheral` over.

Namespace: `using Shiny.BluetoothLE.McuMgr;`

## Firmware update (the common case)

```csharp
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.McuMgr;

// peripheral from IBleManager.Scan(...) - filter on BleSmpTransport.ServiceUuid when the device advertises it
using var client = peripheral.CreateMcuMgrClient();     // connects on first use

var image = new FirmwareImage(await File.ReadAllBytesAsync(path));   // the SIGNED image - throws InvalidDataException otherwise
var result = await client.UpgradeFirmwareAsync(
    image,
    new FirmwareUpgradeOptions
    {
        Mode = FirmwareUpgradeMode.TestAndConfirm,          // default
        EstimatedSwapTime = TimeSpan.FromSeconds(10)        // wait after reset while MCUboot swaps
    },
    new Progress<FirmwareUpgradeProgress>(p =>
    {
        // p.Stage: Validating, Uploading, Testing, Resetting, Reconnecting, Confirming, Completed
        // p.Upload (during Uploading): TransferProgress - PercentComplete (0..1), BytesPerSecond, EstimatedTimeRemaining
    }),
    cancellationToken
);

if (result.AlreadyUpToDate) { /* device already runs this image - nothing was sent */ }
```

What `UpgradeFirmwareAsync` does: read image state -> skip images the device already **runs** (and confirm them if
they are an unconfirmed test) or already **holds** in the secondary slot (no re-upload) -> upload -> mark for test ->
reset -> wait `EstimatedSwapTime` -> reconnect (within `ReconnectTimeout`, default 60s) -> confirm. If the new image is
not running after the reset it throws `McuMgrException` (MCUboot rejected or reverted it).

| `FirmwareUpgradeMode` | After upload |
|---|---|
| `TestAndConfirm` (default) | test, reset, reconnect, confirm. Safe: a bad image reverts on the next reset. |
| `TestOnly` | test, reset. The new firmware must confirm itself (or call `client.Images.ConfirmRunningAsync()` later). |
| `ConfirmOnly` | permanent swap, reset. No way back. |
| `UploadOnly` | nothing swapped. |

### Which file

- Zephyr: `build/zephyr/zephyr.signed.bin` (NOT `zephyr.bin` / `.hex`).
- nRF Connect SDK: `build/dfu_application.zip` (or `app_update.bin` inside it). Use the zip for multi-image parts:

```csharp
await using var zip = File.OpenRead("dfu_application.zip");
var images = await FirmwareImage.FromDfuPackageAsync(zip);   // reads manifest.json; image_index 1 = nRF5340 network core
await client.UpgradeFirmwareAsync(images, options, progress, ct);
```

SUIT packages (`suit-envelope`) are not MCUboot updates and throw `NotSupportedException`.

`McuBootImage.Parse(bytes)` reads `Version` (`McuBootVersion`, `ToString()` matches the device's `1.2.3[.build]`) and
`Hash` (what image state reports) without a device - use it to show "update 1.2.0 -> 1.3.0" before starting.

### Throughput / chunking

`FirmwareUpgradeOptions` inherits `McuMgrUploadOptions`:

- `MaxFrameSize` - null (default) asks the device's SMP buffer size (`os` params) and falls back to the BLE MTU.
- `UseDeviceBufferSize` (default true) - frames larger than the MTU are split across writes and reassembled by the
  device. Set **false** for firmware without `CONFIG_MCUMGR_TRANSPORT_BT_REASSEMBLY` that still reports params
  (symptom: uploads fail or time out on the first chunk).
- `DataAlignment` (1/2/4/8/16) for flash drivers that need aligned writes.
- `FirstChunkTimeout` (default 40s) - the first chunk waits for the slot erase.

On Android, `BleSmpTransportOptions.RequestedMtu` (default 517) is requested on connect. Uploads resume: cancelling
mid-upload and starting again with the same file continues from the device's offset.

## Other commands

```csharp
await client.Os.EchoAsync("ping");                      // link test
await client.Os.ResetAsync();
var p = await client.Os.GetParametersAsync();           // BufferSize, BufferCount
var info = await client.Os.GetApplicationInfoAsync();   // uname-style string
var boot = await client.Os.GetBootloaderNameAsync();    // "MCUboot"
await client.Os.EraseStorageAsync();                    // Zephyr basic group 63 - settings/storage partition

IReadOnlyList<McuMgrImageSlot> slots = await client.Images.GetStateAsync();   // Image, Slot, Version, Hash, Active, Confirmed, Pending, Permanent
await client.Images.UploadAsync(bytes, image: 0, progress: p);
await client.Images.SetStateAsync(hash, confirm: false); // test on next reset
await client.Images.ConfirmRunningAsync();
await client.Images.EraseAsync();

await client.Files.UploadAsync("/lfs1/config.json", bytes);
byte[] log = await client.Files.DownloadAsync("/lfs1/log.txt");
long size = await client.Files.GetSizeAsync(path);
byte[] sha = await client.Files.GetSha256Async(path);
uint crc = await client.Files.GetCrc32Async(path);

var r = await client.Shell.ExecuteAsync("kernel", "uptime");  // r.Output, r.ReturnCode
var groups = await client.Stats.ListGroupsAsync();
var counters = await client.Stats.ReadAsync(groups[0]);
```

Any other or application-defined group (`McuMgrGroups.PerUser` = 64 and up):

```csharp
IReadOnlyDictionary<string, object?> response = await client.SendAsync(
    SmpOperation.Write, 64, 0,
    new Dictionary<string, object?> { ["value"] = 5 }
);
long? v = response.GetInt64("result");   // SmpPayloadExtensions: GetInt64/GetString/GetBytes/GetBoolean/GetList/GetMap
```

Payload values: integers, `string`, `byte[]`, `bool`, `double`, `null`, lists, nested `Dictionary<string, object?>`.
Integers decode as `long`.

## Errors

- `McuMgrException.Error` (`McuMgrError`, e.g. `NotSupported` when the group is not compiled into the firmware) for
  SMP v1 / transport-level `rc` errors.
- `McuMgrException.GroupErrorCode` + `Group` for SMP v2 `err` maps - the code's meaning depends on the group.
- `TimeoutException` when the device does not answer within `McuMgrClientOptions.RequestTimeout` (default 10s).
- The client starts in SMP v2 and drops to v1 by itself for older firmware (`client.Version`).

## Rules

- **Don't run two clients against one peripheral** - SMP has one characteristic; requests from one client queue.
- Dispose the client when finished; it releases the notification subscription but leaves the connection to the app.
- The firmware needs `CONFIG_MCUMGR_TRANSPORT_BT` + the groups you call (`CONFIG_MCUMGR_GRP_IMG`, `_OS`, `_FS`,
  `_SHELL`, `_STAT`). Pairing/bonding requirements (`CONFIG_MCUMGR_TRANSPORT_BT_PERM_RW_AUTHEN`) are the platform's -
  the OS shows its pairing prompt on the first write.
- Keep the app in the foreground during an upload on iOS - a backgrounded app can lose the link.
- `ISmpTransport` is public: implement `MaxFrameSize`, `SendAsync(frame)` (return the response with the matching
  sequence byte) and `ReconnectAsync(delay, timeout)` for serial/UDP or tests, then `new McuMgrClient(transport)`.
