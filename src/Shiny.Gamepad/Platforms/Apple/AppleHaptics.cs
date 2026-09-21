using CoreHaptics;
using Foundation;
using GameController;
using Microsoft.Extensions.Logging;
using ObjCRuntime;

namespace Shiny.Gamepad;


/// <summary>
/// Drives one of a controller's haptic localities - a handle or a trigger - as though it were a
/// rumble motor.
/// </summary>
/// <remarks>
/// <para>Apple does not expose motors. It exposes Core Haptics, the same engine that drives the
/// Taptic Engine in a phone, addressed per <c>GCHapticsLocality</c>. To make that behave like the
/// "set a level and leave it running" contract every other platform has, each locality gets an
/// engine playing a single continuous haptic event of infinite duration, and changing the rumble
/// strength sends a dynamic intensity parameter to the player already running.</para>
/// <para>Starting a fresh pattern for each change instead would be audible: Core Haptics ramps a
/// new event in, so a value updated per frame would stutter rather than swell.</para>
/// <para>The engine stops itself when the app is backgrounded or the controller disconnects, which
/// is why <see cref="SetIntensity"/> restarts it rather than assuming it is running.</para>
/// </remarks>
sealed class AppleHapticsChannel : IDisposable
{
    readonly ILogger logger;
    readonly string gamepadId;
    readonly CHHapticEngine engine;
    ICHHapticAdvancedPatternPlayer? player;
    bool engineRunning;
    bool broken;
    float intensity;


    AppleHapticsChannel(ILogger logger, string gamepadId, CHHapticEngine engine)
    {
        this.logger = logger;
        this.gamepadId = gamepadId;
        this.engine = engine;

        // an engine that dies (app suspended, controller asleep) must not leave us thinking a
        // player is alive - the next SetIntensity has to build a new one from scratch
        this.engine.StoppedHandler = reason =>
        {
            this.engineRunning = false;
            this.player = null;
            this.logger.HapticEngineStopped(this.gamepadId, reason.ToString());
        };

        // the system can reset the engine underneath us; the pattern has to be rebuilt after
        this.engine.ResetHandler = () =>
        {
            this.engineRunning = false;
            this.player = null;
        };
    }


    /// <summary>Opens an engine for one locality, or null when the controller has no haptics there.</summary>
    /// <remarks>
    /// macOS binds <c>createEngineWithLocality:</c> as returning a bare <c>NSObject</c> where the
    /// other three Apple platforms return a <c>CHHapticEngine</c> - Core Haptics reached macOS
    /// after the binding was written and it was never tightened. Re-wrapping the handle gives the
    /// typed engine on every platform, and costs nothing where the binding was already right.
    /// </remarks>
    public static AppleHapticsChannel? TryCreate(ILogger logger, string gamepadId, GCDeviceHaptics haptics, NSString locality)
    {
        var created = haptics.CreateEngine(locality);
        if (created == null)
            return null;

        var engine = created as CHHapticEngine ?? Runtime.GetNSObject<CHHapticEngine>(created.Handle);

        return engine == null ? null : new AppleHapticsChannel(logger, gamepadId, engine);
    }


    /// <summary>
    /// Sets how hard this locality vibrates, 0 to 1, starting or stopping the engine as needed.
    /// </summary>
    public void SetIntensity(float value)
    {
        if (this.broken)
            return;

        value = Math.Clamp(value, 0f, 1f);

        // Core Haptics will not play a zero-intensity event, so "off" is a stop rather than a level
        if (value <= 0f)
        {
            this.intensity = 0f;
            this.StopPlayer();
            return;
        }

        try
        {
            this.EnsureRunning();

            if (this.player == null)
                return;

            if (this.intensity <= 0f)
                this.player.Start(0, out _);

            this.player.Send(
                [new CHHapticDynamicParameter(CHHapticDynamicParameterId.HapticIntensityControl, value, 0)],
                0,
                out _
            );
            this.intensity = value;
        }
        catch (Exception ex)
        {
            // one broken locality must not take the whole SetVibration call down - the other
            // handle may well be fine, and a silent motor beats a thrown call in a game loop
            this.broken = true;
            this.logger.HapticEngineFailed(this.gamepadId, ex);
        }
    }


    void EnsureRunning()
    {
        if (!this.engineRunning)
        {
            if (!this.engine.Start(out var startError))
            {
                this.broken = true;
                this.logger.HapticEngineFailed(this.gamepadId, ToException(startError, "the haptic engine would not start"));
                return;
            }
            this.engineRunning = true;
            this.player = null;
        }

        if (this.player != null)
            return;

        // sharpness at 0.5 is the neutral setting - it is a timbre control, not a strength one, and
        // is left alone so intensity is the only thing SetIntensity moves
        var hapticEvent = new CHHapticEvent(
            CHHapticEventType.HapticContinuous,
            [
                new CHHapticEventParameter(CHHapticEventParameterId.HapticIntensity, 1f),
                new CHHapticEventParameter(CHHapticEventParameterId.HapticSharpness, 0.5f)
            ],
            0,
            GCDeviceHaptics.HapticDurationInfinite
        );

        var pattern = new CHHapticPattern([hapticEvent], Array.Empty<CHHapticDynamicParameter>(), out var patternError);
        if (patternError != null)
        {
            this.broken = true;
            this.logger.HapticEngineFailed(this.gamepadId, ToException(patternError, "the haptic pattern was rejected"));
            return;
        }

        this.player = this.engine.CreateAdvancedPlayer(pattern, out var playerError);
        if (playerError != null)
        {
            this.player = null;
            this.broken = true;
            this.logger.HapticEngineFailed(this.gamepadId, ToException(playerError, "the haptic player could not be created"));
        }
    }


    // Core Haptics signals failure both by returning false and by handing back an NSError, and
    // not always both at once - a false with no error still has to surface as something loggable
    static Exception ToException(NSError? error, string fallback)
        => error == null ? new GamepadException(fallback) : new NSErrorException(error);


    void StopPlayer()
    {
        try
        {
            this.player?.Stop(0, out _);
        }
        catch (Exception ex)
        {
            this.logger.HapticEngineFailed(this.gamepadId, ex);
        }
    }


    public void Dispose()
    {
        this.StopPlayer();
        this.player = null;

        try
        {
            if (this.engineRunning)
                this.engine.Stop(_ => { });
        }
        catch (Exception ex)
        {
            this.logger.HapticEngineFailed(this.gamepadId, ex);
        }

        this.engine.Dispose();
    }
}
