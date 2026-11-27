# Plan: Bug-scan follow-ups

Status: **implemented except 14**. Done (device checks pending): 1 (option B), 2, 3 (option B), 4 (option B), 5–13.
Deferred: 14 (Windows toast activation), at the user's request.
Last updated: 2026-11-26

## Summary

The November 2026 bug scan (everything except BluetoothLE and `samples/`) fixed ~125 issues in the working tree.
These are the 14 items it skipped or only half fixed, each with a concrete fix. Four need a decision first. Each
has a recommendation, so the work can start as soon as the recommendations are accepted or changed.

| # | Item | Module | Kind | Size |
|---|---|---|---|---|
| 1 | `OnUnRegistered` when push permission is revoked | Push (Apple) | **Decision** | S |
| 2 | Create → Delete coalescing | Data Sync | **Decision** | S |
| 3 | Consumed Google consumable verifies as active | In-App Purchases Server | **Decision** | S |
| 4 | StoreKit transactions that fail verification are never finished | In-App Purchases (Apple) | **Decision** | M |
| 5 | Linux VPN watchers not re-subscribed after a reconnect | VPN (Linux) | Fix | S |
| 6 | mDNS SRV/TXT records never expire | Network Discovery | Fix | S |
| 7 | Game Center percent → steps rounding | Game Center (Apple) | Fix + device check | S |
| 8 | Foreground jobs run once on a background cold start | Jobs | Fix | S |
| 9 | Apple Data Sync completion handler called before delegates finish | Data Sync (Apple) | Fix | S |
| 10 | Apple Data Sync ops stranded by the old code | Data Sync (Apple) | Fix | S |
| 11 | Android ≤ 13 mDNS resolve that times out blocks later resolves | Network Discovery (Android) | Fix | M |
| 12 | PC/SC tag dropped from the scan queue stays dead | NFC (PC/SC) | Fix | M |
| 13 | HTTP download resume has no ETag / `If-Range` check | HTTP Transfers | Fix | M |
| 14 | Windows notification taps never reach `OnEntry` | Notifications + Push (Windows) | Feature | L |

Order of work: phase 1 is the no-decision fixes (5–10). Phase 2 is the decisions (1–4) once they're answered. Phase 3
is the medium/large items (11–14), each landing on its own. Every item ships with the usual `CLAUDE.md` set: tests
where the logic runs on `net11.0`, the module skill, the docs feature page, and a release note under the current
`### <version> - TBD` heading.

## Decisions

### 1. Apple push: `OnUnRegistered` when permission is revoked

**Today:** `PushManager.Start()` (`src/Shiny.Push/Platforms/Apple/PushManager.cs:72`) re-checks access when a token
is stored. If the user has turned notifications off, it only logs `"User has removed push notification access"`. The
token stays stored and `IPushDelegate.OnUnRegistered` never fires, though its docs promise it does.

| Option | Effect |
|---|---|
| A. Keep as is, fix the docs | No behavior change. Apps never learn about a revoke unless they poll `GetCurrentAccess()` |
| **B. Raise `OnUnRegistered(token)` and clear the token (recommended)** | Matches the documented contract and lets the server stop targeting the device. Next `RequestAccess()` that succeeds re-registers and raises `OnNewToken` |
| C. Raise `OnUnRegistered` but keep the token | Server is told; a later grant re-uses the token without a new registration. Leaves `RegistrationToken` set while the app is unregistered, which is confusing |

**Plan (B):**
- In `Start()`'s continuation: if `x.Result.Status` is `Denied` (not `Restricted`, not timed out), take the stored token, clear `RegistrationToken` / `NativeRegistrationToken`, then run `OnUnRegistered(token)` through `RunDelegates`.
- Do the same on macOS (`Platforms/MacOS/PushManager.cs`).
- Android: check what `Start()` does with `NotificationManagerCompat.AreNotificationsEnabled()` and match it. Android 13+ treats `POST_NOTIFICATIONS` being revoked the same way.
- Azure Notification Hubs provider: `UnRegister` must handle being called from `Start()` without a UI context.
- Docs and skill: say a revoke is detected at the next app start, not live.

### 2. Data Sync: Create followed by Delete

