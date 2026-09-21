using Android.Hardware.Lights;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;
using AndroidBatteryState = Android.Hardware.BatteryState;

namespace Shiny.Gamepad;


/// <summary>
/// A controller as Android reports it.
/// </summary>
/// <remarks>
/// <para>Android is the odd platform out: there is no API to read a controller's current state.
/// Input arrives as <c>KeyEvent</c>s and <c>MotionEvent</c>s dispatched to whatever has focus, and
/// nothing can ask a pad what its sticks are doing right now. So the state here is accumulated -
/// key events flip button bits, motion events replace the analog values - and
/// <see cref="AbstractGamepad.GetState"/> returns what has been assembled so far.</para>
/// <para>That has one consequence worth knowing: a button held while the app is backgrounded and
/// released outside it stays held in <see cref="AbstractGamepad.GetState"/>, because the release
/// event went somewhere else. <see cref="Reset"/> is called when the activity loses focus to clear
/// exactly that.</para>
/// <para>Rumble, battery, lights and motion sensors all arrive with API 31. Below that the
/// controller reports none of them even when the hardware has them, because there is no way to
/// reach them.</para>
/// </remarks>
class AndroidGamepad : AbstractGamepad
{
    readonly ILogger logger;
    readonly int deviceId;
    readonly GamepadCapabilities capabilities;
    readonly GamepadButton supportedButtons;

    InputDevice? device;
    GamepadButton buttons;
    GamepadButton hatButtons;
    GamepadStick leftStick;
    GamepadStick rightStick;
    float leftTrigger;
    float rightTrigger;
    LightsManager.LightsSession? lightSession;
    AndroidGamepadSensors? sensors;


    public AndroidGamepad(string id, InputDevice device, ILogger logger) : base(id, logger)
    {
        this.logger = logger;
        this.device = device;
        this.deviceId = device.Id;
        this.Name = device.Name ?? "Gamepad";
        this.Kind = AndroidKeyMap.DetectKind(device);
        this.supportedButtons = AndroidKeyMap.GetSupportedButtons(device);
        this.capabilities = Probe(device);
    }


    public override string Name { get; }
    public override GamepadKind Kind { get; }
    public override GamepadCapabilities Capabilities => this.capabilities;
    public override GamepadButton SupportedButtons => this.supportedButtons;

    // ControllerNumber is 0 when Android has not assigned a slot, and 1-based when it has, which is
    // exactly the contract PlayerIndex wants
    public override int? PlayerIndex => this.device?.ControllerNumber is > 0 and var n ? n : null;

    internal int DeviceId => this.deviceId;


    static GamepadCapabilities Probe(InputDevice device)
    {
        var caps = GamepadCapabilities.None;

        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return caps;

        // VibratorManager is the only way to reach a controller's two motors separately; the older
        // InputDevice.Vibrator is one shared motor and is not worth claiming Vibration for, since
        // a caller would then expect LowFrequency and HighFrequency to do different things
        if (device.VibratorManager?.GetVibratorIds() is { Length: > 0 })
            caps |= GamepadCapabilities.Vibration;

        if (device.BatteryState?.IsPresent == true)
            caps |= GamepadCapabilities.Battery;

        if (device.LightsManager?.Lights is { Count: > 0 })
            caps |= GamepadCapabilities.Light;

        if (AndroidGamepadSensors.IsSupported(device))
            caps |= GamepadCapabilities.Motion;

        return caps;
    }


    /// <summary>Folds a key event into the accumulated state. Returns false for a key that is not a controller button.</summary>
    internal bool HandleKey(KeyEvent e)
    {
        var button = AndroidKeyMap.ToButton(e.KeyCode);
        if (button == GamepadButton.None)
            return false;

        // a held button repeats, and a repeat is not a new press - passing it through would raise a
        // second ButtonChanged for a button that never came up
        if (e.RepeatCount > 0 && e.Action == KeyEventActions.Down)
            return true;

        if (e.Action == KeyEventActions.Down)
            this.buttons |= button;
        else if (e.Action == KeyEventActions.Up)
            this.buttons &= ~button;
        else
            return true;

        this.Publish();
        return true;
    }


