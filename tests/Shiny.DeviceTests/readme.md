# Shiny Device Tests

End-to-end tests that exercise the real platform implementations inside a running app on a simulator, emulator,
Mac or phone. They complement the `net10.0` unit tests in `tests/`, which never touch CoreBluetooth, CoreLocation,
EventKit, NsdManager and so on.

| Project | What it is |
|---|---|
| `tests/Shiny.DeviceTests` | MAUI app (iOS, Android, Mac Catalyst). Holds the suites and runs them in-process. |
| `tests/Shiny.DeviceTests.Runner` | Console app on the host. Starts runs, streams results, writes JUnit, and acts on the environment for the tests. |
| `tests/Shiny.DeviceTests.Shared` | Wire contracts linked into both. |

Neither project is in `Build.slnf`, so CI does not build them.

## How it fits together

```
 Runner (host)                                         Shiny.DeviceTests (device)
 ─────────────                                         ──────────────────────────
 POST /api/v1/ext/org.shiny.devicetests/run  ───────▶  DevFlow agent extension ─▶ TestRunner ─▶ suites
 GET  .../status (poll, print, JUnit)        ◀───────
                                                        suites call back to the runner:
 /env/location  (simctl location / adb emu geo) ◀─────  ctx.Host.SetLocationAsync
 /env/push      (xcrun simctl push)             ◀─────  ctx.Host.SendPushAsync
 /env/peer      ─▶ peer device's DevFlow agent  ◀─────  ctx.Host.PeerAsync   (BLE loop)
 fixtures: transfers, data sync, :9100 printer  ◀─────  HttpClient / Shiny APIs
```

- The **MAUI DevFlow agent** (`Microsoft.Maui.DevFlow.Agent`) is the control channel. The app registers a DevFlow
  extension (`Framework/DevFlowTestExtension.cs`), so the same routes also work from `maui devflow extensions call`
  or the DevFlow MCP server.
- Each head listens on its own agent port, so the iOS simulator and the Mac Catalyst app can run side by side:
  iOS 9301, Mac Catalyst 9302, Android 9303 (forwarded with `adb forward`).
- **Location** is pushed by the runner with `xcrun simctl location set` (iOS simulator) and `adb emu geo fix`
  (Android emulator). The DevFlow agent can only *read* the location.
- **Permissions** are pre-granted (`simctl privacy grant`, `adb shell pm grant`). Prompts that can't be pre-granted
  (notifications, Bluetooth) are accepted by an alert watcher that calls `maui devflow ui alert dismiss`. On the iOS
  simulator, `simctl privacy grant` kills the app, so the runner relaunches it.
- Each test declares what it needs. When the environment can't provide it, the test is **skipped** with the reason,
  not failed. Examples: `ctx.RequireCapability(HostCapability.Location)`, `ctx.RequireRealDevice(...)`.

## Running

Prerequisites: the `maui` CLI (`dotnet tool install -g Microsoft.Maui.Cli --prerelease`), a booted simulator or
emulator (or a connected device), and the MAUI workloads.

```bash
# build, install, launch and run everything on the booted iOS simulator
tests/Shiny.DeviceTests/run.sh ios

# a subset, with JUnit output
tests/Shiny.DeviceTests/run.sh android --filter Gps,Geofencing --junit artifacts/device-tests/android.xml

# runner only (the app is already installed and running)
dotnet run --project tests/Shiny.DeviceTests.Runner -- --platform ios --filter Core -v
dotnet run --project tests/Shiny.DeviceTests.Runner -- --platform ios --list
```

`--filter` is a comma-separated, case-insensitive prefix match on `Suite` or `Suite.Test`.
`dotnet run --project tests/Shiny.DeviceTests.Runner -- --help` lists every option.

### The BLE loop (two devices)

A device can't scan or connect to its own advertisement, and the iOS simulator has no Bluetooth. The BLE suites
therefore run with a **peer**, a second copy of the app that hosts or scans on request. The runner relays peer
commands from the device under test to the peer's DevFlow agent.

```bash
# 1. the peer: Mac Catalyst on this Mac (agent on :9302)
dotnet build tests/Shiny.DeviceTests -f net10.0-maccatalyst -t:Run &

# 2. the device under test: a physical iPhone (find its Wi-Fi IP in Settings)
tests/Shiny.DeviceTests/run.sh ios --device <iphone-udid> --agent http://<iphone-ip>:9301 --peer maccatalyst --filter Ble
```

Any two of iPhone, Android phone and Mac Catalyst work. An Android phone needs `--peer-device <serial>` when it is
the peer.

### Running by hand

`run.sh <platform> --serve` (or the runner with `--serve`) hosts only the fixtures. Pressing **Run All** in the app
then runs with no runner attached, and environment-driven tests skip. `maui devflow ui screenshot` shows the live
results page.

## Writing a test

Each module has its own folder under `Tests/<Module>/` holding:

- the suites (`DeviceTestSuite` subclasses)
- test delegates that publish to `EventHub`
- peer commands
- an `Add<Module>DeviceTests()` registration, which `Tests/DeviceTestModules.cs` calls

```csharp
public class MyTests(IMyShinyService service) : DeviceTestSuite
{
    public override string Name => "MyModule";

    protected override void Define()
    {
        this.Test("Thing_Works", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);    // skip when the runner can't move the location
            await ctx.Host.SetLocationAsync(43.6532, -79.3832);
            var reading = await ctx.Events.WaitAsync<MyReading>("my.topic", TimeSpan.FromSeconds(30), ctx.CancellationToken);
            Assert.Equal(43.6532, reading.Latitude, 3);
        }, timeout: TimeSpan.FromMinutes(2));
    }
}
```

Clean up whatever a test creates (events, contacts, regions, pending notifications) in `finally` or
`TeardownAsync`. A leftover poisons the next run on the same simulator.

## Not covered on purpose

These are left out because they need system UI, paid accounts or hardware that a test run can't drive:

- Wi-Fi, Screen Recorder, In-App Purchases (client), Game Center, Wearables, Gamepad, App Functions, and
  OS print dialogs.
- Tapping a delivered notification, BLE pairing dialogs, and OS-scheduled background timing (BGTask windows,
  geofence latency on real devices).
