# Device tests (E2E) - progress

Status as of 2026-10-14 15:45. **Runs are stopped:** the runner is killed and the test app is closed on the
Android emulator and the iOS simulator. Nothing is committed.

## What exists

| Path | What |
|---|---|
| `tests/Shiny.DeviceTests/` | MAUI test app (iOS, Android, Mac Catalyst). `Framework/` holds the runner, `EventHub`, `HostClient` and the DevFlow extension; `Tests/<Module>/` holds one folder per module. |
| `tests/Shiny.DeviceTests.Runner/` | Host console app. Drives runs over the DevFlow agent extension (`org.shiny.devicetests`), pre-grants permissions, moves the simulated location (simctl / `adb emu geo fix`), sends `simctl push`, relays BLE peer commands, and serves the fixtures (HTTP transfers, data sync, a fake 9100 printer). Writes JUnit. |
| `tests/Shiny.DeviceTests.Shared/Contracts.cs` | Wire contracts linked into both. |
| `tests/Shiny.DeviceTests/run.sh` | Build, install and run per platform. |
| `tests/Shiny.DeviceTests/readme.md` | How it works, how to run it, how to write a test. |

- Both projects are in `Shiny.slnx` but not in `Build.slnf`, so CI is unaffected.
- `Directory.Packages.props` gains `xunit.assert`.
- `CLAUDE.md` has a "Device tests" section covering the traps below.

**Suites (140 tests):** Core, BleLocal, BleCentral, BleHosting, Calendar, Contacts, Discovery, NetworkPrinters,
HttpTransfers, DataSync, Gps, Geofencing, Notifications, Push, Jobs, LiveActivities.

Out of scope by agreement: Wi-Fi, Screen Recorder, IAP client, Game Center, Wearables, Gamepad, App Functions, OS
print dialogs, tapping notifications, BLE pairing, and OS-scheduled background timing.

## Results

### iOS simulator (iPhone Air `7517383F-F29F-4A2F-98BA-06A98E1B0205`, dedicated)

Last full run: **95 passed, 13 failed, 32 skipped** (140/140). Since then Live Activities `StartUpdateEnd` passes
and `Push.SilentPush` skips (simulator limit). The remaining 12 failures:

- Calendar `Event_CreateReadDelete`: availability. Library bug (below).
- DataSync, 3 tests. Library bugs.
- Geofencing, 7 tests:
  - `StartMonitoring_SameIdentifier` is a library bug.
  - `StopAllMonitoring_ClearsEveryRegion` fails only because that one leaves a region behind.
  - The other 5 are the unexplained access/state problem (open question 1).
- Gps `CurrentListener_PreservesRequestOptions`. Library bug.

BLE skips throughout, because the simulator has no Bluetooth.

### Android emulator (Pixel_9a, API 37, Play Store image)

The re-run was **stopped at 86/140**: 55 passed, 10 failed, 21 skipped. HttpTransfers, Jobs, LiveActivities,
NetworkPrinters, Notifications and Push had not run yet in this pass.

- Core, Calendar (all 13, including attendees and all-day), Discovery (6/6) and BleLocal pass.
- Contacts `Query_FiltersSortAndPage`: library bug (case-sensitive `Equals`).
- DataSync, 3 tests: library bugs (below).
- Geofencing, 4 tests time out. Inconclusive: the state stays `Exited` after the location moves inside. Emulator
  geofence delivery may simply be too slow - check on a phone.
- Gps:
  - `CurrentListener_PreservesRequestOptions` is a library bug.
  - `Listener_DeliversSimulatedPositions_InOrder` got no reading in 45s. **Not triaged.**

The first Android run, before the `ACCESS_LOCAL_NETWORK` grant, also showed `Notifications.Send_Immediate_IsDelivered`
failing. That is a library bug (below).

### BLE loop

**Not run yet.** It needs the iPhone 15 Pro (`00008130-000E243E3AD1001C`) as the device under test and the Mac
Catalyst app as the peer, plus the iPhone's Wi-Fi IP:

```bash
dotnet build tests/Shiny.DeviceTests -f net10.0-maccatalyst -t:Run &
tests/Shiny.DeviceTests/run.sh ios --device 00008130-000E243E3AD1001C --agent http://<iphone-ip>:9301 --peer maccatalyst --filter Ble
```

The Mac Catalyst build compiles but has never been launched.

## Library bugs found (none fixed - `src/` untouched)

Each is pinned by a failing test.

| # | Area | Bug | Seen on | Where |
|---|---|---|---|---|
| 1 | DataSync | Apple never increments `Attempts`: a persistent 5xx retries forever (88 attempts) and `OnError` never fires | iOS | `DataSyncManager.Events` `OnError` / `RetryOperation` |
| 2 | DataSync | Android never retries a transient failure: after one 503 the op stays queued, with no resend and no `OnError` | Android | Android sync path |
| 3 | DataSync | A tombstone delete carries no identifier (`Entity=null`, `RawPayload="null"`), so delegates can't tell what was deleted | iOS, Android | `SyncInboxProcessor.DispatchTombstone`, Apple `DispatchTombstones` |
| 4 | DataSync | Apple `KeepLocal` / `ClientWins` sets the op to Pending and never resends it | iOS | `DataSyncManager` `OnConflict` |
| 5 | Gps | `RequestPreciseAccuracy` (and `AutoRestart`) are dropped when the request is converted | iOS, Android | `AppleGpsRequestExtensions.ToApple()`; `new AndroidGpsRequest(request.BackgroundMode)` in `AbstractGpsManager.StartListenerInternal` |
| 6 | Geofencing | `StartMonitoring` with an existing identifier throws "already has a record" instead of replacing the region | iOS | `Platforms/Apple/GeofenceManager.cs:169` (`repository.Insert`) |
| 7 | Calendar | Availability is never mapped on Apple: every event reads back `Busy` | iOS | Apple calendar impl (only Windows and Android map it) |
| 8 | Contacts | `Equals` is case-sensitive on Android (SQL `=`) but case-insensitive on Apple and in the in-memory pass | Android | `Platforms/Android/ContactStoreImpl.cs` `GetLikeArgs` |
| 9 | Notifications | `Cancel(id)` doesn't dismiss a displayed immediate notification on Android, because only repository-stored (scheduled) ones are cancelled | Android | `Platforms/Android/NotificationManager.cs:38` |