    /// <summary>Folds a motion event, and its batched history, into the accumulated state.</summary>
    internal bool HandleMotion(MotionEvent e)
    {
        if (e.ActionMasked != MotionEventActions.Move)
            return false;

        this.device ??= InputDevice.GetDevice(this.deviceId);

        // Android batches samples that arrived between frames into one event's history. Replaying
        // them in order gives every intermediate stick position rather than only where it ended up,
        // which is the difference between a smooth analog sweep and a stair-step.
        for (var i = 0; i < e.HistorySize; i++)
            this.ReadMotion(e, i);

        this.ReadMotion(e, -1);
        return true;
    }


    void ReadMotion(MotionEvent e, int historyPos)
    {
        this.leftStick = new GamepadStick(
            AndroidKeyMap.ReadAxis(e, this.device, Axis.X, historyPos),
            -AndroidKeyMap.ReadAxis(e, this.device, Axis.Y, historyPos)
        );
        this.rightStick = AndroidKeyMap.ReadRightStick(e, this.device, historyPos);
        this.leftTrigger = AndroidKeyMap.ReadTrigger(e, this.device, true, historyPos);
        this.rightTrigger = AndroidKeyMap.ReadTrigger(e, this.device, false, historyPos);
        this.hatButtons = AndroidKeyMap.ReadHat(e, this.device, historyPos);

        this.Publish();
    }


    void Publish()
    {
        var all = this.buttons | this.hatButtons;

        if (this.leftTrigger >= GamepadState.TriggerPressThreshold)
            all |= GamepadButton.LeftTrigger;

        if (this.rightTrigger >= GamepadState.TriggerPressThreshold)
            all |= GamepadButton.RightTrigger;

        this.UpdateState(new GamepadState
        {
            Buttons = all,
            LeftStick = this.leftStick,
            RightStick = this.rightStick,
            LeftTrigger = this.leftTrigger,
            RightTrigger = this.rightTrigger
        });
    }


    /// <summary>
    /// Clears the accumulated state, so nothing stays stuck down after the app stops receiving
    /// input.
    /// </summary>
    /// <remarks>
    /// Called when the activity loses focus. Without it a button held as the player switches apps
    /// is still held when they come back, because the key-up was delivered to whatever took focus.
    /// </remarks>
    internal void Reset()
    {
        this.buttons = GamepadButton.None;
        this.hatButtons = GamepadButton.None;
        this.leftStick = GamepadStick.Neutral;
        this.rightStick = GamepadStick.Neutral;
        this.leftTrigger = 0f;
        this.rightTrigger = 0f;

        this.Publish();
    }


    public override Task SetVibration(GamepadVibration vibration, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Vibration, "the controller exposes no vibrators, or this is below Android 12");

        vibration = vibration.Clamp();

        try
        {
            var manager = this.device?.VibratorManager;
            var ids = manager?.GetVibratorIds();
            if (manager == null || ids is not { Length: > 0 })
                return Task.CompletedTask;

            if (vibration.IsSilent)
            {
                manager.Cancel();
                return Task.CompletedTask;
            }

            // Android exposes a controller's motors as an ordered list with no indication of which
            // is the heavy one. The convention the platform's own samples follow is that the first
            // is the low-frequency motor, so that is what the values are mapped onto; a controller
            // reporting a single vibrator gets the stronger of the two.
            var combination = CombinedVibration.StartParallel()!;

            if (ids.Length == 1)
            {
                var strongest = MathF.Max(vibration.LowFrequency, vibration.HighFrequency);
                combination.AddVibrator(ids[0], BuildEffect(strongest));
            }
            else
            {
                combination.AddVibrator(ids[0], BuildEffect(vibration.LowFrequency));
                combination.AddVibrator(ids[1], BuildEffect(vibration.HighFrequency));
            }

            manager.Vibrate(combination.Combine()!);
        }
        catch (Exception ex)
        {
            this.logger.VibrationFailed(this.Id, ex);
        }

