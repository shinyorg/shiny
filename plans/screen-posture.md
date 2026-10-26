# Plan: Screen posture (foldables + iPhone Duo)

Status: **implemented, uncommitted (2026-10-25)** — `IScreenPosture` in Shiny.Core for Android, iOS 27.1 and Blazor WASM,
registered by every host; unit tests (Segments maths, de-dupe/subscription, browser segment parsing) pass; Shiny.Core builds
for all 7 TFMs and `Build.slnf` has no IL/NU1107 warnings. Verified: Android on a Pixel Fold AVD (`adb emu posture 1/2/3` +
`rotate`: closed → None, half open → book / tabletop with dp fold rect, flat → Flat); iPhone Duo simulator (iOS 27.1) reports
`IsSupported`, `Hinge = Closed` at 0° through the hinge interaction and no fold on the outer screen — **hinge transitions were not
driven** (SpringBoard's display-tool / pose-override services refuse an unentitled client; Device Hub UI not tried); Chromium
(headless, CDP posture + display-feature overrides) for `posture.js`. Deviations: geometry is `ScreenRect` / `ScreenSize`, not
`Rect` / `Size` (MAUI ambiguity); iOS calls the 27.1 APIs through the ObjC runtime (`UIKit27.cs`) because a library's
`net10.0-ios` compiles against the 26.0 pack; Android detaches on Stopped (not Paused). AppDeviceBridge: not done (left to decide).
Last updated: 2026-10-25

## Summary

Add `IScreenPosture` to `Shiny.Core`, next to `IScreenInfo`. It reports whether the window is divided by
a fold or hinge, where the fold is, and the hinge state. The data comes from Jetpack WindowManager on
Android foldables and from the iOS 27.1 hinge and reserved-region APIs on **iPhone Duo**. It answers the
question every two-pane layout needs answered — *is there a fold, and where is it?* — once, for every
Shiny consumer:

- **Shiny.Maui.Controls**, for fold-aware Flyout, FloatingPanel and dialogs. See
  `~/Desktop/dev/controls/docs/foldables-plan.md`.
- **Plain MAUI apps**, for their own layouts.
- **Blazor**, via the browser Device Posture and Viewport Segments APIs.

`IScreenInfo` is the right neighbour because orientation and metrics already moved there from the old
`IAppSupport`, and posture is the same kind of fact: the display, as the app is shown on it.

## The platforms

| | Android foldables | iPhone Duo (iOS 27.1+) |
|---|---|---|
| Where the fold is | `FoldingFeature.Bounds` (window px) | `UIView.GetReservedRegions(divisionRegionKind)` → `UIViewReservedRegion.Frame` (pt). Present **only while partly open** |
| State | `FoldingFeature.State`: `Flat` / `HalfOpened` | `UIHinge.Status`: `Unknown` / `Closed` / `PartiallyOpen` / `FullyOpen` |
| Orientation | `FoldingFeature.Orientation`: `Horizontal` / `Vertical` | derived from the region frame's aspect |
| Separating | `FoldingFeature.IsSeparating` (always true when half-open) | the division region exists and `IsActive` |
| Hinge angle | **none** | `UIHinge.Angle` (radians; rate and precision not guaranteed — Apple says prefer `Status`) |
| Camera cut-outs | not reported | `occlusionRegionKind` regions (the outer camera is always present; the inner one only while it is active) |
| Change signal | `WindowInfoTracker` layout-info listener | `UIHingeInteraction` update handler; regions re-queried on layout |
| .NET binding | `Xamarin.AndroidX.Window.WindowJava` 1.5.1.3 (adds `Window` + `Window.WindowCore`) | **already in** `Microsoft.iOS.Ref.net10.0_27.1` (27.1.10018) — `UIHinge`, `UIHingeInteraction`, `UIHingeStatus`, `UIViewReservedRegion*`, `GetReservedRegions` all present (verified 2026-10-25 by `strings` over the installed ref pack) |
| Test rig | Pixel Fold or resizable AVD; `adb emu fold` / `unfold`, `adb emu posture 2` (tabletop) | `iPhone-Duo` simulator device type (iPhone19,4), iOS 27.1 runtime |

**There is no "spanning" mode on iPhone Duo.** The outer display is a compact-width size class and the
inner display is regular-width. Closed means "the app is on the outer screen", not "the app is on half a
spanned window". Android behaves the same way when fully flat (`IsSeparating` is false on a Pixel Fold
laid flat). So the model only needs to describe a fold **while it divides the window**; flat or closed
both read as `FoldState.None`, plus the plain hinge status for anyone who wants it.

## API

