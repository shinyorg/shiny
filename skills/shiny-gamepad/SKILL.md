---
name: shiny-gamepad
description: Generate code using Shiny.Gamepad for cross-platform game controller input - reading sticks, triggers and buttons, handling connect and disconnect, deadzones, rumble/vibration, controller battery, motion sensors and light bars, on Android, iOS, tvOS, Mac Catalyst, macOS, Windows, Linux and Blazor WebAssembly
auto_invoke: true
triggers:
  - gamepad
  - game pad
  - joypad
  - joystick
  - game controller
  - controller input
  - controller support
  - read a controller
  - xbox controller
  - playstation controller
  - dualsense
  - dualshock
  - switch pro controller
  - joy-con
  - siri remote
  - steam deck controller
  - thumbstick
  - analog stick
  - left stick
  - right stick
  - dpad
  - d-pad
  - trigger axis
  - deadzone
  - dead zone
  - stick drift
  - rumble
  - controller vibration
  - haptic feedback controller
  - force feedback
  - controller battery
  - light bar
  - player index
  - player led
  - motion controls
  - gyro aiming
  - IGamepad
  - IGamepadManager
  - GamepadState
  - GamepadButton
  - GamepadAxis
  - GamepadStick
  - GamepadVibration
  - GamepadCapabilities
  - GamepadKind
  - GamepadBattery
  - GamepadMotion
  - GamepadLight
  - GamepadNotSupportedException
  - GamepadDisconnectedException
  - AddGamepads
  - AddNotSupportedGamepads
  - Shiny.Gamepad
  - AndroidGamepadManager
  - BlazorGamepadManager
  - GCController
  - GCExtendedGamepad
  - GCMicroGamepad
  - GCDeviceHaptics
  - GCControllerPlayerIndex
  - GameController framework
  - Windows.Gaming.Input
  - RawGameController
  - GetCurrentReading
  - InputDevice
  - dispatchGenericMotionEvent
  - KEYCODE_BUTTON_A
  - SOURCE_JOYSTICK
  - getAxisValue
  - evdev
  - dev/input/event
  - EVIOCGABS
  - FF_RUMBLE
  - BTN_SOUTH
  - ABS_HAT0X
  - navigator.getGamepads
  - gamepadconnected
  - vibrationActuator
  - W3C Gamepad API
---

# Shiny.Gamepad Skill

You are an expert in Shiny.Gamepad, a cross-platform game controller library covering **reading
sticks, triggers and buttons**, **connect and disconnect**, **rumble**, **controller battery**,
**motion sensors** and **light bars**.

## When to Use This Skill

Invoke this skill when the user wants to:
- Read a game controller from a .NET MAUI, Blazor, console, desktop or tvOS app
- Add controller support to a game or an app that should be navigable without touch
- Handle controllers connecting and disconnecting while the app runs
- Apply a deadzone, or fix a stick that "snaps to eight directions" or drifts
- Rumble a controller, read its battery, use its gyroscope, or set its light bar
- Support two or more players on separate controllers
- Ask why a controller "does nothing" in their browser or on their Raspberry Pi

## ⚠️ Read this before writing anything

Five facts decide most of the design. Getting any of them wrong produces code that looks correct
and fails on real hardware.

1. **Buttons are named by position, not by the label printed on the pad.** `GamepadButton.A` is the
   **bottom face button everywhere**: A on Xbox, Cross on PlayStation, **B on a Nintendo pad**,
   whose face buttons are physically mirrored. `GamepadButton.Y` is the top one. Never write code
   that assumes the letter matches the player's controller - use `IGamepad.Kind` to pick a glyph,
   and nothing else.
2. **Y is positive upwards.** Android, Linux and the browser all report stick Y positive
   *downwards*; every backend here flips it. A stick pushed away from the player always reads
   positive. Do not flip it again.
3. **Nothing happens until `await manager.GetGamepads()` has run once.** That call starts the
   watch. Registering the service touches no hardware at all.
4. **Everything past sticks and buttons is per-controller, not per-platform.** The same DualSense
   reports motion and a light bar on Linux and neither in a browser. Always check
   `IGamepad.Capabilities` - anything missing throws `GamepadNotSupportedException`.
5. **Sticks are raw. Apply a deadzone yourself.** `GetState()` never applies one, because the right
   value depends on the game. A resting thumbstick does not read zero.

