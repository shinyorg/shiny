---
name: shiny-liveactivities
description: Live Activities for .NET MAUI with Shiny.Mobile.LiveActivities - iOS/iPadOS ActivityKit (Lock Screen, Dynamic Island) and Android 16 Live Updates (promoted ongoing notification, status bar chip) behind one typed API, with both APNs push token kinds for server-driven updates.
auto_invoke: true
triggers:
  - live activity
  - live activities
  - Live Updates
  - Dynamic Island
  - ActivityKit
  - Lock Screen activity
  - Shiny.Mobile.LiveActivities
  - Shiny.iOS.LiveActivities.Binding
  - ILiveActivityManager
  - ILiveActivityDelegate
  - LiveActivityDelegate
  - LiveActivityContent
  - LiveActivityRequest
  - LiveActivityProgress
  - LiveActivityAlert
  - LiveActivityContentSchema
  - LiveActivityState
  - AddLiveActivities
  - LiveActivityOptions
  - ChannelName
  - ChannelDescription
  - notification channel localization
  - push to start token
  - PushToStartToken
  - OnPushToStartTokenChanged
  - OnPushTokenChanged
  - LiveActivityStart
  - LiveActivityUpdate
  - NSSupportsLiveActivities
  - widget extension
  - ShinyLiveActivityWidget
  - ShinyLiveActivityWidgetSource
  - ShinyLiveActivityBundleId
  - ShinyLiveActivityCodesignProvision
  - ShinyLiveActivityCodesignEntitlements
  - ShinyLiveActivityMinimumOSVersion
  - ShinyLiveActivitySupportsFrequentUpdates
  - ShinyLiveActivity.appex
  - ShinyActivityAttributes
  - promoted ongoing notification
  - requestPromotedOngoing
  - setShortCriticalText
---

# Shiny Live Activities

`Shiny.Mobile.LiveActivities` puts one API in front of the persistent, updating status surface both phone
platforms grew: **iOS/iPadOS Live Activities** (ActivityKit — Lock Screen and Dynamic Island) and
**Android 16 Live Updates** (a promoted ongoing notification that also renders as a status bar chip and on
the always-on display).

## Platform support — read this first

| Platform | What you get |
|---|---|
| iOS / iPadOS 16.2+ | A real Live Activity via ActivityKit, rendered by a SwiftUI widget extension the package builds into your app (`ShinyLiveActivityWidget`) |
| Android 16 (API 36)+ | `Notification.ProgressStyle` + `requestPromotedOngoing` + `setShortCriticalText` |
| Android 8-15 | An ordinary ongoing notification with a determinate progress bar (no chip) |
| macOS, Mac Catalyst, tvOS, Windows, Linux, Blazor | `NoOpLiveActivityManager` — `IsSupported` is false, every call is a safe no-op |

**There is no macOS or Mac Catalyst implementation, and there never will be one.** `ActivityKit.framework`
ships in the macOS SDK, which misleads people, but every public type in it is annotated
`@available(macOS, unavailable)`, `@available(macCatalyst, unavailable)`, `@available(tvOS, unavailable)`
and `@available(watchOS, unavailable)`. What a Mac shows is an *iPhone's* activity mirrored over iPhone
Mirroring — nothing a Mac app declares or drives. That is why the native binding package is named
`Shiny.iOS.LiveActivities.Binding`, and why the .NET package targets only `net10.0`, `-android` and `-ios`.

Because unsupported platforms get a no-op rather than a throw, shared view models need no `#if` — branch on
`IsSupported` only when the UI should hide the feature entirely.

## Packages

| Package | Role |
|---|---|
| `Shiny.Mobile.LiveActivities` | The cross-platform API. This is the one you reference. |
| `Shiny.iOS.LiveActivities.Binding` | The ActivityKit Swift shim, pulled in automatically on iOS. ActivityKit is Swift-only with no Objective-C interface, so it is bound through an `@objc` bridge built from `native/ShinyLiveActivities` by Xcode. Never reference this directly. |

Namespace is `Shiny.LiveActivities` (the `Mobile` segment is a package-scope qualifier, not part of the API).

## Registration

