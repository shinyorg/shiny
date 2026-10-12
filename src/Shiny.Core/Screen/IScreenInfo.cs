using System;
using System.Threading.Tasks;

namespace Shiny;


/// <summary>
/// The main display - its size, density, refresh rate and orientation, and changes to them - plus programmatic
/// orientation locking. Registered by every Shiny host.
/// </summary>
public interface IScreenInfo
{
    /// <summary>
    /// A snapshot of the main display. <see cref="ScreenMetrics.Empty"/> where the platform has none to report.
    /// </summary>
    ScreenMetrics Metrics { get; }

    /// <summary>
    /// The orientation the app is displayed in - <see cref="ScreenMetrics.Orientation"/> of <see cref="Metrics"/>.
    /// </summary>
    DeviceOrientation Orientation { get; }

    /// <summary>
    /// Raised when any of <see cref="Metrics"/> changes - a rotation, a resolution or scale change, or the main
    /// display changing. Watching starts with the first subscriber and stops with the last.
    /// </summary>
    event EventHandler<ScreenMetrics>? MetricsChanged;

    /// <summary>
    /// Raised when the app turns between portrait and landscape.
    /// </summary>
    event EventHandler<DeviceOrientation>? OrientationChanged;

    /// <summary>
    /// Turns the app to <paramref name="orientation"/> and holds it there. Answers whether it applied - false where
    /// the platform cannot (iOS before 16, Mac Catalyst, macOS, tvOS, Linux) or has nothing to apply it to (no current
    /// Activity on Android). Passing <see cref="DeviceOrientation.Unknown"/> is <see cref="ResetOrientation"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>iOS</b>: holding the orientation needs one line in the app delegate - see <c>IosOrientationLock</c>.
    /// Without it the device turns once and is free to turn back. UIKit also ignores a change made while a
    /// navigation transition is animating, so call this once a page has appeared.
    /// </para>
    /// <para>
    /// <b>Blazor WebAssembly</b>: browsers only lock orientation for a full-screen page or an installed app.
    /// </para>
    /// </remarks>
    Task<bool> SetOrientation(DeviceOrientation orientation);

    /// <summary>
    /// Clears a lock set by <see cref="SetOrientation"/>, so the app follows the device again.
    /// </summary>
    Task<bool> ResetOrientation();
}
