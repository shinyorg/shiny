using Xunit;

namespace Shiny.Gamepad.Tests;


/// <summary>
/// What a controller does after it has gone away.
/// </summary>
/// <remarks>
/// The contract is deliberately asymmetric: <see cref="IGamepad.GetState"/> keeps working so a
/// render loop needs no guard, and everything that would touch hardware throws so a bug is loud
/// rather than silent.
/// </remarks>
public class DisconnectTests
{
    [Fact]
    public void GetStateKeepsReturningTheLastStateSeen()
    {
        var pad = new TestGamepad();
        pad.Push(new GamepadState { Buttons = GamepadButton.A, LeftTrigger = 1f });

        pad.SetDisconnected();

        var state = pad.GetState();
        Assert.True(state.IsPressed(GamepadButton.A));
        Assert.Equal(1f, state.LeftTrigger);
    }


    [Fact]
    public async Task HardwareCallsThrowOnceDisconnected()
    {
        var pad = new TestGamepad(GamepadCapabilities.Vibration | GamepadCapabilities.Battery);
        pad.SetDisconnected();

        await Assert.ThrowsAsync<GamepadDisconnectedException>(() => pad.SetVibration(GamepadVibration.Both(1f)));
        await Assert.ThrowsAsync<GamepadDisconnectedException>(() => pad.GetBattery());
    }


    [Fact]
    public void NoEventsAreRaisedAfterDisconnect()
    {
        var pad = new TestGamepad();
        var count = 0;
        pad.ButtonChanged += (_, _) => count++;

        pad.SetDisconnected();
        pad.Push(new GamepadState { Buttons = GamepadButton.A });

        Assert.Equal(0, count);
    }


    [Fact]
    public void DisconnectingTwiceIsHarmless()
    {
        var pad = new TestGamepad();

        // platforms do report the same disconnect twice - Windows reconciles and raises the static
        // event, Android's listener fires alongside a failed device lookup
        pad.SetDisconnected();
        pad.SetDisconnected();

        Assert.False(pad.IsConnected);
    }


    [Fact]
    public async Task AMissingCapabilityNamesItself()
    {
        var pad = new TestGamepad(GamepadCapabilities.Vibration);

        var ex = await Assert.ThrowsAsync<GamepadNotSupportedException>(() => pad.SetLight(GamepadLight.Off));

        Assert.Equal(GamepadCapabilities.Light, ex.Capability);
    }
}