## Installation

```bash
# Android, iOS, tvOS, Mac Catalyst, macOS, Windows
dotnet add package Shiny.Gamepad

# Linux desktop / Raspberry Pi - instead of the above
dotnet add package Shiny.Gamepad.Linux

# Blazor WebAssembly - instead of the above
dotnet add package Shiny.Gamepad.Blazor
```

```csharp
// MAUI, or any of the native platform targets
builder.Services.AddGamepads();

// Linux and Blazor register the SAME method name from their own package - do not call both
services.AddGamepads();

// a server, console or test host with no controller API: reports no controllers, never throws
services.AddNotSupportedGamepads();
```

**Never call `AddGamepads()` twice.** The Linux and Blazor packages deliberately reuse the name on
the same target framework, so referencing one of them and calling the base package's method is
ambiguous at compile time.

No permission, entitlement or manifest entry is needed on any platform.

## The two ways to read a controller

Use both. They exist for different jobs and are derived from the same state, so they cannot
disagree.

### Snapshot - for a render loop

```csharp
var pads = await manager.GetGamepads();
var pad = pads.FirstOrDefault();
if (pad == null)
    return;

// every frame
var state = pad.GetState();

var movement = state.LeftStick.WithDeadzone(0.15f);   // or state.GetMovement()
var look = state.RightStick.WithDeadzone(0.15f);      // or state.GetLook()

if (state.IsPressed(GamepadButton.A))
    Jump();

// IsPressed with several buttons means ALL of them - a chord
if (state.IsPressed(GamepadButton.LeftShoulder | GamepadButton.RightShoulder))
    Reload();

// IsAnyPressed means either
if (state.IsAnyPressed(GamepadButton.Start | GamepadButton.Select))
    OpenMenu();

Accelerate(state.RightTrigger);        // 0 to 1
var direction = state.DPad;            // the D-pad as a stick, so it drives the same code
```

`GetState()` never blocks and is safe to call after a disconnect - it keeps returning the last
state seen, so a render loop needs no guard.

### Events - for menus and UI

```csharp
pad.ButtonChanged += (_, e) =>
{
    if (e.Button == GamepadButton.Start && e.IsPressed)
        OpenMenu();

    // e.State is the whole pad at that instant, for reading modifiers
    if (e.Button == GamepadButton.A && e.IsPressed && e.State.IsPressed(GamepadButton.LeftShoulder))
        AlternateAction();
};

pad.AxisChanged += (_, e) =>
{
    if (e.Axis == GamepadAxis.LeftStickY && e.Value > 0.5f && e.PreviousValue <= 0.5f)
        MoveSelectionUp();
};
```

**One event per button, never a combined mask.** Two buttons pressed in the same frame raise two
events.

**Threading:** events are raised on whatever thread the platform delivered input on - the main
thread on Android and in the browser, a background queue on Apple, the reader thread on Linux, the
poll timer on Windows. **Marshal before touching UI.**

```csharp
// MAUI
pad.ButtonChanged += (_, e) => MainThread.BeginInvokeOnMainThread(() => this.Handle(e));
```

## Connect and disconnect

```csharp
manager.Connected += (_, e) => this.Assign(e.Gamepad);
manager.Disconnected += (_, e) => this.Release(e.Gamepad);

// starts the watch; Connected also fires for anything already connected
var pads = await manager.GetGamepads();
```

Keep the list current from the events rather than calling `GetGamepads()` every frame.

A disconnected `IGamepad` stays valid but inert: `IsConnected` is false, `GetState()` returns the
last state, and **everything else throws `GamepadDisconnectedException`**. A controller that comes
back is a **new instance**, not the old one revived.

```csharp
// wait for the first controller - the usual opening move for a game
var pad = await manager.WaitForGamepad(ct);

// "press any button to start"
var pressed = await pad.WaitForButton(ct: ct);
```

## Deadzones - get this right

```csharp
var movement = state.LeftStick.WithDeadzone(0.15f);
```

`WithDeadzone` is **radial** (it looks at total deflection) and **rescales** what is left.

**Never do this:**

```csharp
// WRONG - independent per-axis deadzone
var x = MathF.Abs(state.LeftStick.X) < 0.15f ? 0 : state.LeftStick.X;
var y = MathF.Abs(state.LeftStick.Y) < 0.15f ? 0 : state.LeftStick.Y;
```

