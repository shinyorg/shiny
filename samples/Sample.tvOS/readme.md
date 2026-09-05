# Shiny tvOS Sample

A plain UIKit tvOS app exercising every Shiny module that has a `net10.0-tvos` target. There is no
MAUI on tvOS, so this hosts through `Shiny.Hosting.Native` - one `ShinyAppDelegate` subclass and a
`UITabBarController`, nothing more.

## Running it

```bash
dotnet build samples/Sample.tvOS/Sample.tvOS.csproj -p:RuntimeIdentifier=tvossimulator-arm64

xcrun simctl boot "Apple TV"
xcrun simctl install booted samples/Sample.tvOS/bin/Debug/net10.0-tvos/tvossimulator-arm64/Sample.tvOS.app
xcrun simctl launch booted org.shiny.sample.tvos
```

Needs the `tvos` .NET workload installed.

## What each tab shows

| Tab | Module | What it demonstrates |
|---|---|---|
| Status | `Shiny.Core` | `IPlatform` resolves to `IosPlatform` - tvOS reuses the iOS platform layer. `IBattery` reports permanently Full |
| BLE | `Shiny.BluetoothLE` | Permission, scan, connected peripherals. Central role only |
| mDNS | `Shiny.Net.Discovery` | Browse `_http._tcp` over Bonjour, and advertise the Apple TV as a service |
| Jobs | `Shiny.Jobs` | Registered jobs and forcing a run. `BGTaskScheduler` schedules them on a real device |
| HTTP | `Shiny.Net.Http` | Queue a background download and watch progress through `UpdateReceived` |
| Push | `Shiny.Push` | APNs registration and the app icon badge - the whole of tvOS notification UI |
| Record | `Shiny.ScreenRecorder` | ReplayKit capture of this app's own UI, with capabilities printed |
| Sync | `Shiny.Data.Sync` | Queue a "viewing" into the outbox and watch it drain |

## What is deliberately missing

These have no tvOS target because Apple gives tvOS no API to build them on, so referencing them here
would not compile:

- **`Shiny.BluetoothLE.Hosting`** - `CBMutableService` and `CBMutableCharacteristic` have no
  constructors on tvOS. An Apple TV cannot be a GATT peripheral.
- **`Shiny.Net.Wifi`** - no `NEHotspotConfiguration` or `NEHotspotNetwork`.
- **`Shiny.Locations`** - no `CLMonitor`, so no geofencing.
- **`Shiny.Notifications`** - a tvOS notification can only change the app icon badge.

## tvOS gotchas the sample points at

- **No background Bluetooth.** tvOS has no `bluetooth-central` background mode, so the BLE tab stops
  scanning when the app suspends and `RestoreIdentifier` buys you nothing.
- **Push is silent-only.** `IPushDelegate.OnEntry` never fires - nothing on tvOS can be tapped.
  `RequestAccess()` asks for `Badge` alone.
- **No microphone.** `ScreenRecorderCapabilities.Microphone` is not advertised, so a recording
  request with `IncludeMicrophone` is rejected by validation.
- **Storage is evictable.** Downloads and recordings go to the cache directory; an Apple TV app
  container is small and the OS may reclaim it between launches.
- **Background tasks need a real device.** `BGTaskScheduler` never fires on the simulator, exactly
  as on iOS. Use the Jobs tab's "Run now" there.
