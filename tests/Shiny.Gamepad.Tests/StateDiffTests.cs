using Xunit;

namespace Shiny.Gamepad.Tests;


/// <summary>
/// The state-to-events machinery every backend funnels through.
/// </summary>
/// <remarks>
/// This is where the bugs that look like broken hardware live. A combined button mask raised as one
/// event loses the second of two simultaneous presses; a threshold measured against the previous
/// state instead of the last reported value lets a slow stick sweep produce no events at all; and a
/// stick that stops exactly at centre without reporting leaves the player walking into a wall.
/// </remarks>
public class StateDiffTests
{
    [Fact]
    public void TwoButtonsPressedTogetherRaiseTwoEvents()
    {
        var pad = new TestGamepad();
        var seen = new List<GamepadButton>();
        pad.ButtonChanged += (_, e) => seen.Add(e.Button);

        pad.Push(new GamepadState { Buttons = GamepadButton.A | GamepadButton.B });

        // one event per button, never a combined mask - a handler switching on e.Button would
        // otherwise silently miss B
        Assert.Equal(2, seen.Count);
        Assert.Contains(GamepadButton.A, seen);
        Assert.Contains(GamepadButton.B, seen);
    }


    [Fact]
    public void ReleaseIsReportedSeparatelyFromPress()
    {
        var pad = new TestGamepad();
        var events = new List<GamepadButtonChangedEventArgs>();
        pad.ButtonChanged += (_, e) => events.Add(e);

        pad.Push(new GamepadState { Buttons = GamepadButton.A });
        pad.Push(new GamepadState { Buttons = GamepadButton.None });

        Assert.Equal(2, events.Count);
        Assert.True(events[0].IsPressed);
        Assert.False(events[1].IsPressed);
    }


    [Fact]
    public void AnUnchangedStateRaisesNothing()
    {
        var pad = new TestGamepad();
        var count = 0;
        pad.ButtonChanged += (_, _) => count++;
        pad.AxisChanged += (_, _) => count++;

        var state = new GamepadState { Buttons = GamepadButton.A, LeftStick = new GamepadStick(0.5f, 0.5f) };
        pad.Push(state);
        pad.Push(state);
        pad.Push(state);

        // a poll loop running faster than the player must cost nothing but the comparison
        Assert.Equal(3, count); // one button press plus two axes, then nothing further
    }


    [Fact]
    public void AxisMovementBelowTheThresholdIsNotReported()
    {
        var pad = new TestGamepad { AxisChangeThreshold = 0.1f };
        var count = 0;
        pad.AxisChanged += (_, _) => count++;

        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.5f, 0f) });
        count = 0;

        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.55f, 0f) });

        Assert.Equal(0, count);
    }


    [Fact]
    public void ASlowSweepAccumulatesPastTheThreshold()
    {
        var pad = new TestGamepad { AxisChangeThreshold = 0.1f };
        var values = new List<float>();
        pad.AxisChanged += (_, e) => values.Add(e.Value);

        // every step here is 0.05, half the threshold. Compared with the previous state none of
        // them would ever report and the stick would appear frozen; compared with the last value
        // actually reported, the drift accumulates and crosses at 0.10.
        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.05f, 0f) });
        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.10f, 0f) });
        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.15f, 0f) });

        Assert.Contains(0.10f, values);

        // the last reading is within the threshold of what was reported, so it is not an event -
        // an event consumer is behind by at most the threshold, which is what the threshold means.
        // GetState is never approximated this way.
        Assert.Equal(0.15f, pad.GetState().LeftStick.X);
    }


    [Fact]
    public void ReturningExactlyToCentreAlwaysReports()
    {
        // reported at 0.4 under a loose threshold, then tightened so the return to centre is a
        // smaller step than the threshold would normally let through
        var pad = new TestGamepad { AxisChangeThreshold = 0.3f };
        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.4f, 0f) });

        var values = new List<float>();
        pad.AxisChanged += (_, e) => values.Add(e.Value);
        pad.AxisChangeThreshold = 0.5f;

        pad.Push(new GamepadState { LeftStick = new GamepadStick(0f, 0f) });

        // 0.4 to 0 is below a 0.5 threshold, but swallowing it leaves the player walking forever
        Assert.Contains(0f, values);
    }


    [Fact]
    public void FullDeflectionAlwaysReports()
    {
        var pad = new TestGamepad { AxisChangeThreshold = 0.9f };
        var values = new List<float>();

        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.5f, 0f) });
        pad.AxisChanged += (_, e) => values.Add(e.Value);

        pad.Push(new GamepadState { LeftStick = new GamepadStick(1f, 0f) });

        Assert.Contains(1f, values);
    }


    [Fact]
    public void EachAxisIsThresholdedIndependently()
    {
        var pad = new TestGamepad { AxisChangeThreshold = 0.1f };
        var axes = new List<GamepadAxis>();

        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.5f, 0.5f) });
        pad.AxisChanged += (_, e) => axes.Add(e.Axis);

        pad.Push(new GamepadState { LeftStick = new GamepadStick(0.5f, 0.9f) });

        Assert.Equal([GamepadAxis.LeftStickY], axes);
    }


    [Fact]
    public void AHandlerThatThrowsDoesNotStopTheOthers()
    {
        var pad = new TestGamepad();
        var reached = false;

        pad.ButtonChanged += (_, _) => throw new InvalidOperationException("handler bug");
        pad.ButtonChanged += (_, _) => reached = true;

        // an exception escaping here would tear down the reader thread on Linux and the input
        // callback on every other backend
        pad.Push(new GamepadState { Buttons = GamepadButton.A });

        Assert.True(reached);
    }


    [Fact]
    public void TimestampIsFilledInWhenTheBackendLeavesItUnset()
    {
        var pad = new TestGamepad();
        pad.Push(new GamepadState { Buttons = GamepadButton.A });

        Assert.True(pad.GetState().Timestamp > 0);
    }
}