That is the classic bug that makes a stick feel like it has eight directions: pushed hard up and
slightly right, the X is discarded and the stick snaps to straight up.

**Also never** cut without rescaling - the value would jump from 0 straight to 0.15 the moment the
stick left the dead area, and slow movement would be impossible.

`Magnitude` can slightly exceed 1 in the diagonals (most hardware reports a square range, so a
corner reads about 1.41). Clamp if that matters.

## Axis events and `AxisChangeThreshold`

```csharp
manager.AxisChangeThreshold = 0.05f;   // default 0.02
```

This is **not** a deadzone. It is how much an axis must move to be worth an *event*, measured
against **the value that axis last reported** - not against the previous state, so a slow sweep
still produces events. It has no effect on `GetState()`, which is always raw. An axis landing
exactly on centre, +1 or -1 always reports, so a stick released never leaves the player walking.

Raise it for menu navigation, lower it for a racing game's steering.

## Rumble

```csharp
if (pad.Supports(GamepadCapabilities.Vibration))
{
    // sets a LEVEL that holds until changed
    await pad.SetVibration(new GamepadVibration(LowFrequency: 0.7f, HighFrequency: 0.2f));

    // ... later, and do not forget this
    await pad.SetVibration(GamepadVibration.Off);
}

// one-shot bump: sets the level, waits, clears it - even if cancelled
await pad.Pulse(0.8f, TimeSpan.FromMilliseconds(120));
```

- `LowFrequency` is the **heavy weight in the left handle** - a low rumble for impacts and engines.
- `HighFrequency` is the **light, fast weight in the right handle** - a buzz for clicks and texture.
- Driving both at once reads as "everything is shaking", which is rarely what a moment calls for.
- `LeftTrigger`/`RightTrigger` need `GamepadCapabilities.TriggerVibration` and are **ignored**
  rather than folded into the handles when unsupported.

**Always stop the motors.** A controller left rumbling keeps going after the app is backgrounded on
some platforms.

## Battery, motion and light

```csharp
if (pad.Supports(GamepadCapabilities.Battery))
{
    var battery = await pad.GetBattery();
    // battery.Level is 0-1 or null; battery.State is Charging/Discharging/Full/Wired/Unknown
}

if (pad.Supports(GamepadCapabilities.Motion))
{
    await pad.SetMotionEnabled(true);          // OFF by default everywhere - sensors cost battery

    pad.MotionChanged += (_, e) => Aim(e.Motion.AngularVelocityX, e.Motion.AngularVelocityY);
    var latest = pad.GetMotion();              // polling counterpart, null until a sample arrives

    await pad.SetMotionEnabled(false);         // turn them off when you stop reading
}

if (pad.Supports(GamepadCapabilities.Light))
    await pad.SetLight(GamepadLight.FromRgb(0, 128, 255));
```

Battery levels are coarse - four steps is common. Treat the level as a gauge to draw, not a number
to do arithmetic on. Acceleration **includes gravity**: a controller resting on a table reads about
1g on whichever axis points up, not zero.

## Identifying controllers

```csharp
pad.Name          // the platform's name for it, often generic
pad.Kind          // Xbox / PlayStation / Nintendo / Steam / Remote / Standard - for glyphs ONLY
pad.PlayerIndex   // the slot the platform assigned, from 1, or null
pad.Id            // unique among connected controllers
pad.SupportedButtons  // tells "not pressed" apart from "not present"
```

