namespace Shiny.Gamepad;


/// <summary>
/// One connected game controller.
/// </summary>
/// <remarks>
/// <para>Two ways to read it, because games need both. <see cref="GetState"/> returns the whole pad
/// as a value and is what a render loop calls every frame - it never blocks and never allocates
/// beyond the snapshot. <see cref="ButtonChanged"/> and <see cref="AxisChanged"/> report the
/// transitions, which is what menus and UI want; they save you writing the edge detection.</para>
/// <para>Instances come from <see cref="IGamepadManager"/> and live as long as the connection. When
/// the controller goes away the instance stays valid but inert: <see cref="IsConnected"/> turns
/// false, <see cref="GetState"/> keeps returning the last thing it saw, and everything else throws
/// <see cref="GamepadDisconnectedException"/>. A controller that reconnects is a new instance - see
/// <see cref="Id"/> for whether you can recognise it.</para>
/// <para>Everything past sticks and buttons is optional. Check <see cref="Capabilities"/> before
/// offering rumble, a battery gauge, motion controls or a light bar.</para>
/// </remarks>
public interface IGamepad
{
    /// <summary>
    /// Identifies this controller.
    /// </summary>
    /// <remarks>
    /// Always unique among the connected controllers. Whether it survives a reconnect depends on
    /// the platform: Windows and Linux can name a specific physical pad across reboots and set
    /// <see cref="GamepadCapabilities.PersistentId"/> to say so, while the Apple, Android and
    /// browser APIs hand out a fresh identity every time and the id is only good for this
    /// connection. Do not key saved per-player settings on it without checking that flag.
    /// </remarks>
    string Id { get; }

    /// <summary>The controller's name as the platform reports it, for showing to the player.</summary>
    /// <remarks>Never null, but often generic - "Wireless Controller" is a common answer.</remarks>
    string Name { get; }

    /// <summary>Which family it belongs to, for drawing the right button glyphs.</summary>
    GamepadKind Kind { get; }

    /// <summary>
    /// The player slot the platform assigned, starting at 1, or null when it assigns none.
    /// </summary>
    /// <remarks>
    /// This is the number the lights on the front of the controller are showing. It is the
    /// platform's opinion, not yours - Apple and Linux let you set it, Windows and the browser do
    /// not, and it can change while the app runs as other controllers come and go.
    /// </remarks>
    int? PlayerIndex { get; }

    /// <summary>What this controller can do beyond sticks and buttons, on this platform.</summary>
    GamepadCapabilities Capabilities { get; }

    /// <summary>
    /// The buttons this controller physically has.
    /// </summary>
    /// <remarks>
    /// Lets "not pressed" be told apart from "not present", which matters for the touchpad click,
    /// the paddles and <see cref="GamepadButton.Home"/>. A platform that will not enumerate its
    /// buttons reports the standard set, so this is a hint rather than a guarantee.
    /// </remarks>
    GamepadButton SupportedButtons { get; }

    /// <summary>Whether the controller is still there.</summary>
    bool IsConnected { get; }

    /// <summary>
    /// The controller as it is right now.
    /// </summary>
    /// <remarks>
    /// <para>Safe to call every frame, and safe to call after a disconnect - it returns the last
    /// state seen rather than throwing, so a render loop does not need a guard.</para>
    /// <para>Where the platform pushes input at us (Apple, Android, Linux) this is the state as of
    /// the last event, which is as fresh as the hardware has been. Where input has to be polled
    /// (Windows) the hardware is read on the spot. In the browser it is the last state the
    /// animation-frame loop saw, so it can be up to one frame old - the browser offers nothing
    /// more current.</para>
    /// </remarks>
    GamepadState GetState();

    /// <summary>
    /// Fires once each time a button goes down and once each time it comes up.
    /// </summary>
    /// <remarks>
    /// Raised on whichever thread the platform delivered the input on - the main thread on Android
    /// and in the browser, a background queue on Apple, the reader thread on Linux, the poll timer
    /// on Windows. Marshal before touching UI.
    /// </remarks>
    event EventHandler<GamepadButtonChangedEventArgs>? ButtonChanged;