**Today:** `OperationCoalescer.Merge` (`src/Shiny.Data.Sync/Infrastructure/OperationCoalescer.cs:44`) turns Create + Delete
into a single DELETE. If the Create never reached the server, that DELETE gets a 404 and ends in a permanent `OnError`.

The scan skipped this because "the attempt count can't prove the Create never reached the server after a timeout".
That is only true once an attempt was made. **`Attempts == 0` does prove it was never sent.**

**Recommended rule:**
- Create (`Attempts == 0`, `State == Pending`) + Delete → **drop both**. Nothing was ever sent.
- Create (`Attempts > 0`) + Delete → keep the DELETE, and **treat 404 / 410 on a DELETE as success** in `HttpClientDataSyncProcess` and the Apple path. The entity is gone either way, which is what a delete wants.
- The 404/410 rule is worth having for every DELETE, not only coalesced ones, because a retried DELETE whose first try succeeded gets the same 404. Make it a `SyncEndpoint` option (`TreatMissingAsDeleted`, default `true`) so an API that uses 404 for something else can turn it off.

**Tests:** coalescer unit tests for both branches; a process test where the DELETE returns 404 and the op completes.

### 3. Server: consumed Google consumable still verifies as active

**Today:** `PurchaseVerifier` (`src/Shiny.Mobile.InAppPurchases.Server/PurchaseVerifier.cs:215`) sets
`IsActive = PurchaseState == "PURCHASED"` and ignores `ProductOfferDetails.ConsumptionState`, which it already parses.
Apple consumables behave the same way: a finished consumable still verifies.

| Option | Effect |
|---|---|
| A. `IsActive = false` once consumed | Replay-safe by default, but breaks apps that verify *after* finishing |
| **B. Add `bool? IsConsumed` to `VerifiedPurchase` and leave `IsActive` alone (recommended)** | Additive, so nothing breaks. Apps that grant on verify can refuse consumed tokens; set from `ConsumptionState` on Google and left `null` on Apple, which doesn't report it |

**Plan (B):**
- Add `IsConsumed`, filled for Google one-time products.
- Add a "replay protection" section to the skill and `client/inapppurchases/server.mdx`: de-duplicate grants by `TransactionId` (Apple) / `OrderId` (Google), and check `IsConsumed` on Google.
- Test with a recorded Google response that has `CONSUMPTION_STATE_CONSUMED`.

### 4. StoreKit: transactions that fail verification

**Today:** `native/ShinyStoreKit/Sources/ShinyStoreKit.swift` skips `.unverified` results in `currentEntitlements`,
`unfinished` and `updates` (lines 135, 153, 230), and a purchase that fails verification throws. `shiny_storekit_finish`
can finish unverified transactions (line 172), but C# never gets a `Purchase` to pass it. They are redelivered and
skipped on every launch.

| Option | Effect |
|---|---|
| A. Finish unverified transactions automatically in Swift | Clears them, but breaks the module's "nothing auto-finishes" rule (`CLAUDE.md`) |
| **B. Surface them with `Purchase.IsVerified = false` (recommended)** | Nothing auto-finishes. The app (or its server) decides; `FinishPurchaseAsync` already works for them |
| C. Leave as is | Unverified transactions pile up forever |

**Plan (B):**
- Swift: for `.unverified(let tx, let error)`, emit the transaction with `"verified": false` and the error text, instead of skipping it. That covers `purchase`, `unfinished`, `updates` and `currentEntitlements`. A purchase that fails verification resolves as success with an unverified purchase, not a throw.
- C#: add `bool IsVerified = true` (last optional parameter) and `string? VerificationError` to `Purchase`. Android always reports `true`; Play Billing verification is server-side. `IPurchaseDelegate` gets unverified purchases like any other.
- Skill and docs: an unverified purchase must **not** be granted; finish it (consume or not) after logging, or send it to the server, which will reject the JWS.
- Rebuild the xcframework with `native/ShinyStoreKit/build.sh`. Verify in a StoreKit configuration file with "Fail transaction verification" enabled.

## Phase 1 — fixes with no decision

### 5. Linux VPN: re-subscribe watchers after a reconnect