```csharp
builder.Services.AddLiveActivities();

// or, if you push updates from a server — the delegate is the ONLY way to learn the tokens
builder.Services.AddLiveActivities<MyLiveActivityDelegate>();
```

Both overloads take an optional `Action<LiveActivityOptions>`. It is **Android-only** — iOS has no
channel and nothing app-settable to configure:

```csharp
builder.Services.AddLiveActivities(o =>
{
    o.ChannelName        = Strings.LiveActivityChannelName;   // shown in Android notification settings
    o.ChannelDescription = Strings.LiveActivityChannelBlurb;
});
```

Always localize these. They default to English (`"Live Activities"` / `"Ongoing updates such as
deliveries, timers and scores"`) and are re-applied on every startup, so a translation that ships after
first launch still lands. Channel **importance and sound are not configurable** - Android ignores both
once the channel exists, and from that point they belong to the user.

## Start, update, end

```csharp
public class OrderTracker(ILiveActivityManager activities)
{
    string? activityId;

    public async Task Begin(string orderNumber)
    {
        if (!activities.IsSupported)
            return;

        var access = await activities.RequestAccess();
        if (access != AccessState.Available)
            return;

        var activity = await activities.Start(new LiveActivityRequest
        {
            // static for the activity's whole life — on iOS this becomes ShinyActivityAttributes
            Attributes = new Dictionary<string, string> { ["orderNumber"] = orderNumber },
            Kind       = "delivery",           // your widget branches on this
            Content    = new LiveActivityContent
            {
                Title       = "Order confirmed",
                Body        = "Preparing your order",
                ShortStatus = "0%",            // Dynamic Island compact / Android status bar chip
                Progress    = LiveActivityProgress.FromValue(0.0)
            }
        });
        this.activityId = activity.Id;
    }

    // Content REPLACES the previous state — always send the complete content, never a delta
    public Task Advance(double percent, string body) => activities.Update(
        this.activityId!,
        new LiveActivityContent
        {
            Title       = "Out for delivery",
            Body        = body,
            ShortStatus = $"{percent:P0}",
            Progress    = LiveActivityProgress.FromValue(percent)
        }
    );

    public Task Finish() => activities.End(
        this.activityId!,
        new LiveActivityContent { Title = "Delivered", Progress = LiveActivityProgress.FromValue(1.0) },
        dismissAt: DateTimeOffset.UtcNow.AddMinutes(2)
    );
}
```

`GetAll()` returns what is currently running (newest first) and `EndAll()` clears everything — call it on
logout. Pass a `LiveActivityAlert` to `Update` to surface a banner instead of refreshing silently.

## What C# can and cannot express

Get this wrong and you will generate code that cannot work. Two separate limits:

**1. The layout is never C#.** WidgetKit requires SwiftUI in a widget extension signed with the app's own
bundle id. C# only ever sends *state*. Never generate C# that tries to describe a Lock Screen or Dynamic
Island layout, and never suggest a package that would.

**2. There is exactly one `ActivityAttributes` type.** ActivityKit needs a concrete `Codable` type at
compile time, so Shiny pins a single `ShinyActivityAttributes` and routes everything app-specific through
string dictionaries.

| Strongly typed | Free-form |
|---|---|
| `title`, `body`, `shortStatus`, `progress`, `progressStart`, `progressEnd`, `indeterminate` | `data` (dynamic), `values` (static, from `LiveActivityRequest.Attributes`) |

Consequences to respect when generating code:

- **Custom fields must be strings.** Flatten numbers, dates, objects and arrays into `Data` on the C# side
  and parse them in Swift. Only the progress family is typed. Do not invent typed properties on
  `LiveActivityContent`.
- **One attributes type per app.** Use `Kind` as the discriminator (`switch context.attributes.kind` in the
  widget), not several `ActivityAttributes` structs. A push-to-start payload names `ShinyActivityAttributes`
  as its `attributes-type`.
- **4KB content-state cap**, easier to hit with everything stringified.

**Out of reach through this package** — say so rather than generating something that compiles and does
nothing:

- **Interactive activities.** A `Button`/`Toggle` backed by an `AppIntent` lives in the Swift extension, and
  `ILiveActivityDelegate` has no action callback (only `OnStarted`, `OnStateChanged`, and the two token
  events). Bridging back to .NET needs an app group or URL scheme the developer wires themselves.
