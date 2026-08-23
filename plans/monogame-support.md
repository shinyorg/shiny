# Plan: Shiny in MonoGame

Status: **proposal** — nothing here is committed work.
Last updated: 2026-08-23

## Summary

MonoGame is the easy case. On Android and iOS a MonoGame app is a *real* .NET Android / .NET iOS
app — same TFMs, same bindings, same runtime as MAUI — so the libraries already work today. The
only gap is wiring, and it is four method calls. This is a documentation and sample effort, plus an
optional thin helper package.

Desktop (DesktopGL) is the weaker half, and it is weaker in a more interesting way than first
assumed — see the corrected matrix below.

## Why mobile already works

Verified in this repo:

- **Nothing is MAUI-coupled except the MAUI host.** Grepping `Microsoft.Maui` across `src/` hits
  exactly three files, all in `src/Shiny.Hosting.Maui/`. Everything else binds directly to
  AndroidX / UIKit / WinRT.
- **`Shiny.Hosting.Native` exists for precisely this case** — a non-MAUI native app. It ships
  `net10.0`, `-android`, `-ios`, `-maccatalyst`.
- **Activity tracking is base-class agnostic.** `AndroidActivityLifecycle` hooks
  `Application.RegisterActivityLifecycleCallbacks`
  (`src/Shiny.Core/Platforms/Android/AndroidActivityLifecycle.cs:16`), so `CurrentActivity`
  resolves a `AndroidGameActivity` automatically.
- **Notification taps do not assume a `MainActivity`.** `AndroidNotificationManager` uses
  `GetLaunchIntentForPackage` (`src/Shiny.Notifications/Platforms/Android/AndroidNotificationManager.cs:156`),
  so taps reopen the game correctly.

### The one collision

`ShinyAndroidActivity` derives from `AppCompatActivity`, and MonoGame forces you into
`AndroidGameActivity`. You cannot inherit both.

This is not a real problem: `ShinyAndroidActivity` is a pure forwarder. Four overrides, each a
single call into `Host.Lifecycle`:

| Override | Forwards to |
|---|---|
| `OnCreate` | `Host.Lifecycle.OnActivityOnCreate(this, savedInstanceState)` |
| `OnNewIntent` | `Host.Lifecycle.OnNewIntent(this, intent)` |
| `OnActivityResult` | `Host.Lifecycle.OnActivityResult(this, requestCode, resultCode, data)` |
| `OnRequestPermissionsResult` | `Host.Lifecycle.OnRequestPermissionsResult(this, requestCode, permissions, grantResults)` |

`OnRequestPermissionsResult` is the one that cannot be skipped — `AndroidPlatform` completes its
pending-permission `TaskCompletionSource` there, so every permission request hangs forever without
it. That is a silent hang, not an error, which makes it the single most important thing to document.

`ShinyAndroidApplication` is **not** a collision — MonoGame supplies no `Application` class, so a
plain `[Application]` subclass works as-is.

### iOS is easier still

MonoGame's iOS template already has a plain `AppDelegate : UIApplicationDelegate`, so it can
inherit `ShinyAppDelegate` directly. Most lifecycle work goes through `NSNotificationCenter`
observers registered in `IosLifecycleExecutor.Start()` anyway; the AppDelegate overrides only
matter for push tokens, background URL session completion, and `ContinueUserActivity`.

## Corrected desktop matrix

An earlier assessment described the bare `net10.0` target as uniformly "bait no-ops." That is wrong
for three modules. The actual per-module behaviour on plain `net10.0` (what DesktopGL gets on
Windows, macOS and Linux):

| Module | Bare `net10.0` behaviour |
|---|---|
| **Shiny.Net.Discovery** | **Real.** Managed mDNS responder; SSDP and WS-Discovery are managed on *every* platform |
| **Shiny.Jobs** | **Real.** `Platforms/Net/JobManager` — in-process 30s timer over foreground jobs |
| **Shiny.Net.Wifi** | **Honest stub.** `NetWifiManager`: `Capabilities => None`, addressing from `NetworkInformation`, Wi-Fi-specific calls throw |
| **Shiny.Locations** | **No-op.** `AddGps` `#else` branch returns `services` untouched |
| **Shiny.Core** | **No `IPlatform`.** `AddShinyCoreServices()` is entirely `#if PLATFORM` |
| **Shiny.BluetoothLE** | **No API.** `AddBluetoothLE` is inside `#if APPLE \|\| ANDROID \|\| WINDOWS` — calling it is a compile error, not a silent no-op |

Plus the standalone Linux packages (`Shiny.BluetoothLE.Linux`, `Shiny.Net.Wifi.Linux`,
`Shiny.Notifications.Linux`, `Shiny.Core.Linux`), which are pure managed `net10.0` over DBus/sysfs,
reference no `IPlatform`, and work in any plain .NET app — DesktopGL included.

