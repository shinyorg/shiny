using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad;


/// <summary>A controller as the browser describes it, before any state has arrived.</summary>
/// <param name="Index">The slot in <c>navigator.getGamepads()</c> - the browser's own identity for it.</param>
/// <param name="Id">The browser's name for the controller, usually including vendor and product ids.</param>
/// <param name="Mapping">"standard" when the browser has normalised the layout, empty when it has not.</param>
/// <param name="Connected">Whether it was connected when described.</param>
/// <param name="CanVibrate">Whether the browser exposes a vibration actuator for it.</param>
public record BlazorGamepadInfo(int Index, string Id, string Mapping, bool Connected, bool CanVibrate);


/// <summary>One controller's state, as the browser-side loop packs it.</summary>
/// <remarks>
/// The button field is a bitmask built in JavaScript using the same bit positions as
/// <see cref="GamepadButton"/>. Packing it there means one number crosses the interop boundary per
/// controller per changed frame instead of an array of seventeen booleans.
/// </remarks>
public record BlazorGamepadState(
    int Index,
    ulong Buttons,
    float LeftX,
    float LeftY,
    float RightX,
    float RightY,
    float LeftTrigger,
    float RightTrigger
);


/// <summary>
/// A controller as the W3C Gamepad API reports it.
/// </summary>
/// <remarks>
/// <para>The browser gives the least of any platform here, and does so deliberately. There is no
/// battery, no motion sensor and no light bar in the specification - every one of them is a
/// fingerprinting surface - and the controller itself stays invisible until the player presses
/// something on it, for the same reason. Vibration is the one extra, and only in browsers that
/// implement <c>vibrationActuator</c>.</para>
/// <para><see cref="AbstractGamepad.GetState"/> returns the last state the animation-frame loop
/// saw, so it can be up to one frame old. The API offers nothing fresher: <c>getGamepads()</c> is
/// itself a per-frame snapshot, and calling it more often than the display refreshes returns the
/// same values.</para>
/// <para>A controller the browser has not normalised - <see cref="BlazorGamepadInfo.Mapping"/> is
/// not "standard" - is still reported, with its buttons mapped by index. That is a guess, and the
/// wrong one for some hardware, but reporting the controller and getting some buttons wrong is more
/// useful than pretending it is not there.</para>
/// </remarks>
public class BlazorGamepad : AbstractGamepad
{
    readonly ILogger logger;
    readonly Func<Task<IJSObjectReference>> moduleAccessor;
    readonly BlazorGamepadInfo info;
    readonly GamepadCapabilities capabilities;


    internal BlazorGamepad(
        string id,
        BlazorGamepadInfo info,
        Func<Task<IJSObjectReference>> moduleAccessor,
        ILogger logger
    ) : base(id, logger)
    {
        this.logger = logger;
        this.info = info;
        this.moduleAccessor = moduleAccessor;
        this.Name = info.Id;
        this.Kind = DetectKind(info.Id);
        this.capabilities = info.CanVibrate ? GamepadCapabilities.Vibration : GamepadCapabilities.None;
    }


    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override GamepadKind Kind { get; }

    /// <inheritdoc/>
    public override GamepadCapabilities Capabilities => this.capabilities;

    /// <summary>
    /// The browser's slot for this controller. Not a player index - the browser assigns no player
    /// numbers - so <see cref="PlayerIndex"/> stays null.
    /// </summary>
    public int Index => this.info.Index;

    /// <inheritdoc/>
    public override int? PlayerIndex => null;


    static GamepadKind DetectKind(string id)
    {
        // the browser's id string is free-form and browser-specific, but every engine includes the
        // USB vendor and product ids in it for a controller connected over USB or Bluetooth HID
        if (id.Contains("054c", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("DualSense", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("DualShock", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.PlayStation;

        if (id.Contains("045e", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("Xbox", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Xbox;

        if (id.Contains("057e", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("Switch", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("Joy-Con", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Nintendo;

        if (id.Contains("28de", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("Steam", StringComparison.OrdinalIgnoreCase))
            return GamepadKind.Steam;

        return GamepadKind.Standard;
    }


    /// <summary>Takes one packed state from the browser loop.</summary>
    internal void Apply(BlazorGamepadState state)
    {
        var buttons = (GamepadButton)state.Buttons;

        if (state.LeftTrigger >= GamepadState.TriggerPressThreshold)
            buttons |= GamepadButton.LeftTrigger;

        if (state.RightTrigger >= GamepadState.TriggerPressThreshold)
            buttons |= GamepadButton.RightTrigger;

        this.UpdateState(new Gamepad.GamepadState
        {
            Buttons = buttons,
            LeftStick = new GamepadStick(state.LeftX, state.LeftY),
            RightStick = new GamepadStick(state.RightX, state.RightY),
            LeftTrigger = state.LeftTrigger,
            RightTrigger = state.RightTrigger
        });
    }


    /// <inheritdoc/>
    public override async Task SetVibration(GamepadVibration vibration, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Vibration, "this browser exposes no vibrationActuator for the controller");

        vibration = vibration.Clamp();

        try
        {
            var module = await this.moduleAccessor().ConfigureAwait(false);
            await module
                .InvokeAsync<bool>("setVibration", ct, this.info.Index, vibration.LowFrequency, vibration.HighFrequency)
                .ConfigureAwait(false);
        }
        catch (JSException ex)
        {
            this.logger.VibrationFailed(this.Id, ex);
        }
    }


    /// <inheritdoc/>
    public override Task<GamepadBattery> GetBattery(CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Battery, "the W3C Gamepad API exposes no battery - it is a fingerprinting surface and was left out deliberately");

        return Task.FromResult(new GamepadBattery(null, GamepadBatteryState.Unknown));
    }


    /// <inheritdoc/>
    public override Task SetLight(GamepadLight light, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Light, "the W3C Gamepad API exposes no lights");

        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    public override Task SetMotionEnabled(bool enabled, CancellationToken ct = default)
    {
        this.AssertConnected();
        this.AssertCapability(GamepadCapabilities.Motion, "the W3C Gamepad API exposes no motion sensors");

        return Task.CompletedTask;
    }
}