- **Alert sound.** `LiveActivityAlert` is title + body only.

**What fits:** anything whose changing state is title / body / short status / progress / stale date /
relevance score plus a string bag — delivery tracking, rideshare ETA, sports scores, timers, workouts,
transfer progress.

If a genuinely typed schema is required, the only route is forking `ShinyActivityAttributes.swift` in
**both** `native/ShinyLiveActivities/` and `templates/WidgetExtension/` (the copy the widget build compiles) plus
`LiveActivityContentSchema` —
which leaves the contract shared with `Shiny.Extensions.Push`, where drift fails silently. Flag that
tradeoff rather than doing it silently.

## Prefer a time range over a fraction

```csharp
// self-advancing: the system animates it with no further updates from you
Progress = LiveActivityProgress.FromRange(startedAt, expectedFinish)
```

Every push update costs budget, and on iOS a suspended app sends none at all — a fraction-based bar simply
freezes. `FromRange` keeps moving with no app involvement. Use `FromValue` only when the fraction is
genuinely known and not time-shaped, and `Indeterminate = true` when it is unknown.

## The iOS widget extension — one property, no Xcode

iOS renders the activity from a **SwiftUI widget extension in your app bundle** — nothing about the layout
can be driven from C#. Without it (or without `NSSupportsLiveActivities`), `Start` succeeds and the activity
renders nothing, silently. **Always generate this in the app head's `.csproj` when the app targets iOS:**

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <ShinyLiveActivityWidget>true</ShinyLiveActivityWidget>
</PropertyGroup>
```

The package's build targets then compile the stock widget with `swiftc` into `PlugIns/ShinyLiveActivity.appex`
(bundle id `$(ApplicationId).LiveActivity`), infer and embed its provisioning profile on device builds, sign it
with the app's identity, and merge `NSSupportsLiveActivities` into Info.plist. It is opt-in so Android-only and
push-only apps never run a Swift build, and it needs a Mac (skipped with a warning elsewhere).

Do **not** generate any of the old manual setup: no Xcode widget project, no copied Swift, no
`<XcodeProject Kind="AppExtension">` (the SDK treats `Kind` as a `NativeReference` kind, so that never embedded
an extension), no hand-added `NSSupportsLiveActivities`.

| Property / item | Default | Use |
|---|---|---|
| `ShinyLiveActivityWidget` | `false` | Turn the build on |
| `ShinyLiveActivityBundleId` | `$(ApplicationId).LiveActivity` | Extension bundle id — permanent once shipped (it owns a provisioning profile) |
| `ShinyLiveActivityCodesignProvision` | best installed match (exact, then team wildcard) | Name/UUID of the extension's profile — set for Release/CI or when several match |
| `ShinyLiveActivityCodesignEntitlements` | — | Extra entitlements plist (app groups) |
| `ShinyLiveActivityMinimumOSVersion` | `SupportedOSPlatformVersion`, at least 16.2 | Extension deployment target |
| `ShinyLiveActivityDisplayName` | `Live Activity` | Extension display name |
| `ShinyLiveActivitySupportsFrequentUpdates` | `false` | Also adds `NSSupportsLiveActivitiesFrequentUpdates` |
| `ShinyLiveActivityWidgetSource` (item) | stock widget | Your own SwiftUI sources replacing the stock widget |

Signing: simulator builds need nothing. Debug on a device works with a team wildcard development profile.
App Store/Ad Hoc builds need a distribution profile covering `<app>.LiveActivity` (a wildcard App Store profile
counts). No matching profile is a build error naming the bundle id; a profile of a different kind from the
app's (Ad Hoc vs App Store) is a warning.

**Custom layout.** Generate Swift files with a `@main` `WidgetBundle` rendering
`ActivityConfiguration(for: ShinyActivityAttributes.self)` and include them:

```xml
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <ShinyLiveActivityWidgetSource Include="Platforms/iOS/LiveActivity/*.swift" />
</ItemGroup>
```

**Never declare `ShinyActivityAttributes` in those files** — the package always compiles its own copy next to
them, which is what keeps the widget and the app on the same type. Sources are Swift only (no asset catalog or
`.strings`), so use SF Symbols and text from the content. Read custom values from `context.state.data` and
`context.attributes.values`; branch on `context.attributes.kind`.

Android needs no such thing — `POST_NOTIFICATIONS` on API 33+, which `RequestAccess()` asks for.

## Push tokens: three kinds, and they are not interchangeable

| Token | Lifetime | `PushTokenKind` (server) | Where you get it |
|---|---|---|---|
| Device token | the install | `Device` | `Shiny.Push` |
| Push-to-start (iOS 17.2+) | the install, valid before any activity exists | `LiveActivityStart` | `OnPushToStartTokenChanged` |
| Per-activity update | born and dead with that one activity | `LiveActivityUpdate` | `OnPushTokenChanged` |

```csharp
public class MyLiveActivityDelegate(IMyApi api) : LiveActivityDelegate
{
    public override Task OnPushToStartTokenChanged(string token)
        => api.RegisterToken(token, "LiveActivityStart");

