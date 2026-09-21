using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Evdev;
using Shiny.Gamepad.Infrastructure;
using Shiny.Gamepad.Sysfs;

namespace Shiny.Gamepad;


/// <summary>
/// A controller as the kernel's evdev interface reports it.
/// </summary>
/// <remarks>
/// <para>evdev pushes input, so nothing polls: a reader thread blocks until the kernel has
/// something and every event updates the accumulated state. Events arrive as a stream of individual
/// button and axis changes terminated by an <c>EV_SYN</c> report, which is the kernel saying "that
/// is one consistent picture" - the state is published on that boundary rather than per event, so a
/// diagonal stick movement is one update rather than two half-applied ones.</para>
/// <para>This is the only backend that reaches the hardware directly rather than through a platform
/// API, which is why it is also the only one that can offer rumble, battery, light bar and motion
/// sensors on the same controller at once. It is equally why each of them depends on permissions:
/// rumble needs write access to the event node, the light bar needs write access to its sysfs LED,
/// and neither is granted to an ordinary user by default on every distribution.</para>
/// </remarks>
class LinuxGamepad : AbstractGamepad
{
    readonly ILogger logger;
    readonly EvdevDevice device;
    readonly EvdevDevice? motionDevice;
    readonly SysfsDevice sysfs;
    readonly GamepadCapabilities capabilities;
    readonly GamepadButton supportedButtons;
    readonly bool triggersOnZAxes;

    GamepadButton buttons;
    GamepadStick leftStick;
    GamepadStick rightStick;
    float leftTrigger;
    float rightTrigger;
    bool dirty;

    float gyroX, gyroY, gyroZ, accelX, accelY, accelZ;
    bool motionRunning;


    public LinuxGamepad(
        string id,
        EvdevDevice device,
        EvdevDevice? motionDevice,
        SysfsDevice sysfs,
        ILogger logger
    ) : base(id, logger)
    {
        this.logger = logger;
        this.device = device;
        this.motionDevice = motionDevice;
        this.sysfs = sysfs;
        this.Name = device.Name;
        this.Kind = DetectKind(device);

        // the kernel gamepad specification puts the triggers on Z and RZ, which collides with the
        // right stick on controllers that predate it and use RX/RY for the sticks instead. A
        // controller declaring RX and RY is using the older layout, so Z and RZ are its triggers;
        // one declaring neither is using Z and RZ for the right stick and has digital triggers.
        this.triggersOnZAxes = device.Axes.Contains(EvdevCodes.ABS_RX) && device.Axes.Contains(EvdevCodes.ABS_RY);

        this.supportedButtons = MapSupportedButtons(device);
        this.capabilities = this.Probe();
    }


    public override string Name { get; }
    public override GamepadKind Kind { get; }
    public override GamepadCapabilities Capabilities => this.capabilities;
    public override GamepadButton SupportedButtons => this.supportedButtons;

    // evdev has no notion of a player slot - the number lit on a controller is set by its driver
    // and never reported back
    public override int? PlayerIndex => null;

    internal string Path => this.device.Path;


    GamepadCapabilities Probe()
    {
        // a controller's uniq is its Bluetooth MAC or USB serial, which is the same string every
        // time it connects and on every reboot - the one platform here besides Windows that can
        // honestly claim a persistent identity
        var caps = this.device.Unique != null ? GamepadCapabilities.PersistentId : GamepadCapabilities.None;

        if (this.device.CanRumble)
            caps |= GamepadCapabilities.Vibration;

        if (this.sysfs.HasBattery)
            caps |= GamepadCapabilities.Battery;

        if (this.sysfs.HasLight)
            caps |= GamepadCapabilities.Light;

        if (this.motionDevice != null)
            caps |= GamepadCapabilities.Motion;

        if (this.device.Buttons.Contains(EvdevCodes.BTN_TOUCHPAD))
            caps |= GamepadCapabilities.Touchpad;

        return caps;
    }


    static GamepadButton MapSupportedButtons(EvdevDevice device)
    {
        var buttons = GamepadButton.None;

        foreach (var code in device.Buttons)
            buttons |= ToButton(code);

        if (device.Axes.Contains(EvdevCodes.ABS_HAT0X))
            buttons |= GamepadButton.DPad;

        if (device.Axes.Contains(EvdevCodes.ABS_Z) || device.Axes.Contains(EvdevCodes.ABS_RZ))
            buttons |= GamepadButton.LeftTrigger | GamepadButton.RightTrigger;

        return buttons == GamepadButton.None ? StandardButtons : buttons;
    }