**Known gap:** `Platforms/Net/JobManager` depends on `IBattery` and `IConnectivity`. On bare
`net10.0` only `Shiny.Core.Linux` registers those (`AddBattery`/`AddConnectivity`). A DesktopGL
game on Windows or macOS calling `AddJobs()` will fail to resolve them. See Phase 3.

## Phase 1 — document it (~2 days)

The highest-value work, because the thing mostly works and nobody knows.

1. Docs-site page under the appropriate module: "Using Shiny with MonoGame."
2. Cover, concretely:
   - `Shiny.Hosting.Native` is the entry point, not `Shiny.Hosting.Maui`
   - the `[Application] : ShinyAndroidApplication` pattern
   - the four forwarding overrides for `AndroidGameActivity`, with the
     `OnRequestPermissionsResult` hang called out explicitly
   - iOS: inherit `ShinyAppDelegate` directly
   - Windows: MonoGame WindowsDX/DesktopGL default to a plain TFM; retarget to
     `net10.0-windows10.0.19041.0` to light up the Windows backends
   - the desktop matrix above, verbatim — set expectations before people discover them
3. `readme.md` mention.

## Phase 2 — working sample (~3–4 days)

`samples/Sample.MonoGame` — Android + iOS, one module that proves lifecycle wiring end to end.

BLE or Locations is the right choice because both exercise `OnRequestPermissionsResult`, which is
the piece most likely to be got wrong. A sample that only calls Discovery proves nothing about the
wiring.

Per `CLAUDE.md`: **a sample-only change gets no release note.**

## Phase 3 — close the desktop gaps (~2–3 days)

Small, real, and independent of MonoGame — these improve every plain-.NET consumer.

1. **Register `IBattery`/`IConnectivity` on the bare target.** Today `Shiny.Jobs` on `net10.0`
   registers a `JobManager` whose dependencies nothing provides outside Linux. Either supply
   cross-platform managed defaults (`NetworkInformation`-backed connectivity is already proven in
   `Shiny.Core.Linux`) or make `AddJobs` fail loudly with a message that names the missing
   registration. Silent DI failure at resolve time is the worst option and is what happens now.
2. **Decide the `AddGps` no-op policy.** `Shiny.Locations`' `#else` returning `services` unchanged
   means a DesktopGL game compiles, runs, resolves nothing, and reports no error. Consider throwing
   `PlatformNotSupportedException` at registration, matching the honesty of `NetWifiManager`, which
   documents itself as "deliberately a stub rather than a lie."

## Phase 4 — optional `Shiny.Hosting.MonoGame` package (~1 week)

Only worth building if Phase 2 shows the wiring is a repeated stumbling block.

Contents would be thin:

- `ShinyGameActivity : AndroidGameActivity` with the four forwards pre-wired
- an iOS `ShinyGameAppDelegate` convenience
- Windows: set `WindowsPlatform.MainThreadHandler` to marshal onto the game loop. The static hook
  already exists (`src/Shiny.Core/Platforms/Windows/WindowsPlatform.cs`) and is normally set by
  the MAUI host; without it `InvokeOnMainThread` runs inline on the calling thread, which for a
  game is a real correctness issue rather than a cosmetic one.

The honest counter-argument: four overrides in the docs may be cheaper for everyone than a package
to version and maintain. Decide after Phase 2, not before.

## Sequencing against the Unity plan

Phase 1 of `plans/unity-support.md` — the `netstandard2.1` target for `Shiny.Net.Discovery` — also
benefits MonoGame DesktopGL and Godot desktop. It is not required for MonoGame (DesktopGL is
already `net10.0`-capable) but it widens the reach of the same work. No dependency in either
direction; do them in whichever order suits.

## Risks

| Risk | Impact | Mitigation |
|---|---|---|
| `AndroidGameActivity` base class changes across MonoGame versions | Docs go stale | Document the forwarding calls, not the inheritance chain |
| Forgotten `OnRequestPermissionsResult` | Silent permission hang | Lead with it in docs; pre-wire it in Phase 4 |
| Windows retargeting to the 19041 TFM unvalidated | Docs claim something untested | Actually build it in Phase 2 before documenting |
| `MainThreadHandler` unset on Windows | Callbacks on wrong thread | Phase 4, or document the manual assignment |

## Definition of done (when work actually happens)

Per `CLAUDE.md`:

1. `readme.md` updated
2. Relevant `skills/<shiny-module>/` updated — MonoGame hosting guidance belongs with the
   `shiny-core` skill, since `Shiny.Hosting.Native` is core hosting
3. Docs site: feature page + release note in the relevant module's `release-notes.mdx`

Note the split: Phase 1 (docs) and Phase 2 (sample) change no library behaviour and therefore get
**no release note**. Phase 3 and Phase 4 do change shipped behaviour and do.
