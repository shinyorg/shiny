using Xunit;

namespace Shiny.Gamepad.Tests;


/// <summary>The helpers built on top of the interfaces.</summary>
public class ExtensionTests
{
    [Fact]
    public async Task APulseStopsTheMotorsWhenItEnds()
    {
        var pad = new TestGamepad(GamepadCapabilities.Vibration);

        await pad.Pulse(0.8f, TimeSpan.FromMilliseconds(20));

        Assert.Equal(GamepadVibration.Off, pad.LastVibration);
    }


    [Fact]
    public async Task ACancelledPulseStillStopsTheMotors()
    {
        var pad = new TestGamepad(GamepadCapabilities.Vibration);
        using var cts = new CancellationTokenSource();

        var pulse = pad.Pulse(1f, TimeSpan.FromSeconds(30), cts.Token);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pulse);

        // a cancelled pulse that left the controller buzzing would be worse than one that never ran
        Assert.Equal(GamepadVibration.Off, pad.LastVibration);
    }


    [Fact]
    public async Task WaitForButtonReturnsTheButtonThatWasPressed()
    {
        var pad = new TestGamepad();
        var wait = pad.WaitForButton(GamepadButton.Start | GamepadButton.A);

        await Task.Delay(20);
        pad.Push(new GamepadState { Buttons = GamepadButton.Start });

        Assert.Equal(GamepadButton.Start, await wait);
    }


    [Fact]
    public async Task WaitForButtonIgnoresButtonsItWasNotAskedAbout()
    {
        var pad = new TestGamepad();
        using var cts = new CancellationTokenSource();
        var wait = pad.WaitForButton(GamepadButton.Start, cts.Token);

        await Task.Delay(20);
        pad.Push(new GamepadState { Buttons = GamepadButton.A });
        await Task.Delay(20);

        Assert.False(wait.IsCompleted);

        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }


    [Fact]
    public async Task WaitForButtonGivesUpWhenTheControllerDisconnects()
    {
        var pad = new TestGamepad();
        var wait = pad.WaitForButton();

        await Task.Delay(20);
        pad.SetDisconnected();

        // no further ButtonChanged is ever coming from an inert gamepad, so the caller has to be
        // released rather than left waiting forever
        await Assert.ThrowsAsync<GamepadDisconnectedException>(() => wait);
    }


    [Fact]
    public async Task WaitForGamepadReturnsOneThatIsAlreadyConnected()
    {
        var manager = new TestManager();

        var pad = await manager.WaitForGamepad();

        Assert.NotNull(pad);
    }


    [Fact]
    public void SupportsChecksEveryFlagAsked()
    {
        var pad = new TestGamepad(GamepadCapabilities.Vibration | GamepadCapabilities.Battery);

        Assert.True(pad.Supports(GamepadCapabilities.Vibration | GamepadCapabilities.Battery));
        Assert.False(pad.Supports(GamepadCapabilities.Vibration | GamepadCapabilities.Motion));
    }


    [Fact]
    public void MovementAndLookApplyTheDeadzone()
    {
        var state = new GamepadState
        {
            LeftStick = new GamepadStick(0.05f, 0.05f),
            RightStick = new GamepadStick(0.9f, 0f)
        };

        Assert.Equal(GamepadStick.Neutral, state.GetMovement());
        Assert.True(state.GetLook().X > 0.5f);
    }
}
