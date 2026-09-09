---
name: shiny-beacons
description: iBeacon and Eddystone ranging, background region monitoring, and broadcasting for .NET MAUI, iOS, Android, macOS, Windows, Linux and Blazor using Shiny.Beacons
auto_invoke: true
triggers:
  - beacon
  - beacons
  - ibeacon
  - eddystone
  - proximity
  - proximity uuid
  - beacon region
  - beacon ranging
  - beacon monitoring
  - beacon advertising
  - beacon broadcasting
  - major minor
  - rssi
  - tx power
  - measured power
  - path loss
  - beacon distance
  - estimote
  - kontakt.io
  - altbeacon
  - Shiny.Beacons
  - IBeaconRangingManager
  - IBeaconMonitoringManager
  - IBeaconMonitorDelegate
  - IEddystoneScanner
  - IBeaconBroadcaster
  - BeaconRegion
  - BeaconRegionState
  - BeaconRangingOptions
  - BeaconIdentity
  - EddystoneUid
  - EddystoneFrame
  - EddystoneUidFrame
  - EddystoneUrlFrame
  - EddystoneTlmFrame
  - EddystoneEidFrame
  - IBeaconPacket
  - IBeaconDistanceEstimator
  - PathLossDistanceEstimator
  - CurveFitDistanceEstimator
  - ManagedBeaconScan
  - AddBeaconRanging
  - AddBeaconMonitoring
  - AddEddystoneScanning
  - AddBeaconBroadcasting
---

# Shiny Beacons

iBeacon and Eddystone ranging, background region monitoring, and broadcasting.

## When to Use This Skill

Use this skill when the user needs to:

- Detect nearby iBeacons and estimate the distance to them
- Be notified when the device enters or leaves a beacon region, including in the background
- Read Eddystone UID, URL or TLM (telemetry) frames
- Turn the device itself into an iBeacon or Eddystone beacon
- Tune beacon distance/proximity accuracy
- Diagnose why beacon distances are jumping around, or why a region never fires

## Library Overview

| Property | Value |
|---|---|
| NuGet | `Shiny.Beacons` |
| Namespace | `Shiny.Beacons` |
| Platforms | iOS, Mac Catalyst, macOS, Android, Windows, Linux, Blazor WebAssembly. **No tvOS target** — tvOS binds no `CLBeacon*` type at all |
| DI Namespace | `Shiny` (extension methods on `IServiceCollection`) |
| Depends on | `Shiny.BluetoothLE`, `Shiny.BluetoothLE.Hosting` |

## The one thing to understand first

**iBeacon and Eddystone travel through different parts of a BLE advertisement, and that dictates
everything else.**

- **iBeacon** is *manufacturer data* under Apple's company id `0x004C`. CoreBluetooth **strips it**
  from every scan result on iOS, Mac Catalyst and macOS. There is no scan configuration that gets it
  back. So on Apple platforms iBeacon goes through **CoreLocation** and costs a **location**
  permission. Everywhere else Shiny parses the advertisement directly and it costs a **Bluetooth**
  permission.
- **Eddystone** is *service data* under UUID `0xFEAA`, which CoreBluetooth passes through untouched.
  It behaves identically on every platform, Apple included.

Never tell a user that an iOS app can range iBeacons over `IBleManager.Scan()`. It cannot.

## Setup

```csharp
// Foreground ranging - "which beacons are near me and how far?"
services.AddBeaconRanging();

// Background region monitoring - "tell me when I enter/leave"
services.AddBeaconMonitoring<MyBeaconMonitorDelegate>();

// Eddystone frames (works everywhere, including Apple)
services.AddEddystoneScanning();

// Become a beacon
services.AddBeaconBroadcasting();
```

All four accept an optional `BeaconRangingOptions`. `AddBeaconMonitoring` also wires the default
repository so regions survive a process restart.