    /// <summary>
    /// Fires when a stick or trigger moves by more than
    /// <see cref="IGamepadManager.AxisChangeThreshold"/>.
    /// </summary>
    /// <remarks>Same threading caveat as <see cref="ButtonChanged"/>.</remarks>
    event EventHandler<GamepadAxisChangedEventArgs>? AxisChanged;

    /// <summary>
    /// Fires with each new gyroscope and accelerometer reading, once motion is switched on.
    /// </summary>
    /// <remarks>
    /// Nothing arrives until <see cref="SetMotionEnabled"/> has been called with true, and nothing
    /// ever arrives unless <see cref="GamepadCapabilities.Motion"/> is set. Readings come at the
    /// sensor's own rate, which is usually far faster than a frame.
    /// </remarks>
    event EventHandler<GamepadMotionChangedEventArgs>? MotionChanged;

    /// <summary>
    /// Runs the controller's motors until told otherwise.
    /// </summary>
    /// <remarks>
    /// <para>This sets a level, it does not play a pulse: the motors keep running at the strength
    /// given until the next call changes it. Pass <see cref="GamepadVibration.Off"/> to stop, and
    /// do stop - a controller left rumbling will keep going after the app is backgrounded on some
    /// platforms and until its battery dies on others.</para>
    /// <para>For a one-shot bump use <see cref="GamepadExtensions.Pulse(IGamepad, GamepadVibration, TimeSpan, CancellationToken)"/>, which sets the level,
    /// waits and clears it.</para>
    /// <para>The browser is the exception and cannot hold a level indefinitely: its effects carry a
    /// duration, so the Blazor backend re-arms a five second effect on a timer while a level is
    /// set. The observable behaviour is the same.</para>
    /// </remarks>
    /// <param name="vibration">Per-motor strength. Clamped into 0..1 for you.</param>
    /// <param name="ct">Cancels the call, not the vibration already running.</param>
    /// <exception cref="GamepadNotSupportedException"><see cref="GamepadCapabilities.Vibration"/> is not set.</exception>
    /// <exception cref="GamepadDisconnectedException">The controller has gone away.</exception>
    Task SetVibration(GamepadVibration vibration, CancellationToken ct = default);

    /// <summary>
    /// Reads the controller's battery.
    /// </summary>
    /// <remarks>
    /// Cheap on every platform that offers it at all - no radio round trip, just the last value the
    /// driver cached - so polling it every few seconds for a gauge is fine.
    /// </remarks>
    /// <exception cref="GamepadNotSupportedException"><see cref="GamepadCapabilities.Battery"/> is not set.</exception>
    /// <exception cref="GamepadDisconnectedException">The controller has gone away.</exception>
    Task<GamepadBattery> GetBattery(CancellationToken ct = default);

    /// <summary>
    /// Sets the light bar or player LED colour.
    /// </summary>
    /// <remarks>
    /// The colour sticks until changed or the controller disconnects. Where the hardware has only a
    /// player indicator rather than an RGB bar, the platform picks the nearest thing it can show.
    /// </remarks>
    /// <exception cref="GamepadNotSupportedException"><see cref="GamepadCapabilities.Light"/> is not set.</exception>
    /// <exception cref="GamepadDisconnectedException">The controller has gone away.</exception>
    Task SetLight(GamepadLight light, CancellationToken ct = default);

    /// <summary>
    /// Switches the gyroscope and accelerometer on or off.
    /// </summary>
    /// <remarks>
    /// Off to begin with, everywhere, because the sensors cost battery and most games never touch
    /// them. Turn them off again when you stop reading them.
    /// </remarks>
    /// <exception cref="GamepadNotSupportedException"><see cref="GamepadCapabilities.Motion"/> is not set.</exception>
    /// <exception cref="GamepadDisconnectedException">The controller has gone away.</exception>
    Task SetMotionEnabled(bool enabled, CancellationToken ct = default);

    /// <summary>
    /// The latest gyroscope and accelerometer reading, or null when motion is off or none has
    /// arrived yet.
    /// </summary>
    /// <remarks>
    /// The polling counterpart to <see cref="MotionChanged"/>, for a game loop that wants the
    /// current orientation rather than every sample. Needs <see cref="SetMotionEnabled"/> first.
    /// </remarks>
    GamepadMotion? GetMotion();
}
