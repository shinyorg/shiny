using Microsoft.Extensions.Logging;

namespace Shiny.Gamepad.Infrastructure;


/// <summary>
/// The parts of a controller that are the same everywhere: holding the current state, turning one
/// state into the next into events, and refusing work once the controller has gone.
/// </summary>
/// <remarks>
/// <para>Every backend feeds this the same way - build a <see cref="GamepadState"/> and hand it to
/// <see cref="UpdateState"/>. Where the platform pushes input (Apple, Android, Linux) that happens
/// in its callback; where it has to be polled (Windows, the browser) the manager's loop does it.
/// Neither the diffing nor the threshold logic is written twice.</para>
/// <para>Public for the two out-of-package backends, <c>Shiny.Gamepad.Linux</c> and
/// <c>Shiny.Gamepad.Blazor</c>, which derive from it. It is not an extension point for
/// applications.</para>
/// </remarks>
public abstract class AbstractGamepad(string id, ILogger logger) : IGamepad
{
    readonly Lock stateLock = new();
    GamepadState state = GamepadState.Empty;
    GamepadMotion? motion;

    // what each axis read when it last raised an event - the threshold is measured against this,
    // not against the previous state, or a slow drift never crosses it and never reports at all
    float reportedLeftX, reportedLeftY, reportedRightX, reportedRightY, reportedLeftTrigger, reportedRightTrigger;


    /// <inheritdoc/>
    public string Id { get; } = id;

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract GamepadKind Kind { get; }

    /// <inheritdoc/>
    public abstract int? PlayerIndex { get; }

    /// <inheritdoc/>
    public abstract GamepadCapabilities Capabilities { get; }

