using Foundation;
using GameController;
using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad;


/// <summary>
/// A controller as GameController reports it, on iOS, tvOS, Mac Catalyst or macOS.
/// </summary>
/// <remarks>
/// <para>GameController pushes input rather than being polled, so nothing here runs on a timer:
/// <c>ValueChangedHandler</c> fires when something moves and the whole profile is read off in one
/// go. Apple coalesces elements that changed together into a single call, which is why reading
/// everything is cheaper than it looks and gives a consistent snapshot rather than a mix of two
/// moments.</para>
/// <para>tvOS's Siri Remote arrives as a <c>GCMicroGamepad</c> with no sticks, no triggers and two
/// buttons. It is handled here rather than excluded - a remote is still a controller - and reports
/// <see cref="GamepadKind.Remote"/> with its touch surface mapped onto the D-pad.</para>
/// </remarks>
class AppleGamepad : AbstractGamepad
{
    readonly ILogger logger;
    readonly GCController controller;
    readonly GamepadCapabilities capabilities;
    readonly GamepadButton supportedButtons;

    AppleHapticsChannel? lowFrequency;
    AppleHapticsChannel? highFrequency;
    AppleHapticsChannel? leftTrigger;
    AppleHapticsChannel? rightTrigger;
    bool hapticsOpened;


    public AppleGamepad(string id, GCController controller, ILogger logger) : base(id, logger)
    {
        this.logger = logger;
        this.controller = controller;
        this.Name = controller.VendorName ?? "Gamepad";
        this.Kind = DetectKind(controller);
        (this.capabilities, this.supportedButtons) = Probe(controller, this.Kind);

        this.Hook();
    }


    public override string Name { get; }
    public override GamepadKind Kind { get; }
    public override GamepadCapabilities Capabilities => this.capabilities;
    public override GamepadButton SupportedButtons => this.supportedButtons;

    public override int? PlayerIndex => this.controller.PlayerIndex == GCControllerPlayerIndex.Unset
        ? null
        : (int)this.controller.PlayerIndex + 1;


    void Hook()
    {
        if (this.controller.ExtendedGamepad is { } extended)
        {
            extended.ValueChangedHandler = (pad, _) => this.Read(pad);
            this.Read(extended);
        }
        else if (this.controller.MicroGamepad is { } micro)
        {
            micro.ValueChangedHandler = (pad, _) => this.ReadMicro(pad);
            this.ReadMicro(micro);
        }
    }


    void Read(GCExtendedGamepad pad)
    {
        var buttons = GamepadButton.None;

        Set(ref buttons, GamepadButton.A, pad.ButtonA);
        Set(ref buttons, GamepadButton.B, pad.ButtonB);
        Set(ref buttons, GamepadButton.X, pad.ButtonX);
        Set(ref buttons, GamepadButton.Y, pad.ButtonY);
        Set(ref buttons, GamepadButton.LeftShoulder, pad.LeftShoulder);
        Set(ref buttons, GamepadButton.RightShoulder, pad.RightShoulder);
        Set(ref buttons, GamepadButton.LeftStick, pad.LeftThumbstickButton);
        Set(ref buttons, GamepadButton.RightStick, pad.RightThumbstickButton);
        Set(ref buttons, GamepadButton.DPadUp, pad.DPad.Up);
        Set(ref buttons, GamepadButton.DPadDown, pad.DPad.Down);
        Set(ref buttons, GamepadButton.DPadLeft, pad.DPad.Left);
        Set(ref buttons, GamepadButton.DPadRight, pad.DPad.Right);
        Set(ref buttons, GamepadButton.Start, pad.ButtonMenu);
        Set(ref buttons, GamepadButton.Select, pad.ButtonOptions);
        Set(ref buttons, GamepadButton.Home, pad.ButtonHome);

        // the touchpad click lives on the concrete subclass, not on GCExtendedGamepad
        switch (pad)
        {
            case GCDualSenseGamepad dualSense:
                Set(ref buttons, GamepadButton.Touchpad, dualSense.TouchpadButton);
                break;

            case GCDualShockGamepad dualShock:
                Set(ref buttons, GamepadButton.Touchpad, dualShock.TouchpadButton);
                break;
        }

        var leftTriggerValue = pad.LeftTrigger?.Value ?? 0f;
        var rightTriggerValue = pad.RightTrigger?.Value ?? 0f;

        if (leftTriggerValue >= GamepadState.TriggerPressThreshold)
            buttons |= GamepadButton.LeftTrigger;

        if (rightTriggerValue >= GamepadState.TriggerPressThreshold)
            buttons |= GamepadButton.RightTrigger;

        this.UpdateState(new GamepadState
        {
            Buttons = buttons,
            LeftStick = new GamepadStick(pad.LeftThumbstick.XAxis.Value, pad.LeftThumbstick.YAxis.Value),
            RightStick = new GamepadStick(pad.RightThumbstick.XAxis.Value, pad.RightThumbstick.YAxis.Value),
            LeftTrigger = leftTriggerValue,
            RightTrigger = rightTriggerValue
        });
    }


