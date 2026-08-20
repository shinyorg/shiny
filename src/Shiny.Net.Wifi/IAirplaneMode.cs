namespace Shiny.Net.Wifi;


/// <summary>
/// Reads - and where the OS permits, sets - airplane mode.
/// </summary>
/// <remarks>
/// <para>No third-party app on Android, iOS or macOS can toggle airplane mode; the setting is
/// guarded by a signature-level permission on Android and simply has no API on Apple's platforms.
/// Only Linux (NetworkManager) and Windows (by switching every radio, which is what the Windows
/// airplane mode toggle itself does) can actually flip it.</para>
/// <para>Reading is more widely available than writing: Android exposes the real setting, Windows
/// infers it from the radios, and Apple exposes nothing either way. Where you cannot set it,
/// <see cref="OpenSettings"/> puts the user one tap from the switch - it works on every platform
/// that has a settings UI, which is all of them bar plain server .NET.</para>
/// </remarks>
public interface IAirplaneMode
{
    /// <summary>Whether <see cref="IsEnabled"/> reflects the real setting on this platform.</summary>
    bool IsSupported { get; }

    /// <summary>Whether <see cref="SetEnabled"/> will work on this platform.</summary>
    bool CanToggle { get; }

    /// <summary>
    /// True when airplane mode is on. Always false where <see cref="IsSupported"/> is false -
    /// check that flag rather than reading this as "airplane mode is off".
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>Fires when airplane mode is switched on or off.</summary>
    event EventHandler<bool>? Changed;

    /// <summary>
    /// Turns airplane mode on or off.
    /// </summary>
    /// <exception cref="WifiNotSupportedException">
    /// Everywhere except Linux and Windows. Call <see cref="OpenSettings"/> instead.
    /// </exception>
    Task SetEnabled(bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Sends the user to the system settings screen carrying the airplane mode switch, as close to
    /// it as the platform allows. This is the fallback wherever <see cref="CanToggle"/> is false.
    /// </summary>
    /// <exception cref="WifiNotSupportedException">Headless .NET, which has no settings UI to open.</exception>
    Task OpenSettings(CancellationToken ct = default);
}
