# Plan: Fold modes (closed, tablet, book, tabletop/laptop, tent)

Status: **proposed (2026-11-12)**. Nothing implemented. Builds on `plans/screen-posture.md` (`IScreenPosture`, implemented).
Last updated: 2026-11-12

## Summary

`IScreenPosture` answers the layout question: *is there a fold in my window, and where is it?* Apps that want
to react to how the device is being **used** ask something else: *is it closed, open like a tablet, held like a
book, standing like a laptop or alarm clock, or tented?* No OS reports that. Android WindowManager gives
`FLAT` / `HALF_OPENED` plus an orientation. iOS gives `UIHingeStatus` plus an angle. Neither says anything
about gravity, and neither can tell a tent from a closed device.

Add an **opt-in `IFoldModeMonitor`** to `Shiny.Core`. It combines three signals into one debounced
`FoldMode`:

1. `IScreenPosture` (what we already have),
2. the **hinge angle**, already on iOS and new on Android through `Sensor.TYPE_HINGE_ANGLE`, and
3. **gravity**, which says whether the device is resting on a surface and which half is down.

The raw `ScreenPosture` stays exactly as it is. `screen-posture.md` says layout must come from `State` and
`Fold`, and that still holds. `FoldMode` is for behaviour (show a clock, switch to a keyboard, start a
presentation), not for placing panes.

## The modes

| `FoldMode` | Physical | How it is detected |
|---|---|---|
| `Unknown` | Not a foldable, or nothing known yet | `!IsSupported`, or no hinge or fold ever seen (`SupportedPostures` empty, hinge `Unknown`) |
| `Closed` | Shut; the app is on the outer or front screen | hinge `Closed`, or angle < `ClosedMaxAngle` (default 20°), or `State == None` on a device known to fold |
| `Flat` | Open flat, used as a tablet | `State == Flat`, or angle ≥ `FlatMinAngle` (default 165°) |
| `Book` | Half open, fold runs top to bottom | `ScreenPosture.IsBook` |
| `Tabletop` | Half open, fold runs left to right: laptop / keyboard mode, alarm clock, video call stand | `ScreenPosture.IsTabletop` |
| `Tent` | Folded **past** 180°, standing as an upside-down V, one screen facing out | angle in `TentMinAngle..FoldedBackMinAngle` (default 190–330°) and resting with the hinge on top |
| `FoldedBack` | Folded all the way back, used single-screen (360° hinges) | angle ≥ `FoldedBackMinAngle` |

Alongside the mode, the reading carries `IsResting` (`bool?`): gravity has been steady and one half is close to
level for `SettleTime`. It is null when there is no gravity source, such as the iOS simulator or the browser.

### Laptop vs alarm clock

**These are the same physical posture, and no sensor tells them apart.** Both are `Tabletop` with the bottom
half resting. Which one it is depends on what the app shows: a keyboard in the bottom half, or a clock in the
top half. We report `Tabletop` + `IsResting = true` and let the app decide. The docs should say this plainly,
so nobody waits for a `Laptop` value that will never be reported.

### Tent depends on the hardware

Book-style foldables with an inner screen (Galaxy Z Fold, Pixel Fold, and very likely iPhone Duo) stop at
180°, so they never report `Tent` or `FoldedBack`. Those two only appear on 360° hinges (Surface Duo-class
dual-screen devices) and only where the platform reports angles above 180°. See open question 1.

## API

```csharp
namespace Shiny;

public enum FoldMode { Unknown, Closed, Flat, Book, Tabletop, Tent, FoldedBack }

/// <param name="Mode">The debounced mode.</param>
/// <param name="IsResting">Steady gravity with one half level; null when no gravity source is available.</param>
/// <param name="HingeAngleDegrees">The latest hinge angle, or null if the platform has none.</param>
/// <param name="Posture">The <see cref="ScreenPosture"/> the mode was derived from, for layout.</param>
public sealed record FoldModeReading(FoldMode Mode, bool? IsResting, double? HingeAngleDegrees, ScreenPosture Posture)
{
    public static FoldModeReading Unknown { get; }
}

public interface IFoldModeMonitor
{
    bool IsSupported { get; }                         // IScreenPosture.IsSupported
    FoldModeReading Current { get; }
    event EventHandler<FoldModeReading>? Changed;     // raised only when Mode or IsResting changes, on the main thread
}

public sealed class FoldModeOptions
{
    public double ClosedMaxAngle { get; set; } = 20;
    public double FlatMinAngle { get; set; } = 165;
    public double TentMinAngle { get; set; } = 190;
    public double FoldedBackMinAngle { get; set; } = 330;
    public double Hysteresis { get; set; } = 10;                      // degrees either side of each threshold
    public TimeSpan SettleTime { get; set; } = TimeSpan.FromMilliseconds(300);
    public bool UseGravity { get; set; } = true;                      // false = posture + angle only, no motion sensor
}

// registration - opt-in, not added by hosts
services.AddFoldModes(o => o.SettleTime = TimeSpan.FromMilliseconds(500));
```