**`Id` only survives a reconnect where `GamepadCapabilities.PersistentId` is set** - Windows
(`NonRoamableId`) and Linux (the device's MAC or serial). Apple, Android and the browser hand out a
fresh identity every connection. **Never key saved per-player settings on `Id` without checking
that flag.**

## Platform notes that change what you write

### Android
- Input reaches only the **focused activity**, so the package wraps that activity's window callback
  automatically through Shiny's lifecycle. Nothing is required from the app.
- Events are **observed and passed on** by default, so the D-pad still moves focus and B still goes
  back. For a game, consume them:
  ```csharp
  if (manager is AndroidGamepadManager android)
      android.ConsumeEvents = true;
  ```
- A button held while the app is backgrounded is cleared on focus loss rather than staying stuck.
- Rumble, battery, lights and motion all need **API 31+** and are absent below it.

### Apple (iOS, tvOS, Mac Catalyst, macOS)
- One GameController backend for all four platforms.
- Add `GCSupportsControllerUserInteraction` to Info.plist, and list `GCSupportedGameControllers`.
  Without them the system keeps some buttons - the Home button above all - for itself.
- The **Siri Remote on tvOS** reports `GamepadKind.Remote`: no sticks, no triggers, two buttons,
  with its touch surface mapped onto the left stick so menu code works unchanged.

### Windows
- Needs Windows 10 1903+.
- **Reports every stick centred and every button up while the app is not in the foreground.** That
  is `Windows.Gaming.Input`'s own behaviour and cannot be opted out of.
- No motion and no light bar - the API exposes neither, whatever the controller can do.
- `manager.PollInterval` (default 16ms) applies here and nowhere else. Lower it for a game
  rendering above 60Hz; a poll slower than the frame rate shows up as input lag.

### Linux
- Needs **read access to `/dev/input/event*`**, usually via the `input` group. If `evtest` as root
  sees a controller and the app does not, that is the cause.
- **Rumble additionally needs write access** to the same node; the node is opened read-write when
  permitted and read-only otherwise, so a controller always works and just reports no `Vibration`.
- The **light bar needs write access to its sysfs LED**, which is root-owned by default and needs a
  udev rule.
- Works on X11, Wayland and headless alike.

### Blazor WebAssembly
- **A controller is invisible to the page until the player presses something on it.** This is an
  anti-fingerprinting rule with no permission to grant. Show *"press any button on your
  controller"*, never *"no controller found"*.
  ```csharp
  // tell "no Gamepad API" apart from "no gesture yet"
  if (manager is BlazorGamepadManager blazor)
  {
      var probe = await blazor.Probe();
      if (!probe.Supported)
          ShowUnsupportedBrowserMessage();
  }
  ```
- Vibration needs `vibrationActuator` - Chrome and Edge have it, Firefox and Safari do not.
- No battery, no motion, no lights; none are in the specification.
- `PollInterval` is ignored - the loop is paced by `requestAnimationFrame`.

## Multiple players

```csharp
var pads = await manager.GetGamepads();

// PlayerIndex is the platform's opinion and is null on Windows and in the browser
var p1 = await manager.GetGamepadForPlayer(1);

// assign your own slots where the platform assigns none
var assignments = new Dictionary<string, int>();
manager.Connected += (_, e) => assignments[e.Gamepad.Id] = assignments.Count + 1;
manager.Disconnected += (_, e) => assignments.Remove(e.Gamepad.Id);
```

## Common mistakes

| Mistake | What happens | Do instead |
|---|---|---|
| Assuming `GamepadButton.A` is the button labelled A | Nintendo players press the wrong button | It is always the **bottom** face button; use `Kind` for glyphs |
| Per-axis deadzone | Stick snaps to eight directions | `stick.WithDeadzone(0.15f)` - radial |
| Cutting a deadzone without rescaling | Stick jumps; no slow movement | `WithDeadzone` rescales for you |
| Using raw `GetState()` sticks for movement | Character drifts at rest | Apply a deadzone |
| Calling `GetGamepads()` every frame | Allocates a list per frame | Call once; track via `Connected`/`Disconnected` |
| Never calling `GetGamepads()` | No controllers, no events, ever | It starts the watch |
| `SetVibration` without ever stopping | Controller buzzes indefinitely | `GamepadVibration.Off`, or `Pulse` |
| Saving settings against `Id` | Bindings lost on reconnect | Check `GamepadCapabilities.PersistentId` first |
| Calling a feature without checking `Capabilities` | `GamepadNotSupportedException` | `pad.Supports(...)` first |
| Touching UI from `ButtonChanged` | Cross-thread crash | Marshal to the UI thread |
| "No controller found" in the browser | Misleading - it is waiting for a gesture | "Press any button on your controller" |
| Flipping stick Y | Inverted controls | Already flipped; Y is positive up |
| Reusing a disconnected `IGamepad` | `GamepadDisconnectedException` | Take the new instance from `Connected` |

## Exceptions

| Exception | Meaning |
|---|---|
| `GamepadNotSupportedException` | The controller or platform lacks the capability. Carries the `Capability` flag and names the platform reason |
| `GamepadDisconnectedException` | The controller went away. `GetState()` is exempt and keeps working |
| `GamepadException` | Anything else - the platform refused, or a device node could not be opened |
