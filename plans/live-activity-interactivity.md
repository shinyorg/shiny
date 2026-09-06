# Plan: Live Activity interactivity & alert sound

Status: **proposal** — nothing here is committed work.
Last updated: 2026-09-06 (Phase 0 spike run — see [Phase 0 results](#phase-0-results); decisions 1 and 3 settled; the Android channel fix has shipped)

## Summary

Two gaps documented as out-of-reach when `Shiny.Mobile.LiveActivities` landed:

1. **Alert sound.** `LiveActivityAlert` carries a title and body; Apple's `AlertConfiguration` also
   takes a sound. The Swift shim currently hardcodes `sound: .default`.
2. **Interactive activities.** A `Button`/`Toggle` in a Live Activity is backed by an `AppIntent`
   living in the widget extension, and `ILiveActivityDelegate` has no action callback.

Both are workable. They are very different sizes, and only one of them has a genuine unknown.

The thing that makes interactivity possible at all is `LiveActivityIntent` (iOS 17+). A plain
`AppIntent` in a widget extension executes **in the extension's process**, where there is no .NET
runtime and never will be. An intent conforming to `LiveActivityIntent` (or `AudioPlaybackIntent`)
is run by the system **in the app's process** — which in a MAUI app *is* the .NET runtime. That is
the whole enabler; without it this plan would end here.

Scope decisions taken up front:

| Decision | Choice | Consequence |
|---|---|---|
| iOS action buttons | **Declared in C#, rendered by SwiftUI** | The widget owns layout, so C# can only supply ids/titles for it to draw. Actions therefore ride in content-state, which makes this a **wire-contract change** (see [Cross-repo](#cross-repo-coordination)). |
| iOS interactivity floor | **iOS 17.2** for in-place actions | Declared as 17.0, but the metadata processor clamps every intent up to 17.2 (measured, Phase 0 result 4). The module targets 16.2, so 16.2–17.1 degrades to Phase 2 deep-links rather than losing the activity. |
| Android alert sound | **Channel-level, not per-alert** | A notification channel owns its sound from API 26. `LiveActivityAlert.Sound` is honestly documented as iOS-only; Android gets a one-time channel setting. |
| Widget/framework wiring | **Link the framework** (settled by Phase 0) | A copied source file compiles into the widget's own module and yields a different mangled type. The widget must link `ShinyLiveActivities.framework`, which breaks the current template instructions. |

## SDK verification

Dumped from the Xcode SDKs installed on this machine, not from documentation.

| Claim | Evidence | Result |
|---|---|---|
| `AlertConfiguration` supports a sound | `iPhoneOS.sdk/.../ActivityKit.swiftmodule/arm64e-apple-ios.swiftinterface:434-445` | ✅ `init(title:body:sound:)`; `AlertSound` is `.default` or `.named(String)` — no silent option |
| `LiveActivityIntent` exists and is iOS-only | `iPhoneOS.sdk/.../AppIntents.swiftmodule/arm64e-apple-ios.swiftinterface:804-809` | ✅ `protocol LiveActivityIntent : SystemIntent`, `@available(iOS 17.0, *)`, unavailable on macOS/watchOS/tvOS |
| `LiveActivityStartingIntent` is the pre-17 form | same file, `:810-814` | ⚠️ deprecated in 17.0 — *"Use LiveActivityIntent instead"* |
| ActivityKit is iOS-only | `MacOSX.sdk/.../ActivityKit.swiftinterface:12-16` | ✅ every public type `@available(macOS, unavailable)` / `macCatalyst` / `tvOS` / `watchOS`. Nothing here changes that. |
| The shim already builds an alert | `native/ShinyLiveActivities/ShinyLiveActivities/ShinyActivityBridge.swift:115-122` | ✅ hardcodes `sound: .default` — one parameter away from done |

One claim is **not** verifiable from the SDK and drives Phase 0: whether AppIntents will match an
intent type across the app and widget-extension modules. See [The open risk](#the-open-risk).

## Platform asymmetries

These cannot be designed away and should be documented rather than papered over.

| | iOS | Android |
|---|---|---|
| Alert sound | Per-alert, on `AlertConfiguration` | Channel property (API 26+); cannot vary per notification |
| Action buttons | Baked into your SwiftUI; C# supplies ids/titles only | Fully C#-declared per notification via `Notification.Builder.AddAction` |
| Interactivity floor | iOS 17.2 in-place; 16.2–17.1 deep-links only | Works everywhere the module works |
| Action dispatch | `LiveActivityIntent.perform()` in the app process | `PendingIntent` → `BroadcastReceiver` |

## The open risk

ActivityKit matches `ActivityAttributes` **by type-name string**. That is why copying
`ShinyActivityAttributes.swift` into the widget extension works today, and why a push-to-start
payload names `attributes-type` as a plain string.

**AppIntents is not obviously so forgiving.** For the system to route the intent to the app's
process, the intent must appear in the app's build-time `.appintents` metadata, and a
framework-declared intent generally needs an `AppIntentsPackage` declaration in the app target to be
exported into it. If the widget compiles its own copy of the source, the app and the widget hold two
*different* types (`ShinyLiveActivities.ShinyLiveActivityActionIntent` vs
`MyWidget.ShinyLiveActivityActionIntent`).

If that mismatch is real, the widget extension must **link `ShinyLiveActivities.framework`** rather
than copy Swift files. That is a breaking change to the documented setup — and it would incidentally
resolve a contradiction that already exists in the repo:

- `native/ShinyLiveActivities/ShinyLiveActivities/ShinyActivityAttributes.swift` says
  *"Your widget extension links this framework and renders it"*.
- `templates/WidgetExtension/README.md` says *"Replace the generated Swift"* with copies.

Both work today because ActivityKit matches by name. Only one will survive Phase 3.

---

## Phase 0 — spike

Half a day. **Phase 3 is entirely contingent on this**; Phases 1 and 2 are not.

Prove, in a throwaway MAUI app:

1. A `LiveActivityIntent` declared in `ShinyLiveActivities.framework` fires when a widget-extension
   button is tapped, and `perform()` runs **in the app's process** (assert by touching managed state
   or logging from a C#-registered callback).
2. It works with the app **not running** — iOS should background-launch it. This is the same relaunch
   path background `NSURLSession` already exercises, so Shiny's `IShinyStartupTask` wiring should
   cover it; confirm rather than assume.
3. Whether the widget can **copy** the intent source or must **link** the framework, and whether an
   `AppIntentsPackage` declaration is needed in the app target.

Outcome: confirm or kill Phase 3, and settle the template's copy-vs-link question either way.

## Phase 0 results

Run 2026-09-06 against Xcode 26.6 (17F113), iOS SDK 26.5, in a scratch copy of
`native/ShinyLiveActivities` plus a throwaway host app and the widget template. Six builds; no device
was involved, so everything below is build-time evidence. **The two runtime questions remain open.**

### 1. The intent itself is fine ✅

`ShinyLiveActivityActionIntent : AppIntent, LiveActivityIntent` with `@Parameter` properties compiles
in the framework, and Xcode runs `appintentsmetadataprocessor` over it, emitting
`ShinyLiveActivities.framework/Metadata.appintents`.

### 2. Copy vs link — the risk was real ✅ *(answers open decision 2)*

The same source file, compiled two ways:

| Built as | `fullyQualifiedTypeName` | `mangledTypeNameV2` |
|---|---|---|
| Framework | `ShinyLiveActivities.ShinyLiveActivityActionIntent` | `19ShinyLiveActivities0aB20ActivityActionIntentV` |
| Widget, copied source | `ShinyLiveActivityWidgetTemplate.ShinyLiveActivityActionIntent` | `31ShinyLiveActivityWidgetTemplate0abC12ActionIntentV` |

The `identifier` matches (`ShinyLiveActivityActionIntent`) but the **type does not** — the module name
is baked into the mangled name. **The widget must link `ShinyLiveActivities.framework`, not copy the
Swift.** When it links, the `.appex` emits no metadata of its own and `otool -L` shows
`@rpath/ShinyLiveActivities.framework/ShinyLiveActivities` — one definition, which is the correct
topology.

This settles the template's copy-vs-link contradiction in favour of *link*.

### 3. The blocker: a linking app gets no metadata at all 🔴

An app that merely links the framework produces **no `Metadata.appintents` bundle whatsoever**. The
framework's intents are not merged automatically.

Adding `AppIntentsPackage` on both sides fixes it, and reveals the mechanism — the app does *not* copy
the intents, it **references** them:

```
HostApp.app/Metadata.appintents/extract.packagedata
  {"version":1,"includes":["19ShinyLiveActivities0abC7PackageV"]}

HostApp.app/Frameworks/ShinyLiveActivities.framework/Metadata.appintents/extract.actionsdata
  actions: [ShinyLiveActivityActionIntent, ...]
```

So the chain is: **app package → framework package → framework's actions.** Both halves are required.

**The problem is the app half.** It is `struct MyAppPackage: AppIntentsPackage` — *Swift, in the app
target*. A .NET MAUI app has no Swift app target, and the .NET iOS SDK has **no AppIntents awareness at
all**: nothing in `Microsoft.iOS.Sdk 26.5.9004` invokes `appintentsmetadataprocessor` or handles
`Metadata.appintents`. A MAUI app therefore never generates the bundle, and the include chain can never
form by ordinary means.

**Workaround, viable but ugly.** The format is now known and tiny, so the NuGet package could ship a
prebuilt `Metadata.appintents` as a `BundleResource`: `extract.packagedata` with the framework
package's mangled name, an empty `extract.actionsdata` skeleton, and `version.json`. That pins an
undocumented format, hard-codes a mangled Swift name, and stamps an Xcode version into `generator` —
so it needs a guard test that rebuilds the framework and asserts the mangled name still matches.

### 4. The floor is iOS 17.2, not 17.0 ⚠️

Three intents declared `@available(iOS 17.0, *)` — including a plain `AppIntent` with no
`LiveActivityIntent` conformance — all came out of the processor stamped `introducedVersion: 17.2`. A
control declared `@available(iOS 18.0, *)` echoed `18.0`, so the processor clamps *up* to a 17.2 floor
rather than ignoring the annotation. Cause not identified; the behaviour is consistent. **Phase 2's
deep-link fallback therefore has to cover 16.2–17.1, not 16.2–16.x.**

### 5. Incidental repo finding ⚠️

`native/ShinyLiveActivities/project.yml` puts `BUILD_LIBRARY_FOR_DISTRIBUTION: YES` in `settings.base`,
so the `ShinyLiveActivityWidgetTemplate` **app-extension** target inherits it. That is meaningless for
an appex, and it hard-fails the build the moment the template gains an `AppIntent` — swiftinterface
verification rejects `@Parameter`'s unavailable `init()`. Harmless today, a trap for Phase 3. One line
to fix whenever it is touched.

### Still unproven — needs a device 🔶

Nothing above required running the app, and these two cannot be answered without a physical device and
a human tapping a button:

1. That `perform()` genuinely executes **in the app's process** for a .NET MAUI app once the metadata
   chain is in place.
2. That iOS **background-launches** the app for it when the app is not running.

Both are documented Apple behaviour, and Shiny already relies on the same relaunch path for background
`NSURLSession`. Neither is verified here.

### Verdict

Phase 3 is **not dead, but materially harder than assumed**. The Swift side is straightforward; the
cost is entirely in the plumbing — the widget must link the framework, and the NuGet package must
synthesize an app-level metadata bundle that the .NET iOS SDK has no notion of. Recommend deciding
whether that plumbing is worth it *before* writing any of the Phase 3 API, and shipping Phase 2's
deep-links regardless, since they now cover 16.2–17.1 and cost almost nothing.

## Phase 1 — alert sound

Independent of the spike. Small. Ship first.

### Phase 1a — Android channel options ✅ **done**

Split out and shipped on its own as the localization fix (open decision 3). `LiveActivityOptions` +
`AddLiveActivities(configure)` / `AddLiveActivities<TDelegate>(configure)` now carry `ChannelName` and
`ChannelDescription`; `EnsureChannel()` no longer skips when the channel exists, because a repeat
`createNotificationChannel` with the same id is how Android updates a name and description - so a
translation that ships after first launch still reaches the user's settings screen.

Importance and sound are deliberately **not** options: Android ignores both once the channel exists,
and they belong to the user from that point. That constrains Phase 1b below.

### Phase 1b — alert sound (remaining)

**iOS**
- `LiveActivityAlert` gains `string? Sound` — null keeps `.default`, any other value maps to
  `.named(value)`. There is no silent option in `AlertSound`; an alerting update always makes noise.
- `ShinyActivityBridge.update(...)` gains an `alertSound:` parameter; replace the hardcoded
  `sound: .default` at `ShinyActivityBridge.swift:120`. Update `ApiDefinition.cs` to match.

**Android** — `LiveActivityOptions` already exists after Phase 1a, so this is only the sound itself.
Note the constraint found while doing 1a: a channel's sound is fixed **at creation** and Android
ignores it on every subsequent `createNotificationChannel`. So an app that ships a custom sound in an
update cannot apply it to users who already ran the old build without changing `ChannelId`, which
resets the user's own preferences. Decide whether that is worth exposing at all, or whether Android
should simply be documented as "channel default, set by the user".

**Docs** — state plainly that `Alert.Sound` is iOS-only and that Android sound is a one-time channel
setting the OS will not let the app change afterwards.

## Phase 2 — deep-link actions

Small, no contract change, and it is the **only** option on iOS 16.2–17.1 (see [Phase 0 result 4](#4-the-floor-is-ios-172-not-170) — the AppIntents floor is 17.2, not 17.0).

- Add `widgetURL(_:)` / `Link(destination:)` to `templates/WidgetExtension/ShinyLiveActivityWidget.swift`,
  built from a value in `content.Data` so no new content-state field is needed.
- Android already sets a launch `SetContentIntent` (`LiveActivityManager.cs:169`) — extend it to carry
  the same URL.
- Handled in C# through the existing app-link plumbing; no new Shiny API.

Not true interactivity — it opens the app — but it covers a large share of real use for almost nothing.

## Phase 3 — true interactivity

**Contingent on Phase 0.**

### API surface

```csharp
public record LiveActivityAction(
    string Id,
    string Title,
    string? Icon = null,
    bool Destructive = false
);

// LiveActivityContent
public IReadOnlyList<LiveActivityAction> Actions { get; init; } = [];

// ILiveActivityDelegate  (+ a no-op override on LiveActivityDelegate)
Task OnAction(LiveActivity activity, string actionId);
```

### Android

`Notification.Builder.AddAction(icon, title, pendingIntent)` where the `PendingIntent` targets a
Shiny `BroadcastReceiver` carrying `activityId` + `actionId`; the receiver resolves
`ILiveActivityDelegate` and calls `OnAction`. Copy the pattern from `Shiny.Notifications`. Actions are
per-notification, so they may change on every update at no cost.

### iOS

- `ShinyLiveActivityActionIntent : AppIntent, LiveActivityIntent` in the framework, with
  `@Parameter var activityId: String` and `@Parameter var actionId: String`.
- `perform()` calls a static handler registered from C#, exactly the shape
  `startObservingWithStarted:token:pushToStart:state:` already uses
  (`ShinyActivityBridge.swift:231`).
- The template renders `Button(intent:)` per entry in `context.state.actions`, guarded
  `if #available(iOS 17.2, *)` with the Phase 2 deep-link as the fallback below it.

### The contract change

`actions` becomes a **new content-state field**, so it touches all three definitions at once:

- C# `LiveActivityContent` + `LiveActivityContentSchema` (+ schema tests)
- Swift `ShinyActivityAttributes.ContentState` in **both** `native/ShinyLiveActivities/` and
  `templates/WidgetExtension/`
- the server's `content-state` payload in `~/Desktop/dev/serverpush`

A drift between them **does not throw** — ActivityKit silently drops the update and the activity
simply stops refreshing. The field is additive and optional, so existing servers are unaffected.

Watch the 4KB content-state cap; a handful of actions is comfortably inside it.

## Cross-repo coordination

| Repo | Change | Phase |
|---|---|---|
| `~/Desktop/dev/serverpush` (`Shiny.Extensions.Push`) | `aps.alert.sound` on Live Activity alert payloads | 1 |
| `~/Desktop/dev/serverpush` | `actions` in the `content-state` builder | 3 |
| `~/Desktop/dev/documentation` | `liveactivities/` pages, widget template setup, release notes | 1–3 |
| this repo | `skills/shiny-liveactivities/SKILL.md` trigger list + constraints section | 1–3 |

## Decisions taken

1. ✅ **The additive `actions` content-state field is accepted in principle** (2026-09-06). Existing
   servers are unaffected because it is optional; the cost accepted is that both Swift `ContentState`
   copies gain a field that must stay in step with the C# and the server payload forever after, and
   drift there fails silently. Phase 3 is not blocked on this.
3. ✅ **`LiveActivityOptions` shipped on its own** as Phase 1a (2026-09-06), ahead of alert sound.

2. ✅ **Answered by the spike**: a copied source file does *not* match, so the widget must link the
   framework. Whether that is acceptable is now folded into decision 5.

## Open decisions

4. **Is an Android channel sound worth exposing at all**, given it can only be set at channel creation
   and cannot be changed afterwards without resetting the user's own settings? Raised by Phase 1a.
5. **Is Phase 3 worth its plumbing?** It now costs: the widget extension linking the framework
   (breaking the current template instructions), an `AppIntentsPackage` in the framework, and a
   hand-synthesized `Metadata.appintents` shipped as a `BundleResource` because the .NET iOS SDK does
   not generate one — against an alternative (Phase 2 deep-links) that costs almost nothing and now
   covers every device below iOS 17.2.