    static GamepadButton ToButton(ushort code) => code switch
    {
        EvdevCodes.BTN_SOUTH => GamepadButton.A,
        EvdevCodes.BTN_EAST => GamepadButton.B,
        EvdevCodes.BTN_WEST => GamepadButton.X,
        EvdevCodes.BTN_NORTH => GamepadButton.Y,
        EvdevCodes.BTN_TL => GamepadButton.LeftShoulder,
        EvdevCodes.BTN_TR => GamepadButton.RightShoulder,
        EvdevCodes.BTN_TL2 => GamepadButton.LeftTrigger,
        EvdevCodes.BTN_TR2 => GamepadButton.RightTrigger,
        EvdevCodes.BTN_SELECT => GamepadButton.Select,
        EvdevCodes.BTN_START => GamepadButton.Start,
        EvdevCodes.BTN_MODE => GamepadButton.Home,
        EvdevCodes.BTN_THUMBL => GamepadButton.LeftStick,
        EvdevCodes.BTN_THUMBR => GamepadButton.RightStick,
        EvdevCodes.BTN_DPAD_UP => GamepadButton.DPadUp,
        EvdevCodes.BTN_DPAD_DOWN => GamepadButton.DPadDown,
        EvdevCodes.BTN_DPAD_LEFT => GamepadButton.DPadLeft,
        EvdevCodes.BTN_DPAD_RIGHT => GamepadButton.DPadRight,
        EvdevCodes.BTN_TOUCHPAD => GamepadButton.Touchpad,
        _ => GamepadButton.None
    };


    static GamepadKind DetectKind(EvdevDevice device) => device.Id.Vendor switch
    {
        0x045E => GamepadKind.Xbox,
        0x054C => GamepadKind.PlayStation,
        0x057E => GamepadKind.Nintendo,
        0x28DE => GamepadKind.Steam,
        _ => GamepadKind.Standard
    };


    /// <summary>Starts the reader threads for the controller and, where present, its motion node.</summary>
    internal void Start()
    {
        this.device.StartReading(this.OnEvent, ex => this.logger.ReadFailed(this.Id, ex));
        this.Publish();
    }


    void OnEvent(InputEvent e)
    {
        switch (e.Type)
        {
            case EvdevCodes.EV_KEY:
                var button = ToButton(e.Code);
                if (button == GamepadButton.None)
                    return;

                // value 2 is auto-repeat on a held key, which is not a new press
                if (e.Value == 1)
                    this.buttons |= button;
                else if (e.Value == 0)
                    this.buttons &= ~button;
                else
                    return;

                this.dirty = true;
                break;

            case EvdevCodes.EV_ABS:
                this.ReadAxis(e.Code, e.Value);
                break;

            case EvdevCodes.EV_SYN:
                // the kernel has finished describing one consistent state
                if (this.dirty)
                {
                    this.dirty = false;
                    this.Publish();
                }
                break;
        }
    }


    void ReadAxis(ushort code, int value)
    {
        switch (code)
        {
            case EvdevCodes.ABS_X:
                this.leftStick = this.leftStick with { X = this.device.Normalise(code, value, false) };
                break;

            case EvdevCodes.ABS_Y:
                // evdev reports Y positive downwards
                this.leftStick = this.leftStick with { Y = -this.device.Normalise(code, value, false) };
                break;

            case EvdevCodes.ABS_RX:
                this.rightStick = this.rightStick with { X = this.device.Normalise(code, value, false) };
                break;

            case EvdevCodes.ABS_RY:
                this.rightStick = this.rightStick with { Y = -this.device.Normalise(code, value, false) };
                break;

            case EvdevCodes.ABS_Z:
                if (this.triggersOnZAxes)
                    this.leftTrigger = this.device.Normalise(code, value, true);
                else
                    this.rightStick = this.rightStick with { X = this.device.Normalise(code, value, false) };
                break;

            case EvdevCodes.ABS_RZ:
                if (this.triggersOnZAxes)
                    this.rightTrigger = this.device.Normalise(code, value, true);
                else
                    this.rightStick = this.rightStick with { Y = -this.device.Normalise(code, value, false) };
                break;

            case EvdevCodes.ABS_HAT0X:
                this.buttons &= ~(GamepadButton.DPadLeft | GamepadButton.DPadRight);
                if (value < 0)
                    this.buttons |= GamepadButton.DPadLeft;
                else if (value > 0)
                    this.buttons |= GamepadButton.DPadRight;
                break;

            case EvdevCodes.ABS_HAT0Y:
                this.buttons &= ~(GamepadButton.DPadUp | GamepadButton.DPadDown);
                if (value < 0)
                    this.buttons |= GamepadButton.DPadUp;
                else if (value > 0)
                    this.buttons |= GamepadButton.DPadDown;
                break;

            default:
                return;
        }

        this.dirty = true;
    }


