namespace Shiny.Gamepad;


/// <summary>
/// What a particular controller, on a particular platform, can actually do beyond reporting sticks
/// and buttons.
/// </summary>
/// <remarks>
/// <para>Read these off <see cref="IGamepad.Capabilities"/>, never from the target framework. The
/// same DualSense reports motion and a light bar over USB on Linux and neither of them over
/// Bluetooth in a browser, because what is missing is the API, not the hardware.</para>
/// <para>Anything not listed here throws <see cref="GamepadNotSupportedException"/> rather than
/// failing quietly, so a missing feature shows up the first time it is used instead of looking
/// like a broken controller.</para>
/// </remarks>
[Flags]
public enum GamepadCapabilities
{
    /// <summary>Sticks and buttons only.</summary>
    None = 0,

    /// <summary>
    /// <see cref="IGamepad.SetVibration"/> drives the two handle motors.
    /// </summary>
    Vibration = 1,

    /// <summary>
    /// <see cref="GamepadVibration.LeftTrigger"/> and <see cref="GamepadVibration.RightTrigger"/>
    /// reach separate motors inside the triggers.
    /// </summary>
    /// <remarks>
    /// Xbox One and later pads on Windows, and any controller with per-trigger haptic localities on
    /// Apple platforms. Where this is missing the trigger values are ignored rather than folded
    /// into the handles, so a trigger-only effect is silent instead of wrong.
    /// </remarks>
    TriggerVibration = 2,

    /// <summary><see cref="IGamepad.GetBattery"/> returns a reading.</summary>
    Battery = 4,

    /// <summary>
    /// <see cref="IGamepad.GetMotion"/> and <see cref="IGamepad.MotionChanged"/> report the
    /// gyroscope and accelerometer.
    /// </summary>
    /// <remarks>
    /// The sensors may still need waking with <see cref="IGamepad.SetMotionEnabled"/> - they cost
    /// battery, so Apple leaves them off until asked.
    /// </remarks>
    Motion = 8,

    /// <summary><see cref="IGamepad.SetLight"/> sets the light bar or player LED colour.</summary>
    Light = 16,

    /// <summary>The touchpad reports a click through <see cref="GamepadButton.Touchpad"/>.</summary>
    Touchpad = 32,

    /// <summary>Rear paddles report through <see cref="GamepadButton.Paddles"/>.</summary>
    Paddles = 64,

    /// <summary>
    /// <see cref="IGamepad.Id"/> is the same string the next time this controller connects.
    /// </summary>
    /// <remarks>
    /// Windows and Linux can identify a specific physical controller across reconnects and reboots,
    /// so per-controller settings can be remembered. The Apple, Android and browser APIs cannot -
    /// there the id only lasts as long as the connection.
    /// </remarks>
    PersistentId = 128
}


/// <summary>
/// Which family a controller belongs to, for picking button glyphs and prompts.
/// </summary>
/// <remarks>
/// Cosmetic only. <see cref="GamepadButton"/> is already normalised by position, so nothing about
/// reading input changes with this - it exists so "press A" can be drawn as a Cross on a DualSense
/// and a B on a Switch Pro.
/// </remarks>
public enum GamepadKind
{
    /// <summary>The platform did not say, or the name matched nothing known.</summary>
    Unknown,

    /// <summary>A pad with the standard two-stick layout whose brand is not identifiable.</summary>
    Standard,

    /// <summary>Xbox, or an Xbox-compatible pad.</summary>
    Xbox,

    /// <summary>DualShock 4, DualSense or DualSense Edge.</summary>
    PlayStation,

    /// <summary>Switch Pro Controller, Joy-Con pair or Switch 2 Pro.</summary>
    Nintendo,

    /// <summary>A Steam Controller or Steam Deck's built-in pad.</summary>
    Steam,

    /// <summary>
    /// The Siri Remote, or another pad with only a D-pad and two buttons.
    /// </summary>
    /// <remarks>
    /// tvOS only. There are no sticks and no triggers - the touch surface arrives as
    /// <see cref="GamepadState.DPad"/>, and everything else reads as neutral.
    /// </remarks>
    Remote,

    /// <summary>An on-screen or software controller rather than physical hardware.</summary>
    Virtual
}


/// <summary>Whether a controller's battery is charging, and roughly how full it is.</summary>
public enum GamepadBatteryState
{
    /// <summary>The platform reported a level but would not say whether it is charging.</summary>
    Unknown,

    /// <summary>Running on battery.</summary>
    Discharging,

    /// <summary>Plugged in and charging.</summary>
    Charging,

    /// <summary>Plugged in and full.</summary>
    Full,

    /// <summary>Mains powered with no battery in it - a wired pad.</summary>
    Wired
}