        return Task.CompletedTask;
    }


    static VibrationEffect BuildEffect(float intensity)
    {
        // Android has no "run until told otherwise" effect, so a very long one-shot stands in. It
        // is replaced by the next call and cancelled by SetVibration(Off), so the duration is only
        // reached by an app that set a rumble and then stopped caring - where stopping eventually
        // is better than a controller that buzzes until its battery dies.
        var amplitude = Math.Clamp((int)MathF.Round(intensity * 255f), 1, 255);

        return VibrationEffect.CreateOneShot(60_000, amplitude)!;
    }


    public override Task<GamepadBattery> GetBattery(CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Battery, "the controller reports no battery, or this is below Android 12");

        var battery = this.device?.BatteryState;
        if (battery == null)
            return Task.FromResult(new GamepadBattery(null, GamepadBatteryState.Unknown));

        var state = (BatteryStatus)battery.Status switch
        {
            BatteryStatus.Charging => GamepadBatteryState.Charging,
            BatteryStatus.Discharging => GamepadBatteryState.Discharging,
            BatteryStatus.Full => GamepadBatteryState.Full,
            BatteryStatus.NotCharging => GamepadBatteryState.Wired,
            _ => GamepadBatteryState.Unknown
        };

        // capacity is reported 0-1 already, and is NaN on a device that knows it has a battery but
        // not how full it is
        var level = float.IsFinite(battery.Capacity) ? battery.Capacity : (float?)null;

        return Task.FromResult(new GamepadBattery(level, state));
    }


    public override Task SetLight(GamepadLight light, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Light, "the controller exposes no lights, or this is below Android 12");

        light = light.Clamp();

        try
        {
            var manager = this.device?.LightsManager;
            var lights = manager?.Lights;
            if (manager == null || lights is not { Count: > 0 })
                return Task.CompletedTask;

            // the session has to stay open: Android reverts every light the moment it is closed,
            // so a using block here would set the colour and immediately undo it
            this.lightSession ??= manager.OpenSession();

            var color = Android.Graphics.Color.Argb(
                255,
                (int)MathF.Round(light.Red * 255f),
                (int)MathF.Round(light.Green * 255f),
                (int)MathF.Round(light.Blue * 255f)
            );

            var request = new LightsRequest.Builder();
            foreach (var target in lights)
            {
                // a player-id light cannot show a colour; asking it to would throw, so it is left
                // to the platform, which is already showing the right slot number
                if (target.HasRgbControl)
                    request.AddLight(target, new LightState.Builder().SetColor(color)!.Build()!);
            }

            this.lightSession!.RequestLights(request.Build()!);
        }
        catch (Exception ex)
        {
            this.logger.LightFailed(this.Id, ex);
        }

        return Task.CompletedTask;
    }


    public override Task SetMotionEnabled(bool enabled, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Motion, "the controller exposes no sensors, or this is below Android 12");

        if (enabled)
        {
            this.sensors ??= new AndroidGamepadSensors(this.device!, this.UpdateMotion);
            this.sensors.Start();
        }
        else
        {
            this.sensors?.Stop();
            this.ClearMotion();
        }

        this.logger.MotionToggled(this.Id, enabled ? "enabled" : "disabled");
        return Task.CompletedTask;
    }


    protected override void OnDisconnected()
    {
        this.sensors?.Stop();
        this.sensors = null;

        try
        {
            this.lightSession?.Close();
        }
        catch (Exception ex)
        {
            this.logger.LightFailed(this.Id, ex);
        }

        this.lightSession = null;
        this.device = null;
    }
}