    void Publish()
    {
        var all = this.buttons;

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


    public override Task SetVibration(GamepadVibration vibration, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(
            GamepadCapabilities.Vibration,
            this.device.IsWritable
                ? "the controller's driver exposes no FF_RUMBLE effect"
                : $"'{this.device.Path}' could not be opened for writing - add the user to the 'input' group or install a udev rule"
        );

        vibration = vibration.Clamp();

        if (!this.device.SetRumble(vibration.LowFrequency, vibration.HighFrequency))
            this.logger.VibrationFailed(this.Id, new GamepadException("The kernel rejected the force-feedback effect"));

        return Task.CompletedTask;
    }


    public override Task<GamepadBattery> GetBattery(CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Battery, "the controller publishes no power_supply entry under sysfs");

        var battery = this.sysfs.ReadBattery() ?? new GamepadBattery(null, GamepadBatteryState.Unknown);

        return Task.FromResult(battery);
    }


    public override Task SetLight(GamepadLight light, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Light, "the controller publishes no RGB LED under sysfs");

        if (!this.sysfs.WriteLight(light))
            this.logger.LightFailed(this.Id, new GamepadException($"Writing '{this.sysfs.LightDirectory}/multi_intensity' was refused - a udev rule is needed to grant the session write access"));

        return Task.CompletedTask;
    }


    public override Task SetMotionEnabled(bool enabled, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Motion, "the controller exposes no accelerometer node");

        if (enabled)
        {
            if (!this.motionRunning)
            {
                this.motionRunning = true;
                this.motionDevice!.StartReading(this.OnMotionEvent, ex => this.logger.ReadFailed(this.Id, ex));
            }
        }
        else
        {
            // the node has no way to be paused - the kernel pushes samples for as long as it is
            // open - so disabling stops reporting and drops the cached reading. The thread stays,
            // because closing and reopening the node on every toggle is far more expensive than
            // discarding samples, and motion is typically switched on once.
            this.motionRunning = false;
            this.ClearMotion();
        }

        this.logger.MotionToggled(this.Id, enabled ? "enabled" : "disabled");
        return Task.CompletedTask;
    }


    void OnMotionEvent(InputEvent e)
    {
        if (!this.motionRunning || this.motionDevice == null)
            return;

        switch (e.Type)
        {
            case EvdevCodes.EV_ABS:
                // hid-playstation's motion node reports acceleration on X/Y/Z and rotation on
                // RX/RY/RZ, with resolution published per axis: units per g for the accelerometer
                // and units per radian per second for the gyroscope
                switch (e.Code)
                {
                    case EvdevCodes.ABS_X: this.accelX = this.Scale(e.Code, e.Value); break;
                    case EvdevCodes.ABS_Y: this.accelY = this.Scale(e.Code, e.Value); break;
                    case EvdevCodes.ABS_Z: this.accelZ = this.Scale(e.Code, e.Value); break;
                    case EvdevCodes.ABS_RX: this.gyroX = this.Scale(e.Code, e.Value); break;
                    case EvdevCodes.ABS_RY: this.gyroY = this.Scale(e.Code, e.Value); break;
                    case EvdevCodes.ABS_RZ: this.gyroZ = this.Scale(e.Code, e.Value); break;
                    default: return;
                }
                break;

            case EvdevCodes.EV_SYN:
                this.UpdateMotion(new GamepadMotion(
                    this.gyroX, this.gyroY, this.gyroZ,
                    this.accelX, this.accelY, this.accelZ,
                    Environment.TickCount64
                ));
                break;
        }
    }


    float Scale(ushort code, int value)
    {
        var range = this.motionDevice?.GetAxisRange(code);

        // resolution is units per g (or per rad/s); a driver that does not publish one leaves the
        // value raw, which is still usable for relative motion even though the units are unknown
        if (range is { Resolution: > 0 })
            return value / (float)range.Value.Resolution;

        return value;
    }


    protected override void OnDisconnected()
    {
        this.motionRunning = false;
        this.motionDevice?.Dispose();
        this.device.Dispose();
    }
}
