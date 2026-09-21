using Xunit;

namespace Shiny.Gamepad.Tests;


/// <summary>
/// Deadzone handling, which is the difference between a stick that feels right and one that feels
/// broken.
/// </summary>
public class StickTests
{
    [Fact]
    public void ADiagonalNudgeIsNotSnappedToAnAxis()
    {
        // 0.1 on each axis is 0.141 of total deflection - inside a 0.15 radial deadzone, so both go
        // to zero together. An axis-by-axis deadzone would keep neither, but the classic bug is the
        // reverse case below.
        var stick = new GamepadStick(0.1f, 0.1f).WithDeadzone(0.15f);

        Assert.Equal(GamepadStick.Neutral, stick);
    }


    [Fact]
    public void ASmallAxisSurvivesAlongsideALargeOne()
    {
        // pushed hard up and slightly right: an independent per-axis deadzone would discard the X
        // and snap the stick to straight up, which is what makes a stick feel like it has eight
        // directions instead of a full circle
        var stick = new GamepadStick(0.1f, 0.9f).WithDeadzone(0.15f);

        Assert.True(stick.X > 0f);
        Assert.True(stick.Y > 0f);
    }


    [Fact]
    public void LeavingTheDeadzoneStartsFromZeroRatherThanJumping()
    {
        var justOutside = new GamepadStick(0.16f, 0f).WithDeadzone(0.15f);

        // without rescaling this would read 0.16 - the stick would jump the moment it left the dead
        // area and slow movement would be impossible
        Assert.True(justOutside.Magnitude < 0.05f);
    }


    [Fact]
    public void FullDeflectionStillReachesOne()
    {
        var stick = new GamepadStick(1f, 0f).WithDeadzone(0.25f);

        Assert.Equal(1f, stick.Magnitude, 3);
    }


    [Fact]
    public void ZeroDeadzoneLeavesTheStickUntouched()
    {
        var raw = new GamepadStick(0.03f, -0.02f);

        Assert.Equal(raw, raw.WithDeadzone(0f));
    }


    [Fact]
    public void TheDPadReadsAsAStick()
    {
        var state = new GamepadState { Buttons = GamepadButton.DPadUp | GamepadButton.DPadRight };

        Assert.Equal(new GamepadStick(1f, 1f), state.DPad);
    }


    [Fact]
    public void OppositeDPadDirectionsCancel()
    {
        var state = new GamepadState { Buttons = GamepadButton.DPadLeft | GamepadButton.DPadRight };

        Assert.Equal(0f, state.DPad.X);
    }
}


/// <summary>
/// Reading a state - the chord and edge-detection helpers a game loop leans on.
/// </summary>
public class StateReadTests
{
    [Fact]
    public void IsPressedWithSeveralButtonsMeansAll()
    {
        var state = new GamepadState { Buttons = GamepadButton.A | GamepadButton.LeftShoulder };

        Assert.True(state.IsPressed(GamepadButton.A | GamepadButton.LeftShoulder));
        Assert.False(state.IsPressed(GamepadButton.A | GamepadButton.B));
    }


    [Fact]
    public void IsAnyPressedMeansEither()
    {
        var state = new GamepadState { Buttons = GamepadButton.A };

        Assert.True(state.IsAnyPressed(GamepadButton.A | GamepadButton.B));
    }


    [Fact]
    public void IsPressedOfNothingIsFalse()
    {
        var state = new GamepadState { Buttons = GamepadButton.A };

        // the mask-equality form would otherwise report true for None, and "no button is held" must
        // never read as pressed
        Assert.False(state.IsPressed(GamepadButton.None));
    }


    [Fact]
    public void EdgeDetectionReportsOnlyWhatChanged()
    {
        var previous = new GamepadState { Buttons = GamepadButton.A | GamepadButton.B };
        var current = new GamepadState { Buttons = GamepadButton.B | GamepadButton.X };

        Assert.Equal(GamepadButton.X, current.GetPressedSince(previous));
        Assert.Equal(GamepadButton.A, current.GetReleasedSince(previous));
    }


    [Fact]
    public void EveryAxisIsReachableByName()
    {
        var state = new GamepadState
        {
            LeftStick = new GamepadStick(0.1f, 0.2f),
            RightStick = new GamepadStick(0.3f, 0.4f),
            LeftTrigger = 0.5f,
            RightTrigger = 0.6f
        };

        Assert.Equal(0.1f, state.GetAxis(GamepadAxis.LeftStickX));
        Assert.Equal(0.2f, state.GetAxis(GamepadAxis.LeftStickY));
        Assert.Equal(0.3f, state.GetAxis(GamepadAxis.RightStickX));
        Assert.Equal(0.4f, state.GetAxis(GamepadAxis.RightStickY));
        Assert.Equal(0.5f, state.GetAxis(GamepadAxis.LeftTrigger));
        Assert.Equal(0.6f, state.GetAxis(GamepadAxis.RightTrigger));
    }


    [Fact]
    public void VibrationIsClampedRatherThanRejected()
    {
        var clamped = new GamepadVibration(2f, -1f, 5f, 0.5f).Clamp();

        Assert.Equal(new GamepadVibration(1f, 0f, 1f, 0.5f), clamped);
    }


    [Fact]
    public void SilenceIsDetectedAcrossEveryMotor()
    {
        Assert.True(GamepadVibration.Off.IsSilent);
        Assert.False(new GamepadVibration(0f, 0f, 0.1f, 0f).IsSilent);
    }
}
