using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Gamepad.Infrastructure;
using Xunit;

namespace Shiny.Gamepad.Tests;


class TestManager : AbstractGamepadManager
{
    public TestManager() : base(NullLogger.Instance) { }

    public int StartCount { get; private set; }

    protected override Task OnStart(CancellationToken ct)
    {
        this.StartCount++;
        this.Add(new TestGamepad());

        return Task.CompletedTask;
    }

    public void Connect(AbstractGamepad gamepad) => this.Add(gamepad);
    public void Disconnect(string id) => this.Remove(id);
}


class FailingManager : AbstractGamepadManager
{
    public FailingManager() : base(NullLogger.Instance) { }

    public int Attempts { get; private set; }

    protected override Task OnStart(CancellationToken ct)
    {
        this.Attempts++;

        if (this.Attempts == 1)
            throw new GamepadException("the device node was not ready");

        return Task.CompletedTask;
    }
}


/// <summary>
/// The manager's bookkeeping: starting once, not double-reporting a controller, and pushing the
/// axis threshold down to the gamepads it owns.
/// </summary>
public class ManagerTests
{
    [Fact]
    public async Task TheWatchStartsOnceHoweverOftenItIsAsked()
    {
        var manager = new TestManager();

        await manager.GetGamepads();
        await manager.GetGamepads();
        await manager.GetGamepads();

        Assert.Equal(1, manager.StartCount);
    }


    [Fact]
    public async Task ConstructionDoesNotTouchHardware()
    {
        var manager = new TestManager();

        // resolving the service in a console host that never asks for a controller must not open a
        // device node, subscribe to a notification centre or hook an activity
        Assert.Equal(0, manager.StartCount);

        await manager.GetGamepads();
        Assert.Equal(1, manager.StartCount);
    }


    [Fact]
    public async Task AFailedStartIsRetriedRatherThanLatched()
    {
        var manager = new FailingManager();

        await Assert.ThrowsAsync<GamepadException>(() => manager.GetGamepads());

        var second = await manager.GetGamepads();

        Assert.Equal(2, manager.Attempts);
        Assert.Empty(second);
    }


    [Fact]
    public async Task TheSameControllerAnnouncedTwiceIsReportedOnce()
    {
        var manager = new TestManager();
        var connects = 0;
        manager.Connected += (_, _) => connects++;

        await manager.GetGamepads();
        manager.Connect(new TestGamepad());

        var gamepads = await manager.GetGamepads();

        Assert.Single(gamepads);
        Assert.Equal(1, connects);
    }


    [Fact]
    public async Task DisconnectRemovesAndMarksTheGamepad()
    {
        var manager = new TestManager();
        IGamepad? disconnected = null;
        manager.Disconnected += (_, e) => disconnected = e.Gamepad;

        var gamepads = await manager.GetGamepads();
        manager.Disconnect(gamepads[0].Id);

        Assert.NotNull(disconnected);
        Assert.False(disconnected!.IsConnected);
        Assert.Empty(await manager.GetGamepads());
    }


    [Fact]
    public async Task TheAxisThresholdReachesGamepadsConnectedBeforeAndAfter()
    {
        var manager = new TestManager();
        await manager.GetGamepads();

        manager.AxisChangeThreshold = 0.25f;
        var late = new TestGamepad(id: "test-2");
        manager.Connect(late);

        var early = (TestGamepad)(await manager.GetGamepads()).First(x => !ReferenceEquals(x, late));

        Assert.Equal(0.25f, early.AxisChangeThreshold);
        Assert.Equal(0.25f, late.AxisChangeThreshold);
    }


    [Fact]
    public void AnOutOfRangeThresholdIsRejected()
    {
        var manager = new TestManager();

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AxisChangeThreshold = -0.1f);
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.AxisChangeThreshold = 1.5f);
    }


    [Fact]
    public void AZeroPollIntervalIsRejected()
    {
        var manager = new TestManager();

        Assert.Throws<ArgumentOutOfRangeException>(() => manager.PollInterval = TimeSpan.Zero);
    }


    [Fact]
    public async Task AHandlerThatThrowsDoesNotStopTheConnectionBeingReported()
    {
        var manager = new TestManager();
        var reached = false;

        manager.Connected += (_, _) => throw new InvalidOperationException("handler bug");
        manager.Connected += (_, _) => reached = true;

        await manager.GetGamepads();

        Assert.True(reached);
    }
}