On Linux or any plain-.NET host, register a `IBleManager` first (`AddBluetoothLE()` from
`Shiny.BluetoothLE.Linux`) — the beacon registrations throw a named error if none is present.

## Ranging

```csharp
public class BeaconViewModel(IBeaconRangingManager ranging)
{
    IDisposable? sub;

    public async Task Start()
    {
        var access = await ranging.RequestAccess();
        if (access != AccessState.Available)
            return;

        var region = new BeaconRegion("store-front", Guid.Parse("B9407F30-F5F8-466E-AFF9-25556B57FE6D"));

        this.sub = ranging
            .WhenBeaconRanged(region)
            .Subscribe(beacon =>
            {
                // beacon.Distance is metres, beacon.Proximity is the bucket
                Console.WriteLine($"{beacon.Major}/{beacon.Minor}: {beacon.Distance:N1}m ({beacon.Proximity})");
            });
    }

    public void Stop() => this.sub?.Dispose();
}
```

Ranging starts on first subscription and stops when the last subscription is disposed. It is a
**foreground** activity — use monitoring for anything that has to work with the app closed.

### ManagedBeaconScan for UI

`WhenBeaconRanged` emits once per advertisement, which is far too chatty to bind a list to. Use the
managed scan, which keeps one entry per beacon and updates it in place:

```csharp
var scan = ranging.CreateManagedScan();
await scan.Start(region, scheduler, clearTime: TimeSpan.FromSeconds(15));

// scan.Beacons is an INotifyReadOnlyCollection<ManagedBeacon>
```

`clearTime` drops beacons that stop advertising, so the list reflects what is actually in range.

## Monitoring

```csharp
public class MyBeaconMonitorDelegate(INotificationManager notifications) : IBeaconMonitorDelegate
{
    public Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region)
        => notifications.Send("Beacons", $"{region.Identifier}: {newStatus}");
}

// somewhere in your app
var access = await monitoring.RequestAccess();   // asks for ALWAYS location on Apple
if (access == AccessState.Available)
    await monitoring.StartMonitoring(new BeaconRegion("store-front", uuid, major: 1));
```

Monitored regions are persisted and re-armed on the next launch. Monitoring reports **only**
entry/exit — it never reports distance. Range the region in the foreground when you need that.

## Eddystone

```csharp
var access = await scanner.RequestAccess();

scanner.WhenFrameReceived().Subscribe(frame =>
{
    switch (frame)
    {
        case EddystoneUidFrame uid:
            Console.WriteLine($"{uid.Uid.Namespace}/{uid.Uid.Instance} at {uid.Distance:N1}m");
            break;

        case EddystoneUrlFrame url:
            Console.WriteLine(url.Url);
            break;

        case EddystoneTlmFrame { IsEncrypted: false } tlm:
            // BatteryVolts is null on a mains-powered beacon, TemperatureCelsius null with no sensor
            Console.WriteLine($"{tlm.BatteryVolts}V {tlm.TemperatureCelsius}C up {tlm.Uptime}");
            break;
    }
});
```

Correlate a TLM frame with the beacon's UID/URL frame using `frame.PeripheralId` — a beacon
interleaves frame types, and only the peripheral identifier ties them together.

EID frames surface their raw rotating identifier as `EddystoneEidFrame.EphemeralId`. Resolving one
back to a registered beacon needs the deployment's identity key and the Curve25519/AES-EAX
derivation from the spec, which this library does **not** implement.

## Broadcasting

```csharp
await broadcaster.StartIBeacon(uuid, major: 1, minor: 2);
await broadcaster.StartEddystoneUid(EddystoneUid.Parse("0102030405060708090A", "0B0C0D0E0F10"));
await broadcaster.StartEddystoneUrl("https://shinylib.net/");
broadcaster.Stop();
```

Only one advertisement runs at a time; starting a second replaces the first.

## Code Generation Instructions

