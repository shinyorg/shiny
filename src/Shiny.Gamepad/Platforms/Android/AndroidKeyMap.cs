using Android.Views;

namespace Shiny.Gamepad;


/// <summary>
/// Turns Android key codes and motion axes into <see cref="GamepadButton"/> and stick values.
/// </summary>
static class AndroidKeyMap
{
    /// <summary>
    /// The button an Android key code stands for, or <see cref="GamepadButton.None"/> for a key
    /// that is not part of a controller.
    /// </summary>
    /// <remarks>
    /// Android names its face buttons for the Xbox layout it standardised on, so the mapping is
    /// direct and no mirroring is needed - a Nintendo pad connected to Android already reports its
    /// bottom button as <c>ButtonA</c>.
    /// </remarks>
    public static GamepadButton ToButton(Keycode keyCode) => keyCode switch
    {
        Keycode.ButtonA => GamepadButton.A,
        Keycode.ButtonB => GamepadButton.B,
        Keycode.ButtonX => GamepadButton.X,
        Keycode.ButtonY => GamepadButton.Y,
        Keycode.ButtonL1 => GamepadButton.LeftShoulder,
        Keycode.ButtonR1 => GamepadButton.RightShoulder,
        Keycode.ButtonL2 => GamepadButton.LeftTrigger,
        Keycode.ButtonR2 => GamepadButton.RightTrigger,
        Keycode.ButtonThumbl => GamepadButton.LeftStick,
        Keycode.ButtonThumbr => GamepadButton.RightStick,
        Keycode.ButtonStart => GamepadButton.Start,
        Keycode.ButtonSelect => GamepadButton.Select,
        Keycode.ButtonMode => GamepadButton.Home,
        Keycode.DpadUp => GamepadButton.DPadUp,
        Keycode.DpadDown => GamepadButton.DPadDown,
        Keycode.DpadLeft => GamepadButton.DPadLeft,
        Keycode.DpadRight => GamepadButton.DPadRight,
        Keycode.DpadCenter => GamepadButton.A,

        // Android's own guidance: some controllers report their B button as KEYCODE_BACK rather
        // than KEYCODE_BUTTON_B, and a game that only listens for the latter looks broken on them.
        // Only safe because this is reached exclusively for events whose source is a gamepad.
        Keycode.Back => GamepadButton.B,

        _ => GamepadButton.None
    };


    /// <summary>
    /// The buttons this device physically has, from the key codes it claims to support.
    /// </summary>
    /// <remarks>
    /// <c>HasKeys</c> is answered by the kernel driver's key bitmap, so it is a real answer rather
    /// than a guess - which is how the touchpad click and the paddles can be reported honestly
    /// here where other platforms have to assume.
    /// </remarks>
    public static GamepadButton GetSupportedButtons(InputDevice device)
    {
        Keycode[] candidates =
        [
            Keycode.ButtonA, Keycode.ButtonB, Keycode.ButtonX, Keycode.ButtonY,
            Keycode.ButtonL1, Keycode.ButtonR1, Keycode.ButtonL2, Keycode.ButtonR2,
            Keycode.ButtonThumbl, Keycode.ButtonThumbr,
            Keycode.ButtonStart, Keycode.ButtonSelect, Keycode.ButtonMode,
            Keycode.DpadUp, Keycode.DpadDown, Keycode.DpadLeft, Keycode.DpadRight
        ];

        var results = device.HasKeys(candidates.Select(x => (int)x).ToArray());
        var buttons = GamepadButton.None;

        // HasKeys is documented to return one entry per key asked about, but a driver that answers
        // short would otherwise crash enumeration rather than lose a button
        var known = Math.Min(results?.Length ?? 0, candidates.Length);
        for (var i = 0; i < known; i++)
        {
            if (results![i])
                buttons |= ToButton(candidates[i]);
        }

        // a device that reports a hat but no D-pad keys still has a D-pad; it just delivers it as
        // an axis, which the caller folds into the same four bits
        if (device.GetMotionRange(Axis.HatX, InputSourceType.Joystick) != null)
            buttons |= GamepadButton.DPad;

        return buttons == GamepadButton.None ? Infrastructure.AbstractGamepad.StandardButtons : buttons;
    }


    /// <summary>
    /// Reads one axis, clearing values inside the driver's reported flat zone.
    /// </summary>
    /// <remarks>
    /// <para><c>MotionRange.Flat</c> is the slop the driver itself says the stick has at rest, and
    /// is different per device. It is not a gameplay deadzone - it is the difference between "the
    /// stick is centred" and "the stick is centred and the hardware is noisy" - so it is applied
    /// here rather than left to the caller, who has no way to find out what it should be.</para>
    /// <para>Returns 0 for an axis the device does not have, which is how a controller with one
    /// stick reads as a controller with a second stick held at centre.</para>
    /// </remarks>
    public static float ReadAxis(MotionEvent e, InputDevice? device, Axis axis, int historyPos = -1)
    {
        var value = historyPos < 0
            ? e.GetAxisValue(axis)
            : e.GetHistoricalAxisValue(axis, historyPos);

        var range = device?.GetMotionRange(axis, e.Source);
        if (range == null)
            return 0f;

        return MathF.Abs(value) <= range.Flat ? 0f : value;
    }


