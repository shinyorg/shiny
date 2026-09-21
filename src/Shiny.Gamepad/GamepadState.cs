namespace Shiny.Gamepad;


/// <summary>
/// One stick's position. X runs -1 (left) to 1 (right), Y runs -1 (down) to 1 (up).
/// </summary>
/// <remarks>
/// Raw, as the hardware reported it - no deadzone has been applied. A stick at rest very rarely
/// reads exactly zero, so run it through <see cref="WithDeadzone"/> (or check
/// <see cref="Magnitude"/>) before treating movement as intent.
/// </remarks>
public readonly record struct GamepadStick(float X, float Y)
{
    /// <summary>A stick at rest.</summary>
    public static readonly GamepadStick Neutral = new(0f, 0f);

    /// <summary>
    /// How far the stick is pushed, 0 to about 1.
    /// </summary>
    /// <remarks>
    /// Can exceed 1 slightly in the diagonals: most hardware reports a square range rather than a
    /// circular one, so a stick held to a corner reads roughly 1.41. Clamp if that matters.
    /// </remarks>
    public float Magnitude => MathF.Sqrt((this.X * this.X) + (this.Y * this.Y));

    /// <summary>The direction the stick is pushed, in radians, measured counter-clockwise from right.</summary>
    public float Angle => MathF.Atan2(this.Y, this.X);

    /// <summary>
    /// Zeroes the stick while it is within <paramref name="deadzone"/> of centre, and rescales what
    /// is left so the value still reaches 1 at full deflection.
    /// </summary>
    /// <remarks>
    /// <para>This is a radial deadzone: it looks at how far the stick is pushed overall, not at
    /// each axis separately. Deadzoning the axes independently is the classic bug that makes a
    /// stick feel like it snaps to the compass points, because a small X is discarded while a large
    /// Y survives.</para>
    /// <para>The rescale matters as much as the cut. Without it, the stick jumps from 0 straight to
    /// <paramref name="deadzone"/> the moment it leaves the dead area, and slow movement is
    /// impossible.</para>
    /// </remarks>
    /// <param name="deadzone">Radius to treat as centre, 0 to 1. 0.15 suits most thumbsticks.</param>
    public GamepadStick WithDeadzone(float deadzone)
    {
        if (deadzone <= 0f)
            return this;

        var magnitude = this.Magnitude;
        if (magnitude <= deadzone)
            return Neutral;

        if (magnitude == 0f)
            return Neutral;

        var scaled = MathF.Min((magnitude - deadzone) / (1f - deadzone), 1f);
        var factor = scaled / magnitude;

        return new GamepadStick(this.X * factor, this.Y * factor);
    }
}


/// <summary>
/// Everything a controller is reporting at one instant - the whole pad in one value.
/// </summary>
/// <remarks>
/// <para>A snapshot, not a view: it does not change after <see cref="IGamepad.GetState"/> returns
/// it, so two of them can be compared to work out what the player did. That is exactly how
/// <see cref="IGamepad.ButtonChanged"/> and <see cref="IGamepad.AxisChanged"/> are produced, and
/// why you do not have to do it yourself for the common cases.</para>
/// <para>Sticks are raw. Apply <see cref="GamepadStick.WithDeadzone"/> before using them for
/// movement.</para>
/// </remarks>
public readonly record struct GamepadState
{
    /// <summary>
    /// How far a trigger must be pulled before its <see cref="GamepadButton"/> bit is set - 0.5.
    /// </summary>
    /// <remarks>
    /// Halfway, which is where Windows, Android and the browser all put their own digital trigger
    /// threshold. Read <see cref="LeftTrigger"/> or <see cref="RightTrigger"/> directly if you want
    /// to pick your own.
    /// </remarks>
    public const float TriggerPressThreshold = 0.5f;

    /// <summary>A controller with nothing pressed and both sticks centred.</summary>
    public static readonly GamepadState Empty = new();

    /// <summary>Which buttons are held. Triggers are folded in past <see cref="TriggerPressThreshold"/>.</summary>
    public GamepadButton Buttons { get; init; }

    /// <summary>Left stick, raw.</summary>
    public GamepadStick LeftStick { get; init; }

    /// <summary>Right stick, raw.</summary>
    public GamepadStick RightStick { get; init; }

    /// <summary>Left trigger, 0 to 1. Reads 0 or 1 only on hardware whose triggers are switches.</summary>
    public float LeftTrigger { get; init; }

    /// <summary>Right trigger, 0 to 1.</summary>
    public float RightTrigger { get; init; }

    /// <summary>
    /// When the snapshot was taken, from <see cref="Environment.TickCount64"/>.
    /// </summary>
    /// <remarks>
    /// A monotonic millisecond clock for measuring the gap between two snapshots. It is not wall
    /// clock time and is not comparable across processes or devices.
    /// </remarks>
    public long Timestamp { get; init; }

    /// <summary>
    /// Whether every button in <paramref name="buttons"/> is held.
    /// </summary>
    /// <remarks>
    /// Passing more than one button asks for <i>all</i> of them - <c>IsPressed(A | B)</c> is a
    /// chord. Use <see cref="IsAnyPressed"/> for "either".
    /// </remarks>
    public bool IsPressed(GamepadButton buttons)
        => buttons != GamepadButton.None && (this.Buttons & buttons) == buttons;

    /// <summary>Whether any button in <paramref name="buttons"/> is held.</summary>
    public bool IsAnyPressed(GamepadButton buttons) => (this.Buttons & buttons) != GamepadButton.None;

    /// <summary>Reads one analog input by name.</summary>
    public float GetAxis(GamepadAxis axis) => axis switch
    {
        GamepadAxis.LeftStickX => this.LeftStick.X,
        GamepadAxis.LeftStickY => this.LeftStick.Y,
        GamepadAxis.RightStickX => this.RightStick.X,
        GamepadAxis.RightStickY => this.RightStick.Y,
        GamepadAxis.LeftTrigger => this.LeftTrigger,
        GamepadAxis.RightTrigger => this.RightTrigger,
        _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unknown gamepad axis")
    };

    /// <summary>
    /// The D-pad as a stick, so it can drive the same code a thumbstick does.
    /// </summary>
    /// <remarks>
    /// Each axis is -1, 0 or 1. Opposite directions held together cancel, which is what the
    /// hardware means by it.
    /// </remarks>
    public GamepadStick DPad => new(
        (this.IsPressed(GamepadButton.DPadRight) ? 1f : 0f) - (this.IsPressed(GamepadButton.DPadLeft) ? 1f : 0f),
        (this.IsPressed(GamepadButton.DPadUp) ? 1f : 0f) - (this.IsPressed(GamepadButton.DPadDown) ? 1f : 0f)
    );

    /// <summary>
    /// Buttons held here that were not held in <paramref name="previous"/>.
    /// </summary>
    /// <remarks>
    /// The edge detection a game loop needs to fire an action once per press rather than once per
    /// frame. Subscribing to <see cref="IGamepad.ButtonChanged"/> does the same thing without the
    /// bookkeeping.
    /// </remarks>
    public GamepadButton GetPressedSince(GamepadState previous) => this.Buttons & ~previous.Buttons;

    /// <summary>Buttons held in <paramref name="previous"/> that have since been let go.</summary>
    public GamepadButton GetReleasedSince(GamepadState previous) => previous.Buttons & ~this.Buttons;
}