```csharp
namespace Shiny;

public enum FoldState { None, Flat, HalfOpen }
public enum FoldOrientation { None, Vertical, Horizontal }   // Vertical = book, Horizontal = tabletop
public enum HingeStatus { Unknown, Closed, PartiallyOpen, FullyOpen }

/// All rectangles are in window coordinates, device-independent units (dp / pt).
public sealed record ScreenPosture(
    FoldState State,
    FoldOrientation Orientation,
    Rect? Fold,                         // null unless the fold divides the window
    bool IsSeparating,
    HingeStatus Hinge,
    double? HingeAngleDegrees,          // iOS only
    IReadOnlyList<Rect> Occlusions)     // iOS cameras; empty on Android
{
    public static ScreenPosture None { get; }
    public bool IsTabletop => State == FoldState.HalfOpen && Orientation == FoldOrientation.Horizontal;
    public bool IsBook     => State == FoldState.HalfOpen && Orientation == FoldOrientation.Vertical;

    /// The two window regions either side of the fold, or null if nothing separates them.
    public (Rect First, Rect Second)? Segments(Size window);
}

public interface IScreenPosture
{
    bool IsSupported { get; }                    // false off Android / iOS 27.1+ / browser-with-API
    ScreenPosture Current { get; }
    event EventHandler<ScreenPosture>? Changed;  // watching starts with the first subscriber, as IScreenInfo does
    IReadOnlyList<FoldOrientation> SupportedPostures { get; }   // Android WindowInfoTracker.SupportedPostures; iOS: [Horizontal, Vertical] on Duo
}
```

`Rect` and `Size` mean whatever `ScreenMetrics` already uses for geometry. Check before adding a new
type; if it has none, add a small `readonly record struct` in `Screen/`.

`HingeAngleDegrees` is surfaced but documented as **for live effects only**. Layout decisions must use
`State` and `Fold`, which is Apple's own guidance and the only thing Android can supply.

## Implementation

### Files

```
src/Shiny.Core/Screen/
  IScreenPosture.cs
  ScreenPosture.cs            record + enums + Segments()
  AbstractScreenPosture.cs    subscribe-on-first-handler, de-dupe, raise Changed (mirrors AbstractScreenInfo)
src/Shiny.Core/Platforms/Android/AndroidScreenPosture.cs
src/Shiny.Core/Platforms/Apple/IosScreenPosture.cs
src/Shiny.Core/Platforms/Net/Browser/BrowserScreenPosture.cs (+ JS in the existing browser interop)
src/Shiny.Core/Platforms/Net/NetScreenPosture.cs           IsSupported=false, Current=None
```

Register beside `IScreenInfo` in `Platforms/Shared/ShinyInfrastructureExtensions.cs` (lines 32–70), one
line per `#if` branch. macOS, Windows, Linux and tvOS get the `Net` no-op. UIHinge is annotated for tvOS
but there is no foldable Apple TV, so don't bother.

### Android — `AndroidScreenPosture`

- Add `Xamarin.AndroidX.Window.WindowJava` 1.5.1.3 to `Directory.Packages.props` and to the Android
  item group of `Shiny.Core.csproj` (lines 41–44), next to `Xamarin.AndroidX.Core`. Watch for NU1107 —
  it pulls KotlinX Coroutines 1.11 and `Lifecycle` 2.x, and the Lifecycle pins are at 2.11.0.1 (lines
  63–71). Restore and resolve before writing code.
- Follow the activity: `AndroidPlatform.ActivityChanged` (`AndroidPlatform.cs:174`). Attach the
  listener on Resumed and detach it on Paused/Destroyed. Layout info is **per Activity**, not per
  process.
- Listener:
  ```csharp
  var tracker = new WindowInfoTrackerCallbackAdapter(WindowInfoTracker.Companion.GetOrCreate(activity));
  tracker.AddWindowLayoutInfoListener(activity, ContextCompat.GetMainExecutor(activity), consumer);
  // consumer : Java.Lang.Object, AndroidX.Core.Util.IConsumer — Accept(Java.Lang.Object) → WindowLayoutInfo
  ```
  Filter `DisplayFeatures` to `IFoldingFeature`. Convert `Bounds` from px to dp with
  `activity.Resources.DisplayMetrics.Density`. Bounds are window-relative already.
- `SupportedPostures` comes from `WindowInfoTracker.SupportedPostures` (1.4+). Check it is in the
  binding; if not, fall back to `[Horizontal, Vertical]` whenever a `FoldingFeature` has ever been seen.
- **Deprecation:** WindowManager 1.6.0-alpha03 deprecates `WindowInfoTrackerCallbackAdapter` in favour
  of callbacks on `WindowLayoutInfo`. No .NET binding of 1.6 exists (Window is 1.5.1.3 as of
  2026-06-27). Keep all of this in one file so the swap is local. Add a TODO naming the version to
  revisit.
