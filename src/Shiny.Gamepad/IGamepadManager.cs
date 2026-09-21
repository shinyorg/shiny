namespace Shiny.Gamepad;


/// <summary>
/// Finds game controllers and tells you when they come and go.
/// </summary>
/// <remarks>
/// <para>Backed by GameController on iOS, tvOS, Mac Catalyst and macOS, <c>InputDevice</c> plus the
/// activity's window callback on Android, <c>Windows.Gaming.Input</c> on Windows, evdev on Linux
/// (via the separate <c>Shiny.Gamepad.Linux</c> package) and the W3C Gamepad API in the browser
/// (via <c>Shiny.Gamepad.Blazor</c>).</para>
/// <para><b>Nothing happens until <see cref="GetGamepads"/> is awaited once.</b> That call starts
/// the watch, and from then on <see cref="Connected"/> and <see cref="Disconnected"/> report
/// changes without being asked. It is the only asynchronous step - everything a frame needs after
/// it, <see cref="IGamepad.GetState"/> above all, is synchronous.</para>
/// <para>No permission gates gamepads on any of these platforms, so there is nothing to request.
/// The browser is a partial exception: for fingerprinting reasons a controller stays invisible
/// until the player presses something on it, so a Blazor app sees an empty list until then and a
/// <see cref="Connected"/> event when the player acts. Say so in your UI rather than reporting no
/// controller found.</para>
/// </remarks>
public interface IGamepadManager
{
    /// <summary>
    /// The controllers connected right now, starting the watch the first time it is called.
    /// </summary>
    /// <remarks>
    /// <para>Await this once at start-up, then keep the list current from <see cref="Connected"/>
    /// and <see cref="Disconnected"/> rather than calling it again each frame. Calling it again is
    /// harmless and cheap - the watch is only started once - but it returns a fresh list each
    /// time.</para>
    /// <para>Order is not meaningful and is not stable across calls. Use
    /// <see cref="IGamepad.PlayerIndex"/> to decide who is who, and <see cref="IGamepad.Id"/> to
    /// match a controller to one you already know about.</para>
    /// </remarks>
    Task<IReadOnlyList<IGamepad>> GetGamepads(CancellationToken ct = default);

    /// <summary>
    /// Fires when a controller connects, and once for each controller already connected when the
    /// watch starts.
    /// </summary>
    /// <remarks>
    /// The instance is fully usable when this fires. Raised on the platform's own callback thread -
    /// the main thread on Android and in the browser, elsewhere a background one - so marshal
    /// before touching UI.
    /// </remarks>
    event EventHandler<GamepadConnectionEventArgs>? Connected;

    /// <summary>
    /// Fires when a controller is unplugged, switched off or goes out of range.
    /// </summary>
    /// <remarks>
    /// The instance handed over is already inert. Drop your reference to it - a controller that
    /// comes back is a new instance, not this one revived.
    /// </remarks>
    event EventHandler<GamepadConnectionEventArgs>? Disconnected;

    /// <summary>
    /// How far a stick or trigger must move before <see cref="IGamepad.AxisChanged"/> fires.
    /// Defaults to 0.02.
    /// </summary>
    /// <remarks>
    /// <para>Not a deadzone - it is how much change is worth an event, measured against where that
    /// axis was when it last raised one. A resting thumb makes an analog stick jitter by a few
    /// thousandths continuously; without this the event would fire hundreds of times a second
    /// saying nothing happened.</para>
    /// <para>Raise it for menu navigation, lower it for a racing game's steering. It has no effect
    /// on <see cref="IGamepad.GetState"/>, which is always the raw value.</para>
    /// </remarks>
    float AxisChangeThreshold { get; set; }

    /// <summary>
    /// How often controllers are read on platforms that have to be polled. Defaults to 16ms, about
    /// 60Hz.
    /// </summary>
    /// <remarks>
    /// <para>Windows only in practice. Apple, Android and Linux push input as it happens and ignore
    /// this; the browser runs on <c>requestAnimationFrame</c> and is paced by the display instead,
    /// so it ignores it too.</para>
    /// <para>Lower it for a game that renders faster than 60Hz - a poll slower than the frame rate
    /// shows up as input lag. Raise it in a menu to save battery. Changes take effect on the next
    /// poll.</para>
    /// </remarks>
    TimeSpan PollInterval { get; set; }
}
