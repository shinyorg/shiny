namespace Shiny.Gamepad;


/// <summary>
/// The analog inputs a controller reports, as addressed by <see cref="GamepadState.GetAxis"/> and
/// <see cref="GamepadAxisChangedEventArgs.Axis"/>.
/// </summary>
/// <remarks>
/// Sticks run -1 to 1 on both axes; triggers run 0 to 1. Y is positive upwards on both sticks -
/// Android, Linux and the browser all report it positive downwards natively and every backend here
/// flips it, so a stick pushed away from the player always reads positive.
/// </remarks>
public enum GamepadAxis
{
    /// <summary>Left stick horizontal, -1 (left) to 1 (right).</summary>
    LeftStickX,

    /// <summary>Left stick vertical, -1 (down) to 1 (up).</summary>
    LeftStickY,

    /// <summary>Right stick horizontal, -1 (left) to 1 (right).</summary>
    RightStickX,

    /// <summary>Right stick vertical, -1 (down) to 1 (up).</summary>
    RightStickY,

    /// <summary>Left trigger, 0 (released) to 1 (fully pulled).</summary>
    LeftTrigger,

    /// <summary>Right trigger, 0 (released) to 1 (fully pulled).</summary>
    RightTrigger
}