    /// <inheritdoc/>
    public virtual GamepadButton SupportedButtons => StandardButtons;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; } = true;

    /// <inheritdoc/>
    public event EventHandler<GamepadButtonChangedEventArgs>? ButtonChanged;

    /// <inheritdoc/>
    public event EventHandler<GamepadAxisChangedEventArgs>? AxisChanged;

    /// <inheritdoc/>
    public event EventHandler<GamepadMotionChangedEventArgs>? MotionChanged;


    /// <summary>
    /// Everything a two-stick controller is expected to have - the default
    /// <see cref="SupportedButtons"/> for a platform that will not enumerate.
    /// </summary>
    public const GamepadButton StandardButtons =
        GamepadButton.Face |
        GamepadButton.DPad |
        GamepadButton.LeftShoulder | GamepadButton.RightShoulder |
        GamepadButton.LeftTrigger | GamepadButton.RightTrigger |
        GamepadButton.LeftStick | GamepadButton.RightStick |
        GamepadButton.Start | GamepadButton.Select;


    /// <summary>
    /// How far an axis must move to be worth an event. Set by the manager that owns this gamepad
    /// from <see cref="IGamepadManager.AxisChangeThreshold"/>.
    /// </summary>
    public float AxisChangeThreshold { get; set; } = 0.02f;


    /// <inheritdoc/>
    public GamepadState GetState()
    {
        lock (this.stateLock)
            return this.state;
    }


    /// <inheritdoc/>
    public GamepadMotion? GetMotion()
    {
        lock (this.stateLock)
            return this.motion;
    }


    /// <summary>
    /// Takes the controller's new state, stores it, and raises an event for everything that
    /// changed.
    /// </summary>
    /// <remarks>
    /// <para>The one entry point every backend uses. Safe to call with an identical state - it
    /// compares before it raises, so a poll loop running faster than the player costs nothing but
    /// the comparison.</para>
    /// <para>Handlers run inline on the calling thread, so a slow handler holds up the backend's
    /// input callback. One that throws is logged and the remaining handlers still run - an
    /// exception escaping here would tear down the reader thread on Linux and the whole D-Bus-style
    /// callback contract elsewhere.</para>
    /// </remarks>
    /// <param name="next">The controller's new state. <see cref="GamepadState.Timestamp"/> is filled in if unset.</param>
    protected void UpdateState(GamepadState next)
    {
        if (next.Timestamp == 0)
            next = next with { Timestamp = Environment.TickCount64 };

        GamepadState previous;
        var axisEvents = default(List<GamepadAxisChangedEventArgs>);

        lock (this.stateLock)
        {
            previous = this.state;
            this.state = next;

            if (!this.IsConnected)
                return;

            this.Collect(ref axisEvents, GamepadAxis.LeftStickX, next.LeftStick.X, ref this.reportedLeftX, next);
            this.Collect(ref axisEvents, GamepadAxis.LeftStickY, next.LeftStick.Y, ref this.reportedLeftY, next);
            this.Collect(ref axisEvents, GamepadAxis.RightStickX, next.RightStick.X, ref this.reportedRightX, next);
            this.Collect(ref axisEvents, GamepadAxis.RightStickY, next.RightStick.Y, ref this.reportedRightY, next);
            this.Collect(ref axisEvents, GamepadAxis.LeftTrigger, next.LeftTrigger, ref this.reportedLeftTrigger, next);
            this.Collect(ref axisEvents, GamepadAxis.RightTrigger, next.RightTrigger, ref this.reportedRightTrigger, next);
        }

        // raised outside the lock: a handler that calls back into GetState would otherwise deadlock
        // on a non-reentrant Lock, and holding it across arbitrary user code is asking for trouble
        var pressed = next.GetPressedSince(previous);
        var released = next.GetReleasedSince(previous);

        if (pressed != GamepadButton.None)
            this.RaiseButtons(pressed, true, next);

        if (released != GamepadButton.None)
            this.RaiseButtons(released, false, next);

        if (axisEvents != null)
        {
            foreach (var args in axisEvents)
                this.Raise(this.AxisChanged, args);
        }
    }


    void Collect(ref List<GamepadAxisChangedEventArgs>? events, GamepadAxis axis, float value, ref float reported, GamepadState state)
    {
        // an axis that lands exactly on a rail always reports, however small the step, so a stick
        // released from a hair off centre still produces the zero that stops the player moving
        var atRail = value is 0f or 1f or -1f;
        if (!atRail && MathF.Abs(value - reported) < this.AxisChangeThreshold)
            return;

        if (value == reported)
            return;

        var previous = reported;
        reported = value;
        (events ??= new List<GamepadAxisChangedEventArgs>(6)).Add(
            new GamepadAxisChangedEventArgs(this, axis, value, previous, state)
        );
    }


    void RaiseButtons(GamepadButton changed, bool isPressed, GamepadState state)
    {
        // one event per button, never a combined mask - a handler switching on e.Button would
        // otherwise silently miss the second of two buttons pressed in the same poll
        var bits = (ulong)changed;
        while (bits != 0)
        {
            var bit = bits & (~bits + 1);
            bits &= bits - 1;

            this.Raise(this.ButtonChanged, new GamepadButtonChangedEventArgs(this, (GamepadButton)bit, isPressed, state));
        }
    }


    /// <summary>
    /// Stores a new motion reading and raises <see cref="MotionChanged"/>.
    /// </summary>
    protected void UpdateMotion(GamepadMotion reading)
    {
        lock (this.stateLock)
            this.motion = reading;

        if (this.IsConnected)
            this.Raise(this.MotionChanged, new GamepadMotionChangedEventArgs(this, reading));
    }


    /// <summary>
    /// Clears the motion reading, so <see cref="GetMotion"/> reports null again once the sensors
    /// are switched off.
    /// </summary>
    protected void ClearMotion()
    {
        lock (this.stateLock)
            this.motion = null;
    }


    /// <summary>
    /// Marks the controller gone. The last state is kept so <see cref="GetState"/> stays callable;
    /// everything else starts throwing <see cref="GamepadDisconnectedException"/>.
    /// </summary>
    /// <remarks>
    /// Called by the manager. Idempotent, because a platform that reports the same disconnect twice
    /// is not unusual.
    /// </remarks>
    public virtual void SetDisconnected()
    {
        if (!this.IsConnected)
            return;

        this.IsConnected = false;
        this.OnDisconnected();
    }


    /// <summary>Releases whatever the backend was holding for this controller.</summary>
    protected virtual void OnDisconnected() { }


    /// <summary>Throws if the controller has gone away. Call at the top of anything that touches hardware.</summary>
    protected void AssertConnected()
    {
        if (!this.IsConnected)
            throw new GamepadDisconnectedException(this.Id);
    }


    /// <summary>
    /// Throws unless <paramref name="capability"/> is present, naming it and the platform reason.
    /// </summary>
    protected void AssertCapability(GamepadCapabilities capability, string reason)
    {
        if ((this.Capabilities & capability) != capability)
            throw new GamepadNotSupportedException($"'{this.Name}' does not support {capability} - {reason}", capability);
    }


    void Raise<TArgs>(EventHandler<TArgs>? handler, TArgs args)
    {
        if (handler == null)
            return;

        foreach (var invocation in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<TArgs>)invocation).Invoke(this, args);
            }
            catch (Exception ex)
            {
                logger.InputHandlerThrew(this.Id, ex);
            }
        }
    }


    /// <inheritdoc/>
    public abstract Task SetVibration(GamepadVibration vibration, CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task<GamepadBattery> GetBattery(CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task SetLight(GamepadLight light, CancellationToken ct = default);

    /// <inheritdoc/>
    public abstract Task SetMotionEnabled(bool enabled, CancellationToken ct = default);


    /// <inheritdoc/>
    public override string ToString() => $"{this.Name} ({this.Kind}, {this.Id})";
}
