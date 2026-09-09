# Plan: Shiny.Beacons (revival)

Status: **implemented**
Last updated: 2026-09-09

## Summary

`Shiny.Beacons` was removed in `dc1ea03d` (2026-03-26). This brings it back across **every platform
this repo targets except tvOS**, with the Android ranging/distance math rewritten, the iBeacon
broadcast endianness bugs fixed, and **Eddystone (UID / URL / TLM)** added as a first-class
citizen alongside iBeacon.

Scope decisions taken up front:

| Decision | Choice | Consequence |
|---|---|---|
| Packaging | **One `Shiny.Beacons`, and only one** | Observing and broadcasting ship together, referencing `Shiny.BluetoothLE` and `Shiny.BluetoothLE.Hosting`. No `Shiny.Beacons.Blazor` turned out to be needed - see below. |
| Eddystone | **UID + URL + TLM** | Parse and broadcast UID/URL, parse TLM (plain + encrypted passthrough). EID frames surface as raw 8-byte identifiers, unresolved — no Curve25519/EAX crypto. |
| Platforms | iOS, Mac Catalyst, Android, macOS, Windows, Linux, Blazor | **tvOS excluded** — see binding verification. |
| Engine | **One shared managed engine over `IBleManager`** | Android/Windows/Linux/Blazor/plain-.NET all run the same parse → filter → distance → region state machine. Apple overrides iBeacon only, because CoreBluetooth hides it. |

## Binding verification

Dumped with `MetadataLoadContext` from the ref packs on this machine (`Microsoft.iOS.dll`,
`Microsoft.macOS.dll`, `Microsoft.MacCatalyst.dll`, `Microsoft.tvOS.dll` 26.5.10315;
`Microsoft.Windows.SDK.NET.dll` 10.0.19041.57).

| Platform | Type | Present |
|---|---|---|
| iOS, Mac Catalyst, macOS | `CLBeacon`, `CLBeaconRegion`, `CLBeaconIdentityConstraint` | ✅ |
| iOS, Mac Catalyst, macOS | `CLBeaconIdentityCondition : CLCondition` (for `CLMonitor`) | ✅ |
| iOS, Mac Catalyst | `CLLocationManager.StartMonitoring(CLRegion)` + `StartRangingBeacons` | ✅ |
| macOS | `StartRangingBeacons(CLBeaconIdentityConstraint)` — **but no beacon region monitoring** | ⚠️ ranging only |
| **tvOS** | **no `CLBeacon*` type of any kind** — CoreLocation there is GPS-only | ❌ |
| Windows | `BluetoothLEAdvertisement.ManufacturerData` / `.DataSections` on the publisher | ✅ |

Consequences baked into the design:

- **CoreBluetooth strips iBeacon manufacturer data on Apple platforms.** iBeacon on iOS/Catalyst/macOS
  *must* go through CoreLocation; there is no BLE-scan fallback. Eddystone is service data (0xFEAA),
  which CoreBluetooth passes through untouched, so it uses the BLE path on every platform including Apple.
- **tvOS gets nothing.** No `CLBeacon` binding means no iBeacon; the Eddystone-only surface that would
  remain is not worth a partial-API target that throws for half its members. tvOS is left off `TargetFrameworks`.
- **macOS monitoring is unavailable at the OS level**, so macOS falls back to the shared managed
  monitor for Eddystone and reports `PlatformNotSupportedException` for iBeacon region monitoring.
- **Service-data UUID formatting differs per platform** — Apple returns `"FEAA"`, Android
  `"0000feaa-0000-1000-8000-00805f9b34fb"`, Windows/Linux the long form. All Eddystone matching
  normalizes to the 16-bit short form first.

## Bugs being fixed

Carried over from the deleted module and the still-shipping `AdvertiseBeacon`:

1. **Broadcast endianness (shipping bug).** `Guid.ToByteArray()` is little-endian for the first three
   fields and `BitConverter.GetBytes(major)` is little-endian; iBeacon requires big-endian throughout.
   Android and Mac Catalyst currently broadcast a byte-swapped UUID *and* byte-swapped major/minor.
   Fixed with `Guid.TryWriteBytes(span, bigEndian: true, out _)` and `BinaryPrimitives.WriteUInt16BigEndian`.