`Changed` fires only when `Mode` or `IsResting` changes, never for a different angle alone. That keeps it
cheap to bind UI to. Apps that want the live angle should read `IScreenPosture`.

Why it is **opt-in** and not registered by every host like `IScreenPosture`: it runs a motion sensor while it has
subscribers, so it should be something an app asks for. The motion sensor still only runs while `Changed`
has subscribers, the same subscribe-on-first-handler rule `AbstractScreenPosture` uses.

## Implementation

### Phase 1: hinge angle on Android (`AndroidScreenPosture`)

This fills a gap in the existing API and is worth shipping on its own.

- Read `Sensor.TYPE_HINGE_ANGLE` (API 30+) through `SensorManager.GetDefaultSensor(SensorType.HingeAngle)`. Null
  means no hinge sensor, so `HingeAngleDegrees` stays null as it is today.
- Register the listener inside the existing `Watch(refresh)`, next to the WindowManager listener, at
  `SensorDelay.Ui`. Unregister when the watch is disposed. The hinge sensor reports on change, so this is cheap.
- Round to whole degrees, as `UIKit27.ReadStatus` does on iOS, so sensor noise doesn't flood `Changed`.
- Also use the angle **while the fold is not in the window**, where `Hinge` is `Unknown` today. Map angle ≤
  `ClosedMaxAngle` → `HingeStatus.Closed`, ≥ 175 → `FullyOpen`, otherwise `PartiallyOpen`. This means a closed Pixel
  Fold on its outer screen finally reports `Hinge = Closed`, not `Unknown`.
- `ReadPosture()` outside a watch: read the last cached sensor value if one exists. Don't register a sensor just to
  answer a property read.
- Update the `HingeAngleDegrees` and `HingeStatus` XML docs ("iOS only" → "iOS and Android with a hinge sensor").

### Phase 2: gravity source (internal)

```
src/Shiny.Core/Screen/IGravitySource.cs                    internal: Start(Action<GravityVector>) → IDisposable
src/Shiny.Core/Platforms/Android/AndroidGravitySource.cs  Sensor.TYPE_GRAVITY, fallback TYPE_ACCELEROMETER + low-pass
src/Shiny.Core/Platforms/Apple/IosGravitySource.cs        CMMotionManager.StartDeviceMotionUpdates → DeviceMotion.Gravity
src/Shiny.Core/Screen/NullGravitySource.cs                every other TFM, and the browser for v1
```

- No permissions are needed. iOS `NSMotionUsageDescription` only covers `CMMotionActivity` / pedometer.
  Android's `HIGH_SAMPLING_RATE_SENSORS` only matters above 200 Hz, and we sample at ~16 Hz (`SensorDelay.Ui`).
- Report gravity in **window coordinates**: rotate the device-frame vector by the current display rotation
  (`Display.Rotation` on Android, the window scene's interface orientation on iOS). Then "the fold is horizontal in
  the window" and "gravity points at the bottom of the window" use the same axes.
- **Which half holds the sensor.** On a foldable, the accelerometer sits in one half only. The other half's tilt is
  that vector rotated about the hinge axis by `180 - angle`. Phase 3 only needs (a) whether gravity is steady and
  (b) whether *either* half is level, so it can try both halves and not need a per-device table. See open
  question 2.
- iOS simulator and Mac Catalyst: `CMMotionManager.DeviceMotionAvailable` is false, so it falls back to the null
  source and `IsResting` is null.

### Phase 3: the classifier (pure, portable, unit-tested)

```
src/Shiny.Core/Screen/FoldMode.cs             enum + FoldModeReading + FoldModeOptions
src/Shiny.Core/Screen/FoldModeClassifier.cs   no platform code; compiled and tested on net10.0
```

- Input: `(ScreenPosture posture, double? angle, GravityVector? gravity, DateTimeOffset timestamp)`.
  Output: `FoldModeReading`.
- **Raw mode, in order:** `Unknown` → `Closed` → `FoldedBack` → `Tent` → `Flat` → `Book` → `Tabletop`, using the rules
  in the table. When there's an angle, it decides open, closed and flat. Without one (iOS updates can be sparse,
  older Android), fall back to `State` / `Hinge` / `Orientation` alone.
- **Hysteresis:** once in a mode, leave an angle-defined boundary only after crossing it by `Hysteresis` degrees.
  This stops the mode flickering between `Tabletop` and `Flat` around 165–175°.
- **Debounce:** a new raw mode must hold for `SettleTime` before it is published. The classifier takes the timestamp
  as input, and the service owns the timer, so tests stay deterministic.
- **Resting:** keep a short window of gravity samples (~1 s). It is resting when the magnitude variance is below a
  threshold and one half's normal is within ~15° of vertical.
