using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad.Tests;


/// <summary>
/// An <see cref="AbstractGamepad"/> whose state is driven by the test rather than by hardware.
/// </summary>
/// <remarks>
/// Everything worth testing about a gamepad without one plugged in lives in the base class: the
/// state diffing, the axis threshold, the one-event-per-button rule and the disconnected contract.
/// Driving <see cref="AbstractGamepad.UpdateState"/> directly exercises exactly what every backend
/// funnels through.
/// </remarks>
public class TestGamepad(GamepadCapabilities capabilities = GamepadCapabilities.None, string id = "test-1")
    : AbstractGamepad(id, NullLogger.Instance)
{
    public override string Name => "Test Controller";
    public override GamepadKind Kind => GamepadKind.Standard;
    public override int? PlayerIndex => 1;
    public override GamepadCapabilities Capabilities { get; } = capabilities;

    public void Push(GamepadState state) => this.UpdateState(state);
    public void PushMotion(GamepadMotion motion) => this.UpdateMotion(motion);

    public override Task SetVibration(GamepadVibration vibration, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Vibration, "the test gamepad was not given vibration");
        this.LastVibration = vibration.Clamp();

        return Task.CompletedTask;
    }

    public override Task<GamepadBattery> GetBattery(CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Battery, "the test gamepad was not given a battery");

        return Task.FromResult(new GamepadBattery(0.5f, GamepadBatteryState.Discharging));
    }

    public override Task SetLight(GamepadLight light, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Light, "the test gamepad was not given a light");

        return Task.CompletedTask;
    }

    public override Task SetMotionEnabled(bool enabled, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Motion, "the test gamepad was not given motion sensors");

        return Task.CompletedTask;
    }

    public GamepadVibration? LastVibration { get; private set; }
}