- `ConfigChanges`: MAUI's default `MainActivity` already handles `ScreenSize | SmallestScreenSize |
  ScreenLayout | Orientation | UiMode`. Without them a fold **restarts the Activity**. Document that
  posture only streams live with those flags. With other activities it re-reads on recreate.

### iOS — `IosScreenPosture`

- Guard everything with `OperatingSystem.IsIOSVersionAtLeast(27, 1)` and `!IsMacCatalyst()`. Below
  27.1, `IsSupported = false` and `Current = None`.
- **Hinge:** attach one `UIHingeInteraction` to the key window's root view
  (`new UIHingeInteraction((interaction, update) => …)`) and read `update.Hinge?.Status / .Angle`.
  `Hinge` is null when the hierarchy doesn't provide updates — treat that as `Unknown`. Re-attach on
  key-window change; `IosPlatform` already tracks the key window for other services.
- **Fold and cameras:** query `rootView.GetReservedRegions(UIViewReservedRegionKind.Division, 0)` and
  `…Occlusion…` on every hinge update **and** on bounds change. Region frames move with the layout,
  not only with the hinge. Hook bounds the same way `IosScreenInfo` does
  (`UIScreen.ModeDidChangeNotification` + orientation); if that misses resizes, use a zero-size
  `UIView` subclass whose `LayoutSubviews` re-queries.
- `Fold` = the active division region's `Frame`, converted with `ConvertRectToView(…, null)` into window
  coordinates. `Orientation` = `Vertical` when the frame is taller than it is wide.
- `State`:
  - `PartiallyOpen` plus an active division region → `HalfOpen`.
  - `FullyOpen` → `Flat`. Fold is null, because there is no active division to avoid.
  - `Closed` → `None`.
- **Confirm the binding's exact C# names.** The strings dump confirms the types exist but not their
  member spelling. In particular, check whether `GetReservedRegions` takes the kind as an `NSString`
  constant or an enum, and how `UIHingeInteraction`'s handler delegate is shaped.

### Blazor WebAssembly — `BrowserScreenPosture`

- `navigator.devicePosture.type` (`"continuous"` / `"folded"`) and its `change` event.
- `window.viewport.segments` (the Viewport Segments API, an array of `DOMRect`) for the fold geometry.
  The fold is the gap between two segments.
- Chromium on Android foldables only. Everywhere else `IsSupported = false`. Hinge status is
  approximate (`folded` → `PartiallyOpen`), and the angle and occlusions are unavailable.
- CSS consumers don't need this service at all: `@media (device-posture: folded)` and
  `env(viewport-segment-*)` work directly. This service is for C# logic.

## Testing

- **Unit:** `ScreenPosture.Segments()` maths (vertical, horizontal, zero-width hinge, fold outside the
  window) and `AbstractScreenPosture` de-dupe and subscribe/unsubscribe.
- **Android (Pixel Fold AVD):** `adb emu posture 2` → `HalfOpen` + `Horizontal` + a fold rect;
  `adb emu posture 3` → `None`; `adb emu fold` → outer screen, `None`. Also run the resizable AVD.
- **iOS (`iPhone-Duo` simulator):** first find out how the simulator drives poses. Not yet checked;
  Apple says Xcode's Device Hub previews them. Then assert `Closed` → `None`, partly open → `HalfOpen` +
  fold rect, fully open → `Flat`.
- Add a page to the existing sample that prints `Current` live and draws the fold rect.
- Follow `plans/device-tests.md` for any automated device run.

## Open questions

1. **The `Rect` type.** Reuse whatever `ScreenMetrics` uses, or add one? Don't pull in
   `Microsoft.Maui.Graphics` to get one.
2. **Per-window posture.** iPad-style multi-window isn't an issue on Duo (new windows open only on the
   inner display, per third-party reports — unconfirmed). Android multi-window gives each Activity its
   own layout info. v1 reports the **current** Activity / key window only.
3. **Expose `IScreenPosture` through `IScreenInfo`, or keep it separate?** Separate keeps
   `IScreenInfo` stable and lets the Android service own the WindowManager dependency. Recommended:
   separate.
4. **Should `Shiny.Maui.Controls` take a dependency on Shiny.Core for this?** It doesn't reference it
   today. The controls plan proposes an optional bridge instead.
5. **AppDeviceBridge.** Normally skipped when a standard web API exists, but the Device Posture and
   Viewport Segments APIs are Chromium-only. A page in `WKWebView` on iPhone Duo gets nothing, so a
   `Shiny.AppDeviceBridge.ScreenPosture` bridge is probably worth it. Decide when the core API lands.

## Definition of done (per `CLAUDE.md`)

readme.md; the `shiny-core` skill (the API plus trigger keywords: foldable, hinge, posture, iPhone Duo,
tabletop); a docs page under `client/core/` plus a release note under `#### Core`; and the
AppDeviceBridge decision above.