**Today:** `NmVpnClient` forgets the connection once `DisconnectedAsync()` completes (`NmVpnClient.cs:76`), so calls
reconnect. But the two watches `LinuxVpnManager.StartWatching` sets up (`LinuxVpnManager.cs:213-214`) die with the old
connection, and `StatusChanged` stays silent until the last subscriber leaves and a new one arrives.

**Plan:**
- Expose a `ConnectionLost` notification from `NmVpnClient`: a `Task` that completes with the `DisconnectedAsync` reason, replaced on every new connection.
- In `LinuxVpnManager`, when it completes and there are still subscribers, dispose the dead watch handles and call `StartWatching` again. Back off 1 s → 30 s while the bus is unavailable, cancelled by `StopListening`.
- After re-subscribing, raise the current status once, since a change may have been missed while disconnected.
- The D-Bus rules apply to the new handlers: `IsCompletion` first, never throw.
- Verify on a Pi: `systemctl restart NetworkManager` while subscribed, then bring a VPN up and down.

### 6. mDNS: expire SRV / TXT records by TTL

**Today:** `MdnsBrowseSession` (`src/Shiny.Net.Discovery/Managed/Mdns/MdnsBrowseSession.cs:41-42`) caches
`srvRecords` / `txtRecords` with no expiry. Only a goodbye or the instance's PTR expiring removes them. Addresses already
store an expiry (line 251).

**Plan:**
- Store `(record, expiresAt)` for SRV and TXT, refreshed on every re-announcement, with TTL 0 meaning goodbye (as today).
- `Sweep()` removes expired SRV/TXT entries. An instance whose SRV expired but whose PTR is still live is re-queried (one SRV/TXT question) rather than reported lost. RFC 6762 §5.2 says to refresh at 80% of the TTL.
- Tests with a fake `TimeProvider`: SRV expires → re-query sent; SRV refreshed → kept; goodbye → removed.

### 7. Game Center: percent → steps rounding

**Today:** `GameCenterManager.ToSteps` (`src/Shiny.GameCenter/Infrastructure/GameCenterManager.cs:427`) uses
`Math.Floor(percent * total / 100)`. If Game Center stores 33.0 for 1 of 3 steps, this gives 0. The next increment then
sends 1 step (33%), the service ignores it as no progress, and the increment is lost.

**Plan:**
- First, confirm on a device what `GKAchievement.PercentComplete` returns after reporting 33.333…: whether it is rounded, and to how many places. Add a temporary log in the sample, or check through the device tests.
- Replace the floor with the inverse of the formula Shiny writes with: the largest `s` such that `s * 100 / total ≤ percent + ε`, where ε is half the precision Apple stores (0.5 if it rounds to whole percent, 1e-6 if it keeps full precision). This never overstates by more than the rounding Apple applied, which the old floor didn't account for at all.
- Unit tests on `net11.0`: 33.0/3 → 1, 66.0/3 → 2, 99.0/100 → 99, 0.4/1000 → 4 (if rounding to whole percent, document the precision limit for large step counts).

### 8. Jobs: skip the foreground run on a background cold start

**Today:** `JobLifecycleTask.Start()` (`src/Shiny.Jobs/Infrastructure/JobLifecycleTask.cs:39`) always runs foreground
jobs straight away. When WorkManager or a `BGTask` launches the process, that runs them alongside the background run.
The scan said this can't be detected on iOS. It can: `UIApplication.SharedApplication.ApplicationState` is
`Background` during a background launch.

**Plan:**
- Add an internal `IsLaunchedInBackground()` per platform:
  - iOS / Mac Catalyst / tvOS: read `ApplicationState` on the main thread; `Background` means skip.
  - Android: `ActivityManager.GetMyMemoryState(info)`; importance above `Foreground` / `Visible` means skip.
- In `Start()`, only run straight away when not launched in the background. Otherwise wait for the first `OnForeground()`, which already restarts the timer.
- `net11.0`, Windows, Linux and macOS are unchanged; they have no background launch.
- Device tests: a `BGTask` launch on iOS (`e -l objc -- (void)[[BGTaskScheduler sharedScheduler] _simulateLaunchForTaskWithIdentifier:@"…"]`) and a WorkManager launch on Android, each counting how often the job ran.

### 9. Apple Data Sync: completion handler waits for delegates

