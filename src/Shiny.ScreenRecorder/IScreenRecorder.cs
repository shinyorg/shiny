namespace Shiny.ScreenRecorder;


/// <summary>
/// Records the screen to a video file.
/// </summary>
/// <remarks>
/// <para>Backed by MediaProjection on Android, ReplayKit on iOS and Mac Catalyst, ScreenCaptureKit
/// on macOS, Windows.Graphics.Capture on Windows, the xdg-desktop-portal ScreenCast API on Linux
/// (via the separate <c>Shiny.ScreenRecorder.Linux</c> package) and getDisplayMedia + MediaRecorder
/// in the browser (via <c>Shiny.ScreenRecorder.Blazor</c>).</para>
/// <para><b>What gets recorded is not the same everywhere.</b> Android, macOS, Windows and Linux
/// record the system screen, so anything on it - including other apps - ends up in the file. iOS
/// and Mac Catalyst record only your own app's UI, because that is all ReplayKit offers without a
/// Broadcast Upload Extension. Plan your UX around the stricter of the two.</para>
/// <para>Read <see cref="Capabilities"/> before offering a feature; anything unavailable throws
/// <see cref="ScreenRecorderNotSupportedException"/>.</para>
/// <para>One recording at a time. <see cref="Start"/> throws
/// <see cref="ScreenRecorderException"/> while another is in flight - every platform underneath
/// has the same restriction, so serialising here fails earlier and more clearly than the native
/// error would.</para>
/// </remarks>
public interface IScreenRecorder
{
    /// <summary>What this platform can actually do. Check before offering a feature.</summary>
    ScreenRecorderCapabilities Capabilities { get; }

    /// <summary>Where the recorder is in its lifecycle.</summary>
    ScreenRecorderState State { get; }

    /// <summary>
    /// Fires whenever <see cref="State"/> moves, including when the OS ends a recording on its own.
    /// </summary>
    /// <remarks>
    /// Raised on whatever thread the transition happened on, which for the native stop callbacks is
    /// not the UI thread. Marshal before touching UI.
    /// </remarks>
    event EventHandler<ScreenRecorderState>? StateChanged;

    /// <summary>
    /// Asks for whatever the platform gates recording behind.
    /// </summary>
    /// <remarks>
    /// <para>Safe to call repeatedly - it returns the current state without re-prompting where the
    /// platform allows that. What it actually asks for depends on the request: the microphone
    /// permission is only sought when <see cref="ScreenRecordingRequest.IncludeMicrophone"/> is
    /// set.</para>
    /// <para>Not every platform can answer ahead of time. Android's consent dialog is bound to the
    /// projection it authorises and cannot be pre-granted, so this reports
    /// <see cref="AccessState.Unknown"/> there for the screen itself and the real prompt appears
    /// inside <see cref="Start"/>. Treat a non-<see cref="AccessState.Denied"/> answer as
    /// "worth trying", not as a guarantee.</para>
    /// </remarks>
    /// <param name="request">The recording you intend to start. Only the audio flags affect what is asked for.</param>
    /// <param name="ct">Cancels the request. The OS prompt may still be on screen afterwards.</param>
    Task<AccessState> RequestAccess(ScreenRecordingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Lists the displays, windows and applications that can be recorded.
    /// </summary>
    /// <remarks>
    /// Desktop only, and only where the platform lets the app enumerate rather than insisting the
    /// user pick. Linux and the browser both hand target selection to the compositor - there is no
    /// list to return, and the picker appears during <see cref="Start"/> instead. Mobile has no
    /// concept of a target at all.
    /// </remarks>
    /// <exception cref="ScreenRecorderNotSupportedException">
    /// Neither <see cref="ScreenRecorderCapabilities.DisplaySelection"/> nor
    /// <see cref="ScreenRecorderCapabilities.WindowSelection"/> is available.
    /// </exception>
    /// <exception cref="ScreenRecorderPermissionException">
    /// macOS, where enumerating shareable content needs the Screen Recording grant.
    /// </exception>
    Task<IReadOnlyList<CaptureTarget>> GetTargets(CancellationToken ct = default);

    /// <summary>
    /// Starts recording and returns the live session.
    /// </summary>
    /// <remarks>
    /// <para>Returns once frames are actually being written, not when the request was accepted - so
    /// a consent dialog, a compositor picker or a foreground-service promotion all complete before
    /// this does. That can take seconds of wall clock while the user decides.</para>
    /// <para>The returned session must be stopped or disposed. Disposing without
    /// <see cref="IScreenRecording.Stop"/> cancels the recording and deletes the partial file.</para>
    /// </remarks>
    /// <param name="request">What to record and where to put it.</param>
    /// <param name="ct">Cancels the start. A recording that already began is torn down.</param>
    /// <exception cref="ScreenRecorderNotSupportedException">The request asks for something outside <see cref="Capabilities"/>.</exception>
    /// <exception cref="ScreenRecorderPermissionException">The user declined, or a manifest entry, entitlement or usage description is missing.</exception>
    /// <exception cref="ScreenRecorderException">A recording is already in flight, or the platform failed to start one.</exception>
    Task<IScreenRecording> Start(ScreenRecordingRequest request, CancellationToken ct = default);
}