    public override Task OnPushTokenChanged(LiveActivity activity, string token)
        => api.RegisterActivityToken(activity.Id, token);

    public override Task OnStateChanged(LiveActivity activity) => Task.CompletedTask;
}
```

Send an ordinary alert to a Live Activity token and APNs answers `DeviceTokenNotForTopic`, which token
managers read as "invalid" and **prune**. Keep the kinds separated. A `410 Unregistered` on an update token
is normal — that activity ended. Both tokens are null on Android, where you update the activity yourself
(typically from an FCM data message handled by `Shiny.Push`). Never poll `PushToStartToken`; watch the
delegate.

## The wire contract — three definitions, one shape

`LiveActivityContentSchema` is the JSON both platforms and a server push agree on:

```json
{
  "title": "Out for delivery", "body": "2 stops away", "shortStatus": "5 min",
  "progress": 0.65, "progressStart": 774835200.0, "progressEnd": 774838800.0,
  "indeterminate": false, "data": { "orderId": "A-1234" }
}
```

**Dates inside `content-state` are seconds since 2001-01-01** (Swift's stock `Codable` `Date` encoding), not
the Unix epoch — `LiveActivityContentSchema.ToAppleReferenceSeconds` converts. The `aps` envelope
(`timestamp`, `stale-date`, `dismissal-date`) stays Unix.

A field-name or type drift between the C# `LiveActivityContent`, the Swift `ShinyActivityAttributes.ContentState`
(in **both** `native/` and `templates/WidgetExtension/`; the widget build always compiles the package's copy, so
app vs widget can't drift), and the server's `content-state` **does not throw** —
ActivityKit silently drops the update and the activity just stops refreshing. Change all of them together.

Keep the payload small: ActivityKit caps content-state at 4KB. `Data` is deliberately `string`-keyed and
`string`-valued so the payload is identical whether it came from your app or a server.

## Server side

`Shiny.Extensions.Push` sends these: `LiveActivityPush.Start(...)` / `.Update(...)` / `.End(...)` with
`SendLiveActivityToTokens(...)`. Live Activity pushes use a different push type, topic and `aps` body from an
ordinary alert — that library handles it.

## Already done for you: HTTP transfer progress

Do **not** hand-roll a Live Activity for `Shiny.Net.Http` uploads/downloads. `AddTransferProgress()` in
`Shiny.Net.Http` already drives one on iOS and the foreground-service notification on Android, from one
manager with no code in your transfer delegate. See the `shiny-http-transfers` skill.

## Gotchas

1. **`Update` replaces, it does not merge.** Send the complete content every time.
2. **iOS refuses to start an activity from the background.** Expect `Start` to fail on a background wake and retry on the next update.
3. **iOS caps how many activities an app may run**, and may refuse outright.
4. **`Kind` is yours to interpret.** Your widget branches on it; the stock Android renderer only logs it.
5. **`RelevanceScore`** ranks your own activities for the Dynamic Island — iOS only.
6. **`StaleDate`** flips the activity to `LiveActivityState.Stale` so the widget can render an "out of date" view. It does not end it.
7. **`End` without `dismissAt`** leaves the final state on screen for up to four hours on iOS. Pass a past time to remove it now.
