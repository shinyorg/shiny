namespace Shiny.Gamepad;


/// <summary>Something went wrong talking to a controller.</summary>
public class GamepadException : Exception
{
    /// <summary>Creates the exception.</summary>
    public GamepadException(string message) : base(message) { }

    /// <summary>Creates the exception.</summary>
    public GamepadException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// The controller, or the platform it is on, cannot do what was asked.
/// </summary>
/// <remarks>
/// Always raised against a specific <see cref="GamepadCapabilities"/> flag, and the message names
/// it along with why the platform withholds it. Check <see cref="IGamepad.Capabilities"/> first and
/// this never fires.
/// </remarks>
public class GamepadNotSupportedException(string message, GamepadCapabilities capability)
    : GamepadException(message)
{
    /// <summary>The capability that was missing.</summary>
    public GamepadCapabilities Capability { get; } = capability;
}


/// <summary>The controller was unplugged, switched off or went out of range.</summary>
/// <remarks>
/// Raised by anything called on an <see cref="IGamepad"/> after
/// <see cref="IGamepadManager.Disconnected"/> fired for it. <see cref="IGamepad.GetState"/> is the
/// exception - it keeps returning the last known state so a render loop does not have to guard
/// every frame.
/// </remarks>
public class GamepadDisconnectedException(string gamepadId)
    : GamepadException($"Gamepad '{gamepadId}' is no longer connected")
{
    /// <summary>The <see cref="IGamepad.Id"/> of the controller that went away.</summary>
    public string GamepadId { get; } = gamepadId;
}