**Today:** `DidFinishEventsForBackgroundSession` (`src/Shiny.Data.Sync/Platforms/Apple/DataSyncManager.Events.cs:131`)
calls `completionHandler` while async `IDataSyncDelegate` calls may still be running. iOS can suspend the app partway
through `OnSent` / `OnReceived`.

**Plan:**
- Port the HTTP Transfers fix from this scan: a pending-delegate counter, incremented around every delegate dispatch. `TryCompleteSession()` invokes the handler only when events are finished and the counter is 0. The handler is invoked once and then cleared.
- Share the counter helper with `Shiny.Net.Http` if it fits a common internal type. Otherwise copy it (the projects don't reference each other).
- Verify in the device-test runner with a delegate that awaits 2 s before returning.

### 10. Apple Data Sync: recover ops stranded by the old code

**Today:** after this scan, `Start()` re-sends ops with `NextAttemptAt != null` and no running task
(`DataSyncManager.cs:74`). Ops stranded by the *old* code have `NextAttemptAt == null`, so they are never picked up.

**Plan:**
- In `Start()`, after the existing pass: list the session's native tasks (`GetAllTasksAsync`), and for every op in `Pending` / `InProgress` with `NextAttemptAt == null` whose identifier has **no** native task, set `NextAttemptAt = now`. They are sent on the same pass.
- This doesn't guess: an op with no native task can't be in flight. It runs every start, not only once, so it also covers any future path that strands an op.
- Unit-test the selection logic by extracting it into a static helper that takes the ops and the task identifiers.

## Phase 3 — medium and large

### 11. Android ≤ 13: mDNS resolve that times out

**Today:** before API 34, `ResolveViaListener` (`src/Shiny.Net.Discovery/Platforms/Android/AndroidMdnsManager.cs:202`)
can't cancel a resolve (`stopServiceResolution` is API 34+). The legacy `NsdService` rejects further resolves with
`FAILURE_ALREADY_ACTIVE` while one is outstanding, so one service that never answers fails every later resolve.

**Plan:**
- Track the outstanding legacy resolve. When it times out, keep its listener alive so its eventual `OnServiceResolved` / `OnResolveFailed` clears the busy flag. Don't treat the timeout as freeing the slot.
- While the slot is busy (or on `FAILURE_ALREADY_ACTIVE`), **fall back to the managed mDNS stack** this package already has: send an SRV/TXT/A query for the instance through `ManagedMdnsManager` and return its result. On Android that needs a `WifiManager.MulticastLock` held for the query, and the `CHANGE_WIFI_MULTICAST_STATE` permission, which the package should declare.
- Device test on an Android 13 emulator: resolve a service that was unpublished mid-resolve, then resolve a live one; the second must succeed.

### 12. PC/SC: dropped tag stays dead

**Today:** the one-slot channel in `AbstractNfcManager` (`DropOldest`) disposes a dropped tag (`OnTagDropped`, line 67),
but `PcscNfcManager` still holds it in `present[reader]` (`PcscNfcManager.cs:220`). The tag isn't reported again until
it is removed and re-presented. Re-reporting naively can make two readers keep reconnecting in turn when the consumer
is slow.

**Plan:**
- Change the PC/SC contract so a dropped tag is **released, not disposed**: `OnTagDropped` tells the session for that reader, which closes the card handle and moves the reader to a `Dropped` state but keeps it out of `present`.
- On the next poll, a reader in `Dropped` whose card is still there is reported again only when the channel has room (`Writer.TryWrite` succeeds), never by evicting the other reader's tag. Two slow readers therefore alternate at the consumer's pace, without reconnect churn.
- Unit tests on `tests/Shiny.Nfc.Tests/FakePcscApi.cs` (as `PcscManagerTests` does): two readers, a slow consumer, assert each tag is delivered and no reconnect happens while the channel is full.

### 13. HTTP download resume: ETag / `If-Range`

**Today:** `HttpClientHttpTransferProcess` (`src/Shiny.Net.Http/Infrastructure/HttpClientHttpTransferProcess.cs:450`)
resumes when the partial file was written after `CreatedAt`. If the file changes on the server between attempts, the
resumed file is corrupt.

**Plan:**
- Store the validator in an **internal** side record (`TransferValidator { Identifier, ETag, LastModified }`) in the transfer repository, not on the public `HttpTransfer` record. That avoids another binary-breaking constructor change. Remove it with the transfer.
- First response: save a strong `ETag` (weak `W/` ones can't be used with ranges), or `Last-Modified` if there's no ETag.
- Resume: send `Range` plus `If-Range: <validator>`. A 206 continues; a 200 means it changed, so truncate and restart (the "server ignored Range" path already handles a 200). With no validator stored, keep today's behavior.
- Applies to the managed loop (Android, Windows, Linux, macOS, .NET). iOS uses `NSURLSession`, which handles its own resume; Blazor has no resume.
- Tests with the existing in-memory server: file changes between attempts → restart and correct bytes; unchanged → 206 continue; weak ETag → falls back to `Last-Modified`.

### 14. Windows: notification taps reach `OnEntry`

**Today:** `NotificationManager` (`src/Shiny.Notifications/Platforms/Windows/NotificationManager.cs`) sends with the WinRT
`ToastNotificationManager`, using a package AUMID or `AppDomain.FriendlyName` when unpackaged. Nothing handles
activation, so taps and action buttons never reach `INotificationDelegate.OnEntry`. Windows push has the same gap.

**Approach:** move activation to the **Windows App SDK `AppNotificationManager`** (`Microsoft.WindowsAppSDK` 2.5.1 is
already a central package). It registers the COM activator for packaged *and* unpackaged apps, and supports
`AppNotification` built from our existing toast XML. Scheduled toasts stay on `ScheduledToastNotification`, which is
why this starts with a spike.

**Steps:**
1. **Spike (go/no-go):** in `samples/` on a scratch branch, check that a toast shown by `AppNotificationManager.Default.Show` and a `ScheduledToastNotification` created under the same registered AUMID both activate through `AppNotificationManager.NotificationInvoked`. Test packaged and unpackaged, running and cold-started. If scheduled toasts can't activate, the fallback is to schedule through Shiny's own startup top-up and show them with `AppNotificationManager`. That loses delivery while the app isn't running, so it needs a decision.
2. Call `AppNotificationManager.Default.Register()` in `NotificationManager.Start()`, and subscribe to `NotificationInvoked`. Map the `arguments` (put the notification id and action id into the toast's `launch` / button `arguments`) to `OnEntry`. Text-input actions come from `UserInput`.
3. **Cold start:** the app's `OnLaunched` has to forward `AppInstance.GetCurrent().GetActivatedEventArgs()` when the kind is `AppNotification`. Add that to the Windows lifecycle hooks in `Shiny.Hosting.Maui` / `.Uno` / `.Avalonia` / `.Native` so apps don't write it by hand.
4. Push: route Windows push toast activation (`Platforms/Windows/PushManager.cs`) through the same handler into `IPushDelegate.OnEntry`.
5. Unregister on app exit is not needed; re-registering on every start is idempotent.
6. Trim/AOT: `AppNotificationManager` is a projection, but check `Build.slnf` stays at zero `IL2xxx`/`IL3xxx`.
7. Docs: a Windows section in `client/notifications/index.mdx` (packaged vs unpackaged, what the host does for you), and release notes for Local Notifications and Push Notifications.

## Verification matrix

| Item | Unit test (`net11.0`) | Device / OS run |
|---|---|---|
| 1 | — | iOS: revoke in Settings, relaunch, assert `OnUnRegistered` |
| 2 | ✅ coalescer + 404 handling | — |
| 3 | ✅ recorded Google response | — |
| 4 | — | StoreKit config with failed verification |
| 5 | — | Pi: restart NetworkManager while subscribed |
| 6 | ✅ fake `TimeProvider` | — |
| 7 | ✅ inverse formula | iOS: read back stored percent |
| 8 | — | iOS `BGTask` simulate launch, Android WorkManager launch |
| 9 | — | iOS background session with a slow delegate |
| 10 | ✅ selection helper | iOS: upgrade from a build with stranded ops |
| 11 | — | Android 13 emulator |
| 12 | ✅ fake PC/SC | real dual-reader setup if available |
| 13 | ✅ in-memory server | — |
| 14 | — | Windows packaged + unpackaged, running + cold start |