- **Tent:** angle in the tent band, *and* (when gravity is available) neither half level, with the hinge axis
  roughly horizontal and above both halves. Without gravity, the angle band alone decides.

### Phase 4: the service

```
src/Shiny.Core/Screen/FoldModeMonitor.cs        IFoldModeMonitor over IScreenPosture + IGravitySource + classifier
src/Shiny.Core/Screen/FoldModeExtensions.cs     AddFoldModes(this IServiceCollection, Action<FoldModeOptions>?)
```

- On the first `Changed` subscriber: subscribe to `IScreenPosture.Changed` (which brings in the angle from Phase 1)
  and start gravity if `UseGravity` is set. Stop both with the last subscriber.
- Gravity is only started while the posture is not `Closed` or `Unknown`. A closed phone in a pocket shouldn't
  keep the accelerometer running. Exception: the tent and folded-back bands, which come *from* the angle, so they
  don't need gravity to be detected first.
- Raise `Changed` on the main thread, as `IScreenPosture` does on Android and iOS.
- `AddFoldModes` uses `TryAdd`, so calling it twice is harmless. `IGravitySource` gets one `#if` branch per platform,
  next to the posture registration in `ShinyInfrastructureExtensions.cs`, but `IFoldModeMonitor` itself is only
  added by `AddFoldModes`.

### Phase 5: sample, docs, skill, bridge

- `samples/Sample.Shared.Maui/Pages/Device/PosturePage`: add the live `FoldMode`, `IsResting` and angle, plus a
  demo that switches the bottom half to a keyboard in `Tabletop` and a clock when resting and idle.
- Definition of done per `CLAUDE.md`: readme.md; the `shiny-core` skill (API, the "laptop vs alarm clock is
  the app's choice" rule, triggers: fold mode, tent, laptop mode, tabletop, flex mode, hinge angle); docs page
  `client/core/` (extend the posture page, don't add a new one); release notes under `#### Core`, covering the Android
  hinge angle (enhancement) and fold modes (feature).
- AppDeviceBridge: decide together with the still-open posture bridge from `screen-posture.md`. A `FoldMode` stream
  is exactly what a WebView page can't get from web APIs (Device Posture has no angle and no gravity), so it
  probably belongs in the same bridge.

## Testing

- **Unit (`tests/Shiny.Tests/FoldModeClassifierTests.cs`):** table tests for each mode; hysteresis at each
  threshold (oscillate 160↔170°, expect one transition); debounce (a 100 ms blip publishes nothing); missing
  angle and missing gravity fallbacks; tent with and without gravity. Add **recorded traces**: capture
  `(t, posture, angle, gravity)` CSV from a real device in Phase 5's sample page and replay them as fixtures.
- **Android (Pixel Fold AVD):** `adb emu posture 1/2/3` as before. Drive the hinge with
  `adb emu sensor set hinge-angle0 <deg>` (**check the emulator exposes it**; the extended controls window has a
  hinge slider for foldable AVDs) and gravity with `adb emu sensor set acceleration x:y:z`. Expect closed → `Closed`,
  90° + horizontal → `Tabletop`, 90° + vertical → `Book`, 180° → `Flat`.
- **iOS (iPhone Duo simulator):** posture-only path (no CoreMotion in the simulator); `IsResting` must be null.
  As in `screen-posture.md`, hinge transitions may not be drivable headless, so try the Device Hub UI. The gravity
  path needs a device.
- **Trim/AOT:** `Build.slnf` stays at zero IL warnings.

## Open questions

1. **Does iPhone Duo's `UIHinge.angle` ever exceed π (180°)?** If the hinge stops at 180, `Tent` / `FoldedBack` are
   Android 360°-hinge only, and the docs should say so. Check on the simulator's hinge range or the API docs.
2. **Accelerometer placement on Android foldables.** The "try both halves" approach avoids needing to know it for
   resting detection, but tent apex detection is cleaner with it. Check whether the CDD defines the sensor frame for
   foldables (it may be tied to the default display) before adding any per-device data. Never ship a device table.
3. **Name.** `FoldMode` vs `DevicePosture`. `DevicePosture` reads naturally but sits too close to `ScreenPosture`
   and Android's `SupportedPosture`. Recommended: `FoldMode`.
4. **Should angle-only changes keep raising `IScreenPosture.Changed`?** iOS already does (rounded to whole degrees).
   Phase 1 makes Android match. If layout consumers complain, add a coarser `IScreenPosture` mode later. Don't
   pre-empt it.
5. **Browser.** The Generic Sensor API (`GravitySensor`) exists in Chromium but there is no hinge angle, so the
   browser would only get `Closed` (from `continuous` with segments absent, which is unreliable), `Book` and
   `Tabletop`. v1: browser gets the posture-only classifier with the null gravity source.
