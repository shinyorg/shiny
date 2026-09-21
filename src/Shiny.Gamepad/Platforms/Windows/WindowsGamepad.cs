using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;
using Windows.Gaming.Input;
using NativeGamepad = Windows.Gaming.Input.Gamepad;
using NativeVibration = Windows.Gaming.Input.GamepadVibration;

namespace Shiny.Gamepad;


/// <summary>
/// A controller as <c>Windows.Gaming.Input</c> reports it.
/// </summary>
/// <remarks>
/// <para>The only backend here that is genuinely polled. <c>Windows.Gaming.Input</c> has no input
/// callback at all - <c>GetCurrentReading</c> is the whole API - so the manager runs a timer and
/// hands each reading to <see cref="AbstractGamepad.UpdateState"/>. The read itself is a shared
/// memory fetch rather than a device round trip, so 60Hz costs very little.</para>
/// <para><c>Windows.Gaming.Input</c> reports nothing while the app is not in the foreground. A
/// minimised or background window reads every stick centred and every button up - not the player's
/// last input, and not an error. There is no way to ask for background input from this API.</para>
/// <para>No motion and no light: the API exposes neither, whatever the controller underneath can
/// do. Reaching a DualSense's gyro or light bar on Windows means speaking HID to it directly, which
/// is a different library.</para>
/// </remarks>
class WindowsGamepad : AbstractGamepad
{
    readonly ILogger logger;
    readonly NativeGamepad pad;
    readonly RawGameController? raw;
    readonly GamepadCapabilities capabilities;


    public WindowsGamepad(string id, NativeGamepad pad, RawGameController? raw, ILogger logger) : base(id, logger)
    {
        this.logger = logger;
        this.pad = pad;
        this.raw = raw;
        this.Name = raw?.DisplayName is { Length: > 0 } name ? name : "Gamepad";
        this.Kind = DetectKind(raw);

        this.capabilities =
            GamepadCapabilities.Vibration |
            GamepadCapabilities.TriggerVibration |
            GamepadCapabilities.PersistentId;

        // TryGetBatteryReport is present on every Gamepad but answers null for a wired pad, which
        // is a correct "there is no battery" rather than a failure - the flag tracks that
        if (pad.TryGetBatteryReport() != null)
            this.capabilities |= GamepadCapabilities.Battery;
    }


    public override string Name { get; }
    public override GamepadKind Kind { get; }
    public override GamepadCapabilities Capabilities => this.capabilities;

    // Windows.Gaming.Input has no player index: the number lit on an Xbox pad is assigned by XInput
    // underneath and is not surfaced here
    public override int? PlayerIndex => null;

    internal NativeGamepad Native => this.pad;


    static GamepadKind DetectKind(RawGameController? raw)
    {
        if (raw == null)
            return GamepadKind.Standard;

        return raw.HardwareVendorId switch
        {
            0x045E => GamepadKind.Xbox,
            0x054C => GamepadKind.PlayStation,
            0x057E => GamepadKind.Nintendo,
            0x28DE => GamepadKind.Steam,

            // anything reaching Windows.Gaming.Input as a Gamepad speaks the Xbox button layout,
            // whatever its shell looks like, so Standard rather than Unknown is the honest answer
            _ => GamepadKind.Standard
        };
    }


