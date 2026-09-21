namespace Shiny.Gamepad;


/// <summary>
/// Every button a controller can report, as a bit per button so a whole controller's button state
/// fits in one <see cref="GamepadState.Buttons"/> value.
/// </summary>
/// <remarks>
/// <para><b>Named by position, not by the label printed on the pad.</b> <see cref="A"/> is the
/// bottom face button wherever you are: A on Xbox, Cross on PlayStation, B on a Nintendo pad (whose
/// face buttons are mirrored). <see cref="Y"/> is the top face button: Y on Xbox, Triangle on
/// PlayStation, X on Nintendo. This is the only mapping that lets "A jumps" mean the same thing on
/// every controller, and it is what every platform SDK underneath already normalises to. Use
/// <see cref="IGamepad.Kind"/> if you want to draw the right glyph.</para>
/// <para>Triggers appear here <i>and</i> as axes. <see cref="LeftTrigger"/> is set once
/// <see cref="GamepadState.LeftTrigger"/> crosses <see cref="GamepadState.TriggerPressThreshold"/>,
/// so code that only needs "is it held" does not have to pick a threshold of its own.</para>
/// <para>A bit that a controller has no button for is simply never set. Ask
/// <see cref="IGamepad.SupportedButtons"/> to tell "not pressed" apart from "not present" - the
/// paddles and the touchpad click exist on very little hardware.</para>
/// </remarks>
[Flags]
public enum GamepadButton : ulong
{
    /// <summary>No button.</summary>
    None = 0,

    /// <summary>Bottom face button - A on Xbox, Cross on PlayStation, B on Nintendo.</summary>
    A = 1UL << 0,

    /// <summary>Right face button - B on Xbox, Circle on PlayStation, A on Nintendo.</summary>
    B = 1UL << 1,

    /// <summary>Left face button - X on Xbox, Square on PlayStation, Y on Nintendo.</summary>
    X = 1UL << 2,

    /// <summary>Top face button - Y on Xbox, Triangle on PlayStation, X on Nintendo.</summary>
    Y = 1UL << 3,

    /// <summary>Upper left bumper - LB, L1, L.</summary>
    LeftShoulder = 1UL << 4,

    /// <summary>Upper right bumper - RB, R1, R.</summary>
    RightShoulder = 1UL << 5,

    /// <summary>Left trigger held past <see cref="GamepadState.TriggerPressThreshold"/> - LT, L2, ZL.</summary>
    LeftTrigger = 1UL << 6,

    /// <summary>Right trigger held past <see cref="GamepadState.TriggerPressThreshold"/> - RT, R2, ZR.</summary>
    RightTrigger = 1UL << 7,

    /// <summary>Left stick pressed in - L3, LSB.</summary>
    LeftStick = 1UL << 8,

    /// <summary>Right stick pressed in - R3, RSB.</summary>
    RightStick = 1UL << 9,

    /// <summary>D-pad up.</summary>
    DPadUp = 1UL << 10,

    /// <summary>D-pad down.</summary>
    DPadDown = 1UL << 11,

    /// <summary>D-pad left.</summary>
    DPadLeft = 1UL << 12,

    /// <summary>D-pad right.</summary>
    DPadRight = 1UL << 13,

    /// <summary>The right-hand menu button - Menu/Start on Xbox, Options on PlayStation, + on Nintendo.</summary>
    Start = 1UL << 14,

    /// <summary>The left-hand menu button - View/Back on Xbox, Create/Share on PlayStation, - on Nintendo.</summary>
    Select = 1UL << 15,

    /// <summary>
    /// The centre system button - Xbox Guide, PlayStation button, Nintendo Home.
    /// </summary>
    /// <remarks>
    /// Most platforms keep this for themselves and never hand it to the app. iOS and tvOS only
    /// deliver it when the app opts in, Android routes it to the launcher, and Windows never
    /// reports it at all. Do not put anything behind it that the player cannot reach another way.
    /// </remarks>
    Home = 1UL << 16,

    /// <summary>The touchpad pressed as a button - DualShock 4 and DualSense.</summary>
    Touchpad = 1UL << 17,

    /// <summary>Upper right rear paddle - Xbox Elite P1, DualSense Edge RB.</summary>
    Paddle1 = 1UL << 18,

    /// <summary>Upper left rear paddle - Xbox Elite P2, DualSense Edge LB.</summary>
    Paddle2 = 1UL << 19,

    /// <summary>Lower right rear paddle - Xbox Elite P3.</summary>
    Paddle3 = 1UL << 20,

    /// <summary>Lower left rear paddle - Xbox Elite P4.</summary>
    Paddle4 = 1UL << 21,

    /// <summary>Every D-pad direction, for masking.</summary>
    DPad = DPadUp | DPadDown | DPadLeft | DPadRight,

    /// <summary>Every face button, for masking.</summary>
    Face = A | B | X | Y,

    /// <summary>Every rear paddle, for masking.</summary>
    Paddles = Paddle1 | Paddle2 | Paddle3 | Paddle4
}
