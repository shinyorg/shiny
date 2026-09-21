namespace Shiny.Gamepad;


/// <summary>
/// A single button going down or coming up.
/// </summary>
/// <param name="Gamepad">The controller it happened on.</param>
/// <param name="Button">Exactly one <see cref="GamepadButton"/> bit - never a combination.</param>
/// <param name="IsPressed">True on the way down, false on the way up.</param>
/// <param name="State">The whole controller at that instant, for reading chords and modifiers.</param>
public record GamepadButtonChangedEventArgs(
    IGamepad Gamepad,
    GamepadButton Button,
    bool IsPressed,
    GamepadState State
);


/// <summary>
/// An analog input that has moved far enough to be worth reporting.
/// </summary>
/// <param name="Gamepad">The controller it happened on.</param>
/// <param name="Axis">Which stick axis or trigger moved.</param>
/// <param name="Value">Where it is now. Raw - no deadzone has been applied.</param>
/// <param name="PreviousValue">Where it was when this axis last raised an event.</param>
/// <param name="State">The whole controller at that instant.</param>
/// <remarks>
/// "Far enough" is <see cref="IGamepadManager.AxisChangeThreshold"/>. Sticks are noisy and a
/// resting thumb will otherwise raise thousands of events a second reporting nothing.
/// </remarks>
public record GamepadAxisChangedEventArgs(
    IGamepad Gamepad,
    GamepadAxis Axis,
    float Value,
    float PreviousValue,
    GamepadState State
);


/// <summary>A gyroscope and accelerometer reading.</summary>
/// <param name="Gamepad">The controller it came from.</param>
/// <param name="Motion">The reading.</param>
public record GamepadMotionChangedEventArgs(IGamepad Gamepad, GamepadMotion Motion);


/// <summary>A controller appearing or going away.</summary>
/// <param name="Gamepad">The controller. After a disconnect it is inert - every call throws.</param>
public record GamepadConnectionEventArgs(IGamepad Gamepad);