    /// <summary>Reads the controller and publishes the result. Called by the manager's poll loop.</summary>
    internal void Poll()
    {
        var reading = this.pad.GetCurrentReading();
        var buttons = GamepadButton.None;

        Map(ref buttons, reading.Buttons, GamepadButtons.A, GamepadButton.A);
        Map(ref buttons, reading.Buttons, GamepadButtons.B, GamepadButton.B);
        Map(ref buttons, reading.Buttons, GamepadButtons.X, GamepadButton.X);
        Map(ref buttons, reading.Buttons, GamepadButtons.Y, GamepadButton.Y);
        Map(ref buttons, reading.Buttons, GamepadButtons.LeftShoulder, GamepadButton.LeftShoulder);
        Map(ref buttons, reading.Buttons, GamepadButtons.RightShoulder, GamepadButton.RightShoulder);
        Map(ref buttons, reading.Buttons, GamepadButtons.LeftThumbstick, GamepadButton.LeftStick);
        Map(ref buttons, reading.Buttons, GamepadButtons.RightThumbstick, GamepadButton.RightStick);
        Map(ref buttons, reading.Buttons, GamepadButtons.DPadUp, GamepadButton.DPadUp);
        Map(ref buttons, reading.Buttons, GamepadButtons.DPadDown, GamepadButton.DPadDown);
        Map(ref buttons, reading.Buttons, GamepadButtons.DPadLeft, GamepadButton.DPadLeft);
        Map(ref buttons, reading.Buttons, GamepadButtons.DPadRight, GamepadButton.DPadRight);
        Map(ref buttons, reading.Buttons, GamepadButtons.Menu, GamepadButton.Start);
        Map(ref buttons, reading.Buttons, GamepadButtons.View, GamepadButton.Select);
        Map(ref buttons, reading.Buttons, GamepadButtons.Paddle1, GamepadButton.Paddle1);
        Map(ref buttons, reading.Buttons, GamepadButtons.Paddle2, GamepadButton.Paddle2);
        Map(ref buttons, reading.Buttons, GamepadButtons.Paddle3, GamepadButton.Paddle3);
        Map(ref buttons, reading.Buttons, GamepadButtons.Paddle4, GamepadButton.Paddle4);

        if (reading.LeftTrigger >= GamepadState.TriggerPressThreshold)
            buttons |= GamepadButton.LeftTrigger;

        if (reading.RightTrigger >= GamepadState.TriggerPressThreshold)
            buttons |= GamepadButton.RightTrigger;

        this.UpdateState(new GamepadState
        {
            Buttons = buttons,
            LeftStick = new GamepadStick((float)reading.LeftThumbstickX, (float)reading.LeftThumbstickY),
            RightStick = new GamepadStick((float)reading.RightThumbstickX, (float)reading.RightThumbstickY),
            LeftTrigger = (float)reading.LeftTrigger,
            RightTrigger = (float)reading.RightTrigger
        });
    }


    static void Map(ref GamepadButton buttons, GamepadButtons reading, GamepadButtons flag, GamepadButton button)
    {
        if ((reading & flag) == flag)
            buttons |= button;
    }


    public override Task SetVibration(GamepadVibration vibration, CancellationToken ct = default)
    {
        this.AssertConnected();
        vibration = vibration.Clamp();

        try
        {
            this.pad.Vibration = new NativeVibration
            {
                LeftMotor = vibration.LowFrequency,
                RightMotor = vibration.HighFrequency,
                LeftTrigger = vibration.LeftTrigger,
                RightTrigger = vibration.RightTrigger
            };
        }
        catch (Exception ex)
        {
            this.logger.VibrationFailed(this.Id, ex);
        }

        return Task.CompletedTask;
    }


    public override Task<GamepadBattery> GetBattery(CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Battery, "the controller is wired, or Windows reports no battery for it");

        var report = this.pad.TryGetBatteryReport();
        if (report == null)
            return Task.FromResult(new GamepadBattery(null, GamepadBatteryState.Wired));

        var state = report.Status switch
        {
            Windows.System.Power.BatteryStatus.Charging => GamepadBatteryState.Charging,
            Windows.System.Power.BatteryStatus.Discharging => GamepadBatteryState.Discharging,
            Windows.System.Power.BatteryStatus.Idle => GamepadBatteryState.Full,
            Windows.System.Power.BatteryStatus.NotPresent => GamepadBatteryState.Wired,
            _ => GamepadBatteryState.Unknown
        };

        // Windows reports charge in mWh against a full-charge capacity, not as a percentage, and
        // an Xbox pad reports the pair as whole numbers of a four-step gauge
        float? level = report is { RemainingCapacityInMilliwattHours: { } remaining, FullChargeCapacityInMilliwattHours: { } full } && full > 0
            ? Math.Clamp((float)remaining / full, 0f, 1f)
            : null;

        return Task.FromResult(new GamepadBattery(level, state));
    }


    public override Task SetLight(GamepadLight light, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Light, "Windows.Gaming.Input exposes no light API - reaching one means speaking HID to the controller directly");

        return Task.CompletedTask;
    }


    public override Task SetMotionEnabled(bool enabled, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Motion, "Windows.Gaming.Input exposes no motion sensors - reaching them means speaking HID to the controller directly");

        return Task.CompletedTask;
    }
}