    /// <summary>
    /// Reads the right stick, which Android does not map consistently.
    /// </summary>
    /// <remarks>
    /// The documented mapping is Z and RZ, and that is what an Xbox-style pad on Android reports.
    /// Plenty of controllers - older Bluetooth pads and most emulated ones - use RX and RY instead,
    /// and a few report both with one pair stuck at zero. Preferring whichever pair the device
    /// actually declares a motion range for gets both right without guessing from the device name.
    /// </remarks>
    public static GamepadStick ReadRightStick(MotionEvent e, InputDevice? device, int historyPos = -1)
    {
        var hasZ = device?.GetMotionRange(Axis.Z, e.Source) != null;
        var hasRz = device?.GetMotionRange(Axis.Rz, e.Source) != null;

        if (hasZ && hasRz)
        {
            return new GamepadStick(
                ReadAxis(e, device, Axis.Z, historyPos),
                -ReadAxis(e, device, Axis.Rz, historyPos)
            );
        }

        return new GamepadStick(
            ReadAxis(e, device, Axis.Rx, historyPos),
            -ReadAxis(e, device, Axis.Ry, historyPos)
        );
    }


    /// <summary>
    /// Reads a trigger, from whichever of the two axis pairs the device uses.
    /// </summary>
    /// <remarks>
    /// LTRIGGER/RTRIGGER is the joystick convention and BRAKE/GAS the one driving controllers use;
    /// several gamepads report both, with only one pair moving. Taking the larger of the two reads
    /// both kinds correctly and is harmless where only one is present, because the absent axis
    /// reads zero.
    /// </remarks>
    public static float ReadTrigger(MotionEvent e, InputDevice? device, bool left, int historyPos = -1)
    {
        var primary = ReadAxis(e, device, left ? Axis.Ltrigger : Axis.Rtrigger, historyPos);
        var secondary = ReadAxis(e, device, left ? Axis.Brake : Axis.Gas, historyPos);

        return MathF.Max(primary, secondary);
    }


    /// <summary>The D-pad, when the device reports it as a hat rather than as key events.</summary>
    public static GamepadButton ReadHat(MotionEvent e, InputDevice? device, int historyPos = -1)
    {
        if (device?.GetMotionRange(Axis.HatX, e.Source) == null)
            return GamepadButton.None;

        var x = ReadAxis(e, device, Axis.HatX, historyPos);
        var y = ReadAxis(e, device, Axis.HatY, historyPos);
        var buttons = GamepadButton.None;

        if (x < -0.5f)
            buttons |= GamepadButton.DPadLeft;
        else if (x > 0.5f)
            buttons |= GamepadButton.DPadRight;

        // the hat's Y is positive downwards, as every Android axis is
        if (y < -0.5f)
            buttons |= GamepadButton.DPadUp;
        else if (y > 0.5f)
            buttons |= GamepadButton.DPadDown;

        return buttons;
    }


    /// <summary>Whether an input source is a game controller rather than a touchscreen or keyboard.</summary>
    public static bool IsGamepad(InputSourceType sources)
        => (sources & InputSourceType.Gamepad) == InputSourceType.Gamepad ||
           (sources & InputSourceType.Joystick) == InputSourceType.Joystick;


    /// <summary>Guesses the controller family from its name and vendor id, for button glyphs.</summary>
    public static GamepadKind DetectKind(InputDevice device)
    {
        // USB vendor ids are exact where they are present; a Bluetooth pad often reports 0 and the
        // name is all there is
        var kind = device.VendorId switch
        {
            0x045E => GamepadKind.Xbox,       // Microsoft
            0x054C => GamepadKind.PlayStation, // Sony
            0x057E => GamepadKind.Nintendo,   // Nintendo
            0x28DE => GamepadKind.Steam,      // Valve
            _ => GamepadKind.Unknown
        };

        if (kind != GamepadKind.Unknown)
            return kind;

        var name = device.Name ?? string.Empty;

        if (name.Contains("Xbox", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Xbox;

        if (name.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("DualShock", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PS4", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("PS5", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.PlayStation;

        if (name.Contains("Switch", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Joy-Con", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Nintendo;

        return device.IsVirtual ? GamepadKind.Virtual : GamepadKind.Standard;
    }
}
