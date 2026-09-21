namespace Shiny.Gamepad;


/// <summary>
/// How hard to drive each of a controller's motors, 0 (off) to 1 (flat out).
/// </summary>
/// <remarks>
/// <para>The two handle motors are not the same part. <see cref="LowFrequency"/> is the heavy
/// off-centre weight in the left handle - a low rumble you feel in the palm, for impacts and
/// engines. <see cref="HighFrequency"/> is the smaller, faster weight in the right handle - a
/// buzz, for clicks and surface texture. Driving both at once reads as "everything is shaking",
/// which is rarely what a moment calls for.</para>
/// <para><see cref="LeftTrigger"/> and <see cref="RightTrigger"/> reach motors inside the triggers
/// themselves and are ignored unless <see cref="GamepadCapabilities.TriggerVibration"/> is set.
/// They are deliberately not mixed into the handles when unsupported: a trigger effect that
/// silently becomes a whole-pad rumble is worse than one that does nothing.</para>
/// </remarks>
/// <param name="LowFrequency">The heavy left-handle motor, 0 to 1.</param>
/// <param name="HighFrequency">The light right-handle motor, 0 to 1.</param>
/// <param name="LeftTrigger">The left trigger motor, 0 to 1. Needs <see cref="GamepadCapabilities.TriggerVibration"/>.</param>
/// <param name="RightTrigger">The right trigger motor, 0 to 1. Needs <see cref="GamepadCapabilities.TriggerVibration"/>.</param>
public readonly record struct GamepadVibration(
    float LowFrequency,
    float HighFrequency,
    float LeftTrigger = 0f,
    float RightTrigger = 0f
)
{
    /// <summary>Every motor off. Pass this to <see cref="IGamepad.SetVibration"/> to stop.</summary>
    public static readonly GamepadVibration Off = new(0f, 0f);

    /// <summary>Both handle motors at the same strength.</summary>
    public static GamepadVibration Both(float intensity) => new(intensity, intensity);

    /// <summary>Clamps every motor into 0..1. Called for you before the values reach the hardware.</summary>
    public GamepadVibration Clamp() => new(
        Math.Clamp(this.LowFrequency, 0f, 1f),
        Math.Clamp(this.HighFrequency, 0f, 1f),
        Math.Clamp(this.LeftTrigger, 0f, 1f),
        Math.Clamp(this.RightTrigger, 0f, 1f)
    );

    /// <summary>Whether every motor is off.</summary>
    public bool IsSilent => this is { LowFrequency: <= 0f, HighFrequency: <= 0f, LeftTrigger: <= 0f, RightTrigger: <= 0f };
}


/// <summary>
/// A controller's battery.
/// </summary>
/// <param name="Level">How full, 0 to 1, or null when the platform reports a state but no number.</param>
/// <param name="State">Whether it is charging.</param>
/// <remarks>
/// Most controllers report this coarsely - four steps is common, and some only ever say "fine" or
/// "nearly empty". Treat <paramref name="Level"/> as a gauge to draw, not a number to do
/// arithmetic on.
/// </remarks>
public readonly record struct GamepadBattery(float? Level, GamepadBatteryState State);


/// <summary>
/// A colour for a controller's light bar, as three channels from 0 to 1.
/// </summary>
/// <remarks>
/// Controllers vary wildly in how faithfully they render this. A DualSense has a real RGB bar; an
/// Xbox pad has no light at all; several Android controllers only expose a four-way player
/// indicator, where the nearest player slot is lit instead of the colour asked for.
/// </remarks>
public readonly record struct GamepadLight(float Red, float Green, float Blue)
{
    /// <summary>The light off, where the hardware allows it.</summary>
    public static readonly GamepadLight Off = new(0f, 0f, 0f);

    /// <summary>Builds a colour from three 0-255 channels.</summary>
    public static GamepadLight FromRgb(byte red, byte green, byte blue)
        => new(red / 255f, green / 255f, blue / 255f);

    /// <summary>Clamps every channel into 0..1. Called for you before the values reach the hardware.</summary>
    public GamepadLight Clamp() => new(
        Math.Clamp(this.Red, 0f, 1f),
        Math.Clamp(this.Green, 0f, 1f),
        Math.Clamp(this.Blue, 0f, 1f)
    );
}


/// <summary>
/// A reading from the gyroscope and accelerometer built into a controller.
/// </summary>
/// <param name="AngularVelocityX">Pitch rate, radians per second.</param>
/// <param name="AngularVelocityY">Yaw rate, radians per second.</param>
/// <param name="AngularVelocityZ">Roll rate, radians per second.</param>
/// <param name="AccelerationX">Acceleration along X, in g.</param>
/// <param name="AccelerationY">Acceleration along Y, in g.</param>
/// <param name="AccelerationZ">Acceleration along Z, in g.</param>
/// <param name="Timestamp">When it was taken, from <see cref="Environment.TickCount64"/>.</param>
/// <remarks>
/// <para>Acceleration includes gravity, so a controller sitting still on a table reads about 1g on
/// whichever axis is pointing up - it is not zero. Subtract the low-passed average to get the part
/// caused by the player moving it.</para>
/// <para>Axes follow the controller's own body, with X to the right, Y up and Z out of the back
/// towards the player. Apple, Linux and Android all report in these units already; nothing is
/// converted beyond sign.</para>
/// </remarks>
public readonly record struct GamepadMotion(
    float AngularVelocityX,
    float AngularVelocityY,
    float AngularVelocityZ,
    float AccelerationX,
    float AccelerationY,
    float AccelerationZ,
    long Timestamp
);