    void ReadMicro(GCMicroGamepad pad)
    {
        var buttons = GamepadButton.None;

        Set(ref buttons, GamepadButton.A, pad.ButtonA);
        Set(ref buttons, GamepadButton.X, pad.ButtonX);
        Set(ref buttons, GamepadButton.Start, pad.ButtonMenu);
        Set(ref buttons, GamepadButton.DPadUp, pad.Dpad.Up);
        Set(ref buttons, GamepadButton.DPadDown, pad.Dpad.Down);
        Set(ref buttons, GamepadButton.DPadLeft, pad.Dpad.Left);
        Set(ref buttons, GamepadButton.DPadRight, pad.Dpad.Right);

        // the remote's touch surface is analog, and is the only stick-like input it has - reporting
        // it as the left stick lets menu code written for a gamepad work unchanged on a Siri Remote
        this.UpdateState(new GamepadState
        {
            Buttons = buttons,
            LeftStick = new GamepadStick(pad.Dpad.XAxis.Value, pad.Dpad.YAxis.Value)
        });
    }


    static void Set(ref GamepadButton buttons, GamepadButton button, GCControllerButtonInput? input)
    {
        if (input?.IsPressed == true)
            buttons |= button;
    }


    static GamepadKind DetectKind(GCController controller)
    {
        // the concrete profile class is authoritative where Apple provides one; the product
        // category string covers everything else and is the only signal for a remote
        if (controller.ExtendedGamepad is GCDualSenseGamepad or GCDualShockGamepad)
            return GamepadKind.PlayStation;

        if (controller.ExtendedGamepad is GCXboxGamepad)
            return GamepadKind.Xbox;

        var category = controller.ProductCategory;
        if (string.IsNullOrWhiteSpace(category))
            return controller.ExtendedGamepad == null ? GamepadKind.Remote : GamepadKind.Standard;

        if (category.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("DualShock", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.PlayStation;

        if (category.Contains("Xbox", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Xbox;

        if (category.Contains("Switch", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Nintendo", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("Joy-Con", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Nintendo;

        if (category.Contains("Remote", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Remote;

        return controller.ExtendedGamepad == null ? GamepadKind.Remote : GamepadKind.Standard;
    }


    static (GamepadCapabilities, GamepadButton) Probe(GCController controller, GamepadKind kind)
    {
        var caps = GamepadCapabilities.None;
        var buttons = StandardButtons;

        if (controller.Battery != null)
            caps |= GamepadCapabilities.Battery;

        if (controller.Light != null)
            caps |= GamepadCapabilities.Light;

        if (controller.Motion != null)
            caps |= GamepadCapabilities.Motion;

        if (controller.Haptics is { } haptics)
        {
            var localities = haptics.SupportedLocalities;

            // "can it rumble" means a handle or the default locality; the triggers are a separate
            // claim because driving a trigger motor is not the same effect at all
            if (Has(localities, GCHapticsLocality.LeftHandle) ||
                Has(localities, GCHapticsLocality.RightHandle) ||
                Has(localities, GCHapticsLocality.Handles) ||
                Has(localities, GCHapticsLocality.Default))
                caps |= GamepadCapabilities.Vibration;

            if (Has(localities, GCHapticsLocality.LeftTrigger) || Has(localities, GCHapticsLocality.RightTrigger))
                caps |= GamepadCapabilities.TriggerVibration;
        }

        switch (controller.ExtendedGamepad)
        {
            case GCDualSenseGamepad or GCDualShockGamepad:
                caps |= GamepadCapabilities.Touchpad;
                buttons |= GamepadButton.Touchpad;
                break;
        }

        if (controller.ExtendedGamepad?.ButtonHome != null)
            buttons |= GamepadButton.Home;

        if (kind == GamepadKind.Remote)
            // a remote has none of the shoulders, triggers, stick clicks or a second stick
            buttons = GamepadButton.A | GamepadButton.X | GamepadButton.Start | GamepadButton.DPad;

        return (caps, buttons);
    }


    static bool Has(NSSet<NSString> localities, NSString locality) => localities.Contains(locality);


    public override Task SetVibration(GamepadVibration vibration, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Vibration, "the controller reports no haptic localities to Core Haptics");

        vibration = vibration.Clamp();
        this.EnsureHaptics();

        // the heavy motor is the left handle and the light one the right, matching how every other
        // platform lays its two motors out, so the same GamepadVibration feels the same everywhere
        this.lowFrequency?.SetIntensity(vibration.LowFrequency);
        this.highFrequency?.SetIntensity(vibration.HighFrequency);

        if ((this.capabilities & GamepadCapabilities.TriggerVibration) != 0)
        {
            this.leftTrigger?.SetIntensity(vibration.LeftTrigger);
            this.rightTrigger?.SetIntensity(vibration.RightTrigger);
        }

        return Task.CompletedTask;
    }


    void EnsureHaptics()
    {
        if (this.hapticsOpened || this.controller.Haptics is not { } haptics)
            return;

        this.hapticsOpened = true;
        var localities = haptics.SupportedLocalities;

        // a controller with one shared motor exposes Default but neither handle; driving it from
        // the stronger of the two values keeps a single-motor pad responsive rather than silent
        if (Has(localities, GCHapticsLocality.LeftHandle) && Has(localities, GCHapticsLocality.RightHandle))
        {
            this.lowFrequency = AppleHapticsChannel.TryCreate(this.logger, this.Id, haptics, GCHapticsLocality.LeftHandle);
            this.highFrequency = AppleHapticsChannel.TryCreate(this.logger, this.Id, haptics, GCHapticsLocality.RightHandle);
        }
        else
        {
            var shared = AppleHapticsChannel.TryCreate(this.logger, this.Id, haptics, GCHapticsLocality.Default);
            this.lowFrequency = shared;
            this.highFrequency = shared;
        }

        if (Has(localities, GCHapticsLocality.LeftTrigger))
            this.leftTrigger = AppleHapticsChannel.TryCreate(this.logger, this.Id, haptics, GCHapticsLocality.LeftTrigger);

        if (Has(localities, GCHapticsLocality.RightTrigger))
            this.rightTrigger = AppleHapticsChannel.TryCreate(this.logger, this.Id, haptics, GCHapticsLocality.RightTrigger);
    }


    public override Task<GamepadBattery> GetBattery(CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Battery, "GameController reports no battery for this controller");

        var battery = this.controller.Battery!;
        var state = battery.BatteryState switch
        {
            GCDeviceBatteryState.Charging => GamepadBatteryState.Charging,
            GCDeviceBatteryState.Discharging => GamepadBatteryState.Discharging,
            GCDeviceBatteryState.Full => GamepadBatteryState.Full,
            _ => GamepadBatteryState.Unknown
        };

        return Task.FromResult(new GamepadBattery(battery.BatteryLevel, state));
    }


    public override Task SetLight(GamepadLight light, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Light, "GameController reports no light for this controller");

        light = light.Clamp();
        this.controller.Light!.Color = new GCColor(light.Red, light.Green, light.Blue);

        return Task.CompletedTask;
    }


    public override Task SetMotionEnabled(bool enabled, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Motion, "GameController reports no motion sensors for this controller");

        var motion = this.controller.Motion!;

        // most controllers hand over readings the moment a handler is attached; a few (DualSense
        // over Bluetooth) keep the sensors asleep until asked, and asking a controller that does
        // not need it is harmless
        if (motion.SensorsRequireManualActivation)
            motion.SensorsActive = enabled;

        if (enabled)
        {
            motion.ValueChangedHandler = m => this.UpdateMotion(new GamepadMotion(
                (float)m.RotationRate.X,
                (float)m.RotationRate.Y,
                (float)m.RotationRate.Z,
                (float)m.Acceleration.X,
                (float)m.Acceleration.Y,
                (float)m.Acceleration.Z,
                Environment.TickCount64
            ));
        }
        else
        {
            motion.ValueChangedHandler = null;
            this.ClearMotion();
        }

        this.logger.MotionToggled(this.Id, enabled ? "enabled" : "disabled");
        return Task.CompletedTask;
    }


    protected override void OnDisconnected()
    {
        if (this.controller.ExtendedGamepad is { } extended)
            extended.ValueChangedHandler = null;

        if (this.controller.MicroGamepad is { } micro)
            micro.ValueChangedHandler = null;

        if (this.controller.Motion is { } motion)
            motion.ValueChangedHandler = null;

        // highFrequency can be the same channel as lowFrequency on a single-motor pad, so dispose
        // by reference identity rather than twice
        this.lowFrequency?.Dispose();
        if (!ReferenceEquals(this.highFrequency, this.lowFrequency))
            this.highFrequency?.Dispose();

        this.leftTrigger?.Dispose();
        this.rightTrigger?.Dispose();

        this.lowFrequency = this.highFrequency = this.leftTrigger = this.rightTrigger = null;
    }


    /// <summary>The native controller, so a disconnect notification can be matched to its gamepad.</summary>
    internal GCController Controller => this.controller;
}