### Suspected from reading source, not yet confirmed by a run

These come from the agents' source reading; BLE ones need the loop run.

- **BLE Hosting:**
  - `WriteOptions` / `NotificationOptions` are `[Flags]` enums with sequential values (Write=0, Notify=0 ...), so flag checks are wrong.
  - Apple `OnWrite` uses `Enum.Parse<CBATTError>(status.ToString())`, which throws for `Failure` / `InsufficientResources` inside `async void`. It also responds per request rather than per batch.
  - Apple `OnRead` maps every failure to `InsufficientEncryption`.
  - Android advertising never sets `SetIncludeDeviceName` and renames the adapter instead.
  - Android `IPeripheral.Context` is not kept per central.
- **Jobs:** `JobLifecycleTask.HasPowerLevel` compares `battery.Level > 20`, but the level is 0.0-1.0, so `BatteryNotLow` foreground jobs never run on battery.
- **Push:**
  - Android `AddPush()` registers `PushManager` twice.
  - iOS simulator access is hard-coded to `NotSupported`.
- **Notifications:**
  - Unknown channel throws `InvalidOperationException` on Apple but `InvalidProgramException` on Android.
  - `GetNotification` nullability mismatch.
- **Calendar (Android):**
  - Reads don't filter `DELETED=1` rows.
  - Android `Update` / `Delete` of an unknown id is a silent no-op (Apple throws).
  - Android all-day event writes were a suspect, but `Event_AllDay` passes on the emulator.
- **HttpTransfers (Android):**
  - `Cancel` leaves the partial file.
  - An existing destination file is treated as a partial download and appended via `Range`.
  - Both should be checked when the Android run reaches HttpTransfers.
- **Stores:** the secure store failed a plain `string` round trip in the first Core-only run, but passes once other modules register JSON contexts. Its behaviour depends on what else is registered.

## Open questions

1. **iOS simulator geofencing:** `GeofenceManager.CurrentStatus` / `RequestAccess` report `Unknown` despite `simctl privacy grant location-always`, and `RequestState` never leaves `Unknown`.
   - Setup logs are now attached to every result (`[setup] ...`), so the next `--filter Geofencing` run shows what `RequestAccess` returned.
   - Then check on the physical iPhone before treating it as a bug in the CLMonitor / `CLServiceSession` path.
2. **Android Gps `Listener_DeliversSimulatedPositions_InOrder`:** no reading within 45s. Not triaged; it may be the fused provider not taking emulator fixes while the app is in that state.
3. **Android geofencing** on the emulator vs a real phone.

## Environment traps (also in CLAUDE.md)

- **The Simulator UI app isn't available on this Mac, so simulators are headless.**
  - DevFlow `ui alert` can't see system prompts.
  - Notifications therefore use iOS **provisional** authorization.
  - `simctl privacy grant` kills the app; the runner relaunches it.
- **Share nothing with other work.** A second session's app took the foreground on iPhone 18 Pro and suspended the test app. Use the dedicated iPhone Air simulator (`--device 7517383F-...`). The runner now tolerates 5 minutes of agent silence.
- **Android 17 (API 37)** needs `ACCESS_LOCAL_NETWORK` granted to reach the runner at `10.0.2.2` and for multicast. The runner now grants it.
- **`simctl push`** rejects silent pushes (UNErrorDomain 1401).
- **Stale iOS `Info.plist`.** Incremental iOS builds can leave it stale. After editing it, delete `obj/` and `bin/` `Debug/net10.0-ios`.
- **DevFlow agent `Content-Length`.** The agent's HTTP server needs a `Content-Length`, so send buffered `StringContent`.
- **Mac Catalyst minimum.** The Mac Catalyst SDK needs `SupportedOSPlatformVersion` 17.0 or higher.
- **Pinned DevFlow version.** The repo pins `Microsoft.Maui.DevFlow.Agent` preview.12. It has no mutation lease and no request cancellation token. Preview.13 adds both; check `DevFlowTestExtension` if the pin moves.

## Next steps

1. Re-run Android for the suites that didn't run (`--filter HttpTransfers,Jobs,LiveActivities,NetworkPrinters,Notifications,Push`), then triage Gps open question 2.
2. Run `--filter Geofencing` on iOS with setup logs, then check on the iPhone.
3. Run the BLE loop (iPhone + Mac Catalyst peer), then confirm or clear the BLE suspects.
4. Decide which library bugs to fix. Each fix needs readme, skill, docs page and release note per CLAUDE.md. The pinning test should flip to passing.
5. Optional runner additions the agents asked for:
   - `/env/location/clear`
   - GPX route playback (`maui apple simulator location run`)
   - a first-class "peer skipped" outcome
   - a cancellation token through peer commands