1. **Always `RequestAccess()` and check the `AccessState` before ranging, monitoring or broadcasting.**
2. **Inject the managers** (`IBeaconRangingManager`, `IBeaconMonitoringManager`, `IEddystoneScanner`,
   `IBeaconBroadcaster`) — never instantiate them.
3. **Dispose the ranging subscription.** Ranging runs until the last subscriber goes away; leaving a
   subscription alive keeps the radio (and on Apple, CoreLocation) busy.
4. **`BeaconRegion` requires a unique `Identifier` and a UUID.** `Major` is optional; `Minor` requires
   `Major`. `0` is a legal value for both — do not treat it as "unset".
5. **Use `IBeaconMonitorDelegate` for background transitions**, never a subscription. The app may not
   be running when the transition fires.
6. **Bind lists through `ManagedBeaconScan`**, not through raw `WhenBeaconRanged` output.
7. **Prefer `Beacon.Distance` over `Beacon.Rssi`** in user-facing code. `Rssi` is the raw single-packet
   reading and is very noisy; `Distance` is computed from filtered signal.
8. **`Beacon.TxPower` is null on Apple platforms.** CoreLocation never surfaces the raw advertisement.
   Do not write code that requires it cross-platform.
9. **Do not compare `Beacon` records for identity** — every observation differs in RSSI and timestamp.
   Use `Beacon.Identity` (a `BeaconIdentity` of UUID/major/minor).
10. **Use `IBeaconPacket`** (in `Shiny.BluetoothLE`) to read or build a raw iBeacon payload. Never
    hand-roll it with `Guid.ToByteArray()` or `BitConverter.GetBytes()` — both are little-endian and
    iBeacon is big-endian throughout.

## Tuning distance accuracy

Everything lives on `BeaconRangingOptions`, passed to the `Add*` call:

```csharp
services.AddBeaconRanging(new BeaconRangingOptions
{
    // cluttered indoor space attenuates faster than free space (2.0)
    DistanceEstimator = new PathLossDistanceEstimator(3.0),

    // shorter window reacts faster to movement; longer reads more steadily when stationary
    RssiFilterWindow = TimeSpan.FromSeconds(10),

    // when a beacon advertises no calibration value
    DefaultTxPower = -59,

    ImmediateThreshold = 0.5,
    NearThreshold = 3.0,
    RegionExitTimeout = TimeSpan.FromSeconds(30)
});
```

- **`PathLossDistanceEstimator` (default)** — `d = 10^((txPower - rssi) / (10n))`. Hardware-neutral
  and predictable. Raise `n` towards 3-4 indoors.
- **`CurveFitDistanceEstimator`** — the Radius Networks/AltBeacon empirical fit. Use it for parity
  with other Android beacon stacks; its constants were fitted to one specific device.
- Implement `IBeaconDistanceEstimator` for your own model.

Samples are **always** fed through a windowed trimmed-mean filter first (`RssiFilter`), which is what
actually removes the jitter. On Apple platforms CoreLocation does its own filtering and hands back a
distance directly, so the filter and estimator are bypassed there — only the thresholds apply.

## Conventions

- `Proximity`: `Unknown`, `Immediate` (<0.5m), `Near` (<3m), `Far`.
- `BeaconRegionState`: `Unknown`, `Entered`, `Exited`.
- `Beacon.Distance` is metres; a **negative value means unknown**, not "very close".
- `EddystoneUid` is a value type; `Namespace` is 20 hex chars, `Instance` is 12.
- Eddystone calibrates transmit power at **0 metres**, iBeacon at **1 metre** — a 41 dBm difference
  the parser accounts for. Do not "fix" an Eddystone TxPower by comparing it to an iBeacon one.
- `AccessState` is from Shiny.Core: `Available`, `Denied`, `Disabled`, `Restricted`, `NotSupported`, `Unknown`.

## Platform Notes

