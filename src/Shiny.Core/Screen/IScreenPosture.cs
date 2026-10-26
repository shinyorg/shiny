using System;
using System.Collections.Generic;

namespace Shiny;


/// <summary>
/// Whether the app's window is divided by a fold or hinge, where the fold is, and the hinge's state - Jetpack
/// WindowManager on Android foldables, the hinge and reserved-region APIs on iPhone Duo (iOS 27.1+), and the Device
/// Posture / Viewport Segments APIs in Chromium on Blazor WebAssembly. Registered by every Shiny host; elsewhere it
/// reports <see cref="ScreenPosture.None"/>.
/// </summary>
/// <remarks>
/// v1 reports the current Activity (Android) or key window (iOS) only.
/// </remarks>
public interface IScreenPosture
{
    /// <summary>
    /// Whether this platform can report posture at all - Android, iOS 27.1+ (not Mac Catalyst), or a browser with the
    /// Device Posture or Viewport Segments API. It says nothing about whether this device folds - see
    /// <see cref="SupportedPostures"/>.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// The posture now. <see cref="ScreenPosture.None"/> where unsupported or nothing is known yet.
    /// </summary>
    ScreenPosture Current { get; }

    /// <summary>
    /// Raised when <see cref="Current"/> changes. Watching starts with the first subscriber and stops with the last.
    /// Raised on the platform's main thread on Android and iOS.
    /// </summary>
    event EventHandler<ScreenPosture>? Changed;

    /// <summary>
    /// The fold orientations this device can take while half open - Android: <see cref="FoldOrientation.Horizontal"/>
    /// when WindowManager reports tabletop support, plus any orientation a fold has been seen in; iOS: both once a
    /// hinge has been reported; empty when nothing is known.
    /// </summary>
    IReadOnlyList<FoldOrientation> SupportedPostures { get; }
}
