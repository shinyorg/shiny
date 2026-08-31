namespace Shiny.ScreenRecorder;


/// <summary>
/// The screen recording features the current platform can actually perform.
/// </summary>
/// <remarks>
/// <para>Screen recording is exposed very unevenly. Apple's mobile platforms can only record the
/// app's own UI, Windows.Graphics.Capture has no audio path whatsoever, the Linux portal and the
/// browser both put target selection in the compositor's hands rather than yours, and pause is
/// native in exactly one place. Rather than pretend otherwise, every recorder reports what it can
/// do here and throws <see cref="ScreenRecorderNotSupportedException"/> - naming the specific
/// platform limit - for the rest.</para>
/// <para>Check the flag before offering the feature in your UI; catching the exception afterwards
/// is the fallback, not the plan.</para>
/// <para>Flags can differ between two devices running the same OS - the macOS 15 recording path
/// cannot pause where the macOS 12-14 one can, and Windows reports nothing at all where
/// <c>GraphicsCaptureSession.IsSupported()</c> is false. Read them off the instance, never assume
/// them from the target framework.</para>
/// </remarks>
[Flags]
public enum ScreenRecorderCapabilities
{
    /// <summary>Nothing is available - no capture API, or the OS refused to expose one.</summary>
    None = 0,

    /// <summary><see cref="IScreenRecorder.Start"/> can record video.</summary>
    Recording = 1,

    /// <summary>
    /// <see cref="IScreenRecording.Pause"/> and <see cref="IScreenRecording.Resume"/> work.
    /// </summary>
    /// <remarks>
    /// Only the browser pauses natively. Where the flag is set on a native platform the pause is
    /// synthesised - frames are dropped and later timestamps are shifted back - so the output has
    /// no gap but the wall clock and <see cref="IScreenRecording.Elapsed"/> will disagree.
    /// </remarks>
    PauseResume = 2,

    /// <summary>The microphone can be mixed into the recording.</summary>
    Microphone = 4,

    /// <summary>
    /// Audio the device is playing can be captured.
    /// </summary>
    /// <remarks>
    /// What "system" covers narrows as you move down the list: macOS and Linux capture everything
    /// the machine is playing, iOS and Android capture only the app's own audio (and on Android
    /// only from apps that permit capture), and a browser only ever offers the audio of the tab
    /// the user picked.
    /// </remarks>
    SystemAudio = 8,

    /// <summary>
    /// <see cref="IScreenRecorder.GetTargets"/> lists displays, and one can be passed to
    /// <see cref="IScreenRecorder.Start"/>.
    /// </summary>
    DisplaySelection = 16,

    /// <summary>
    /// <see cref="IScreenRecorder.GetTargets"/> lists individual windows, and one can be recorded
    /// on its own.
    /// </summary>
    WindowSelection = 32,

    /// <summary><see cref="ScreenRecordingRequest.ShowCursor"/> is honoured.</summary>
    CursorToggle = 64,

    /// <summary><see cref="ScreenRecordingRequest.FrameRate"/> is honoured.</summary>
    FrameRateControl = 128,

    /// <summary><see cref="ScreenRecordingRequest.VideoBitrate"/> is honoured.</summary>
    BitrateControl = 256,

    /// <summary><see cref="ScreenRecordingRequest.MaxWidth"/> is honoured.</summary>
    Downscaling = 512
}


/// <summary>
/// Where the recorder is in its lifecycle.
/// </summary>
public enum ScreenRecorderState
{
    /// <summary>Nothing is being recorded. The recorder is ready to start.</summary>
    Idle,

    /// <summary>
    /// A start is in flight - asking for consent, raising a foreground service, negotiating with
    /// the compositor. May still fail or be declined.
    /// </summary>
    Starting,

    /// <summary>Frames are being written.</summary>
    Recording,

    /// <summary>Started, but frames are currently being dropped.</summary>
    Paused,

    /// <summary>A stop is in flight - flushing the encoder and finalising the container.</summary>
    Stopping
}


/// <summary>
/// What a <see cref="CaptureTarget"/> refers to.
/// </summary>
public enum CaptureTargetKind
{
    /// <summary>A whole display.</summary>
    Display,

    /// <summary>A single window, recorded even when other windows sit on top of it.</summary>
    Window,

    /// <summary>Every window belonging to one running application.</summary>
    Application
}


/// <summary>
/// Why a recording ended without <see cref="IScreenRecording.Stop"/> being called.
/// </summary>
public enum ScreenRecordingFaultReason
{
    /// <summary>The cause could not be determined. <see cref="ScreenRecordingFaultedEventArgs.Exception"/> may say more.</summary>
    Unknown,

    /// <summary>
    /// The user revoked the capture - Android's projection notification, the browser's "Stop
    /// sharing" bar, the macOS menu-bar stop button.
    /// </summary>
    RevokedByUser,

    /// <summary>
    /// The OS pre-empted the recording - an incoming call on iOS, a foreground-service timeout on
    /// Android, the screen locking.
    /// </summary>
    InterruptedBySystem,

    /// <summary>The thing being recorded went away - a monitor unplugged, a window closed.</summary>
    TargetLost,

    /// <summary>The encoder or the muxer failed. The output file is likely unusable.</summary>
    EncoderFailed,

    /// <summary>
    /// <see cref="ScreenRecordingRequest.MaxDuration"/> elapsed. The recording was stopped
    /// cleanly and the file is complete.
    /// </summary>
    MaxDurationReached
}