| Platform | Ranging | Monitoring | Eddystone | Broadcast |
|---|---|---|---|---|
| iOS / Mac Catalyst | CoreLocation | `CLMonitor` (18+), `CLLocationManager` below | BLE scan | iBeacon only |
| macOS | CoreLocation | **Not supported by the OS** | BLE scan | iBeacon only |
| Android | BLE scan | BLE scan + foreground service | BLE scan | iBeacon + Eddystone |
| Windows | BLE scan | BLE scan | BLE scan | iBeacon + Eddystone |
| Linux | BLE scan | BLE scan | BLE scan | iBeacon + Eddystone |
| Blazor WASM | BLE scan | BLE scan | BLE scan | No |
| tvOS | — | — | — | — |

- **macOS monitoring** registers successfully but reports `AccessState.NotSupported` and throws
  `PlatformNotSupportedException` from `StartMonitoring`. This is deliberate so shared startup code
  runs unchanged; branch on `CurrentStatus` rather than on `OperatingSystem.IsMacOS()`.
- **iOS caps an app at 20 monitored regions** across beacons *and* geofences together. Below iOS 18
  Shiny throws a named error at the cap; iOS itself would silently drop the excess.
- **Apple cannot broadcast Eddystone.** `startAdvertising` accepts only a local name and service
  UUIDs. `StartEddystoneUid`/`StartEddystoneUrl` throw `PlatformNotSupportedException` there.
- **Apple broadcasting stops working when backgrounded.** iOS moves the advertisement into an
  overflow area only another iOS device explicitly scanning for the same service can read.
- **Linux broadcasting needs a BlueZ that will let you register an advertisement.** BlueZ calls back
  into the process to read the payload, and the adapter has to be powered. `bluetoothd` limits how
  many advertising instances are active at once (`LEAdvertisingManager1.SupportedInstances`); when it
  is full, `RegisterAdvertisement` fails and the exception carries BlueZ's own reason.
- **Blazor** needs `navigator.bluetooth.requestLEScan`, which is Chromium-only and behind
  `chrome://flags/#enable-experimental-web-platform-features`. The chooser fallback reports no
  advertisement payload, so beacons are invisible through it.

## Permissions

**iOS / Mac Catalyst `Info.plist`**
- `NSLocationWhenInUseUsageDescription` — required for ranging
- `NSLocationAlwaysAndWhenInUseUsageDescription` — required for monitoring
- `NSBluetoothAlwaysUsageDescription` — required for Eddystone and broadcasting
- Word the "always" string for what background monitoring actually does. The system shows that
  upgrade prompt once, so ask from the feature that needs it rather than at launch.

**Android `AndroidManifest.xml`**
```xml
<uses-permission android:name="android.permission.BLUETOOTH_SCAN"
                 android:usesPermissionFlags="neverForLocation" />
<uses-permission android:name="android.permission.BLUETOOTH_ADVERTISE" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<!-- monitoring only -->
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_CONNECTED_DEVICE" />
```
The foreground service type is `connectedDevice`, **not** `location` — Android 14 rejects a mismatched
type, and a BLE scan is not a location activity.

**Windows** — `bluetooth` capability in the app manifest for a packaged app.

## Best Practices

- Range in the foreground, monitor in the background. Do not try to keep a ranging subscription alive
  to approximate monitoring — it will be killed and it will drain the battery.
- Keep the region as narrow as the use case allows. A UUID-only region matches every beacon in the
  deployment; adding `Major` cuts the scanning work and the false positives.
- Treat `Distance` as an estimate with metres of error, not a measurement. Design around
  `Proximity` buckets or "closest beacon wins" rather than absolute distances.
- Calibrate `DefaultTxPower` against your actual hardware if the beacons advertise `0`. The published
  measured power of a beacon at one metre is the number to use.
- Raise `RegionExitTimeout` rather than lower it if regions flap. The default 30s already tolerates a
  1 Hz beacon interval and a throttled background scan.
- Check `CurrentStatus` before offering beacon features in the UI, so a macOS or tvOS build degrades
  gracefully instead of throwing.