2. **`CalculateProximity` was dimensionally meaningless** — `Math.Pow(10, (txpower - rssi) / 20)`
   compared against `6E-6`/`0.5E-6`. Replaced with thresholds on the estimated distance
   (`< 0.5 m` immediate, `< 3 m` near, else far — Apple's own boundaries).
3. **No RSSI filtering at all.** Every advertisement produced an independent distance, so readings
   jumped metres apart packet to packet. Added a windowed filter that trims outliers before averaging.
4. **First sighting never fired an entry event.** `state.IsInRange ??= true;` followed by
   `if (!state.IsInRange.Value)` meant a region entered from cold was silently swallowed.
5. **Duplicate-key crash on monitor restart.** `StartScan` did `states.Add(...)` over a dictionary it
   never cleared, and the repository `Add` branch added again on top of that.
6. **Major = 0 was rejected as invalid.** `Check.Assert` threw on `major < 1`; zero is a legal value.
7. **Company ID was never checked when parsing.** Any manufacturer payload beginning `0x02 0x15` was
   treated as an iBeacon regardless of whether it came from company `0x004C`.
8. **Wrong Android foreground service type.** `TypeLocation` for a BLE scan; Android 14+ wants
   `TypeConnectedDevice`.
9. **Ranging used `ScanMode.Balanced`.** Ranging wants `LowLatency`; monitoring wants `LowPower`.

## Package layout

```
src/Shiny.Beacons/          net10.0 + android + ios + maccatalyst + macos + windows
src/Shiny.Beacons.Blazor/   net10.0 (Microsoft.NET.Sdk.Razor)
tests/Shiny.Beacons.Tests/  net10.0
```

The `net10.0` target carries the whole shared engine, so a Linux host that registers
`Shiny.BluetoothLE.Linux`'s `IBleManager` gets beacons with no extra package — same as any other
plain-.NET host.

## Architecture

```
                          ┌─────────────────────────────┐
                          │  shared managed engine       │  net10.0, no platform code, unit tested
                          │  IBeaconPacket.Parse         │
                          │  EddystoneParser             │
                          │  RssiFilter (windowed trim)  │
                          │  IBeaconDistanceEstimator    │
                          │  BeaconRegionMonitor (FSM)   │
                          └──────────────┬──────────────┘
                                         │ IBleManager
        ┌──────────────┬─────────────────┼──────────────┬──────────────┐
     Android        Windows            Linux          Blazor        macOS(Eddystone)
        │
        └─ + ShinyBeaconMonitoringService (TypeConnectedDevice) for background

     iOS / Mac Catalyst / macOS  ── iBeacon only ──▶ CoreLocation
        ranging:    CLLocationManager.StartRangingBeacons(CLBeaconIdentityConstraint)
        monitoring: CLMonitor + CLBeaconIdentityCondition   (iOS/Catalyst 18+)
                    CLLocationManager.StartMonitoring(CLBeaconRegion)  (below 18)
                    macOS: not supported by the OS
```

## Public API

```csharp
namespace Shiny.Beacons;

public enum Proximity { Unknown, Immediate, Near, Far }
public enum BeaconRegionState { Unknown, Entered, Exited }

public record Beacon(
    Guid Uuid, ushort Major, ushort Minor,
    int Rssi, Proximity Proximity, double Distance,
    sbyte? TxPower, DateTimeOffset Timestamp
);

public record BeaconRegion(
    string Identifier, Guid Uuid, ushort? Major = null, ushort? Minor = null,
    bool NotifyOnEntry = true, bool NotifyOnExit = true
) : IRepositoryEntity;

public interface IBeaconRangingManager {
    AccessState CurrentStatus { get; }
    Task<AccessState> RequestAccess();
    IObservable<Beacon> WhenBeaconRanged(BeaconRegion region);
}

public interface IBeaconMonitoringManager {
    AccessState CurrentStatus { get; }
    Task<AccessState> RequestAccess();
    IList<BeaconRegion> GetMonitoredRegions();
    Task StartMonitoring(BeaconRegion region);
    Task StopMonitoring(string identifier);
    Task StopAllMonitoring();
    Task<BeaconRegionState> RequestState(BeaconRegion region, CancellationToken ct = default);
}

public interface IBeaconMonitorDelegate {
    Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region);
}

// Eddystone — BLE service data 0xFEAA, works on every platform incl. Apple
public interface IEddystoneScanner {
    Task<AccessState> RequestAccess();
    IObservable<EddystoneFrame> WhenFrameReceived();
}

public abstract record EddystoneFrame(string PeripheralId, int Rssi, DateTimeOffset Timestamp);
public sealed record EddystoneUidFrame(..., EddystoneUid Uid, sbyte TxPower, double Distance);
public sealed record EddystoneUrlFrame(..., string Url, sbyte TxPower, double Distance);
public sealed record EddystoneTlmFrame(..., double BatteryVolts, double? TemperatureCelsius,
                                       uint AdvertisementCount, TimeSpan Uptime);
public sealed record EddystoneEidFrame(..., byte[] EphemeralId, sbyte TxPower, double Distance);

public interface IBeaconBroadcaster {
    bool IsBroadcasting { get; }
    Task<AccessState> RequestAccess();
    Task StartIBeacon(Guid uuid, ushort major, ushort minor, sbyte? txPower = null);
    Task StartEddystoneUid(EddystoneUid uid, sbyte? txPower = null);
    Task StartEddystoneUrl(string url, sbyte? txPower = null);
    void Stop();
}

// DI
services.AddBeaconRanging();
services.AddBeaconMonitoring<TDelegate>();
services.AddEddystoneScanning();
services.AddBeaconBroadcasting();
```

## Distance estimation

`IBeaconDistanceEstimator` with two shipped implementations, selectable through
`BeaconRangingOptions`:

- **`PathLossDistanceEstimator` (default)** — `d = 10^((txPower − rssi) / (10 · n))`, `n` configurable
  (default `2.0`). Predictable, no magic constants, works the same on every platform.
- **`CurveFitDistanceEstimator`** — the Android/Radius-Networks empirical fit
  (`0.89976 · ratio^7.7095 + 0.111`), kept for parity with the wider Android beacon ecosystem.

Both are fed **filtered** RSSI, never raw: `RssiFilter` keeps a time window (default 20 s), discards
the top and bottom 10 % of samples, and averages the rest — the AltBeacon `RunningAverageRssiFilter`
approach, which is what actually kills the packet-to-packet jitter. On Apple, CoreLocation already
does its own filtering and hands us `CLBeacon.Accuracy` directly, so the filter is bypassed there.

## Work breakdown

1. Core abstractions + shared engine (net10.0, unit tested)
2. Shared `IBleManager` scan backend — ranging, monitoring FSM, Eddystone
3. Apple — CoreLocation ranging, `CLMonitor`/`CLLocationManager` monitoring, macOS carve-out
4. Android — foreground service, permissions, scan-mode tuning
5. Broadcasting — endianness fixes, Eddystone builders, Windows + Linux implementations
6. Blazor — extend `Shiny.BluetoothLE.Blazor` JS interop for manufacturer/service data, then `Shiny.Beacons.Blazor`
7. Tests, sample pages, readme, `skills/shiny-beacons`, docs site page + release notes
