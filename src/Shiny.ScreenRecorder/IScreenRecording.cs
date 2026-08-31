namespace Shiny.ScreenRecorder;


/// <summary>
/// A recording in flight. Stop it to get the file, or dispose it to throw the file away.
/// </summary>
/// <remarks>
/// Obtained from <see cref="IScreenRecorder.Start"/> and valid until <see cref="Stop"/>,
/// <see cref="Cancel"/> or disposal. Every call on a finished session throws
/// <see cref="ObjectDisposedException"/> except <see cref="Stop"/>, which returns the result it
/// already produced so a double-stop is harmless.
/// </remarks>
public interface IScreenRecording : IAsyncDisposable
{
    /// <summary>
    /// How much footage has been captured, which is wall-clock time minus any paused span.
    /// </summary>
    /// <remarks>
    /// Matches the duration of the finished file rather than the time since <see cref="IScreenRecorder.Start"/>
    /// returned. Safe to poll on a UI timer.
    /// </remarks>
    TimeSpan Elapsed { get; }

    /// <summary>Whether frames are currently being dropped.</summary>
    bool IsPaused { get; }

    /// <summary>
    /// Stops writing frames without ending the recording.
    /// </summary>
    /// <remarks>
    /// Only the browser pauses natively. Elsewhere this drops incoming frames and shifts later
    /// timestamps back so the output has no gap - the encoder keeps running, so a long pause still
    /// costs battery. Pausing an already-paused recording does nothing.
    /// </remarks>
    /// <exception cref="ScreenRecorderNotSupportedException"><see cref="ScreenRecorderCapabilities.PauseResume"/> is unavailable.</exception>
    Task Pause(CancellationToken ct = default);

    /// <summary>Resumes after <see cref="Pause"/>. Does nothing if not paused.</summary>
    /// <exception cref="ScreenRecorderNotSupportedException"><see cref="ScreenRecorderCapabilities.PauseResume"/> is unavailable.</exception>
    Task Resume(CancellationToken ct = default);

    /// <summary>
    /// Ends the recording, finalises the file and returns it.
    /// </summary>
    /// <remarks>
    /// Flushing the encoder and writing the container index is not instant - expect this to take a
    /// moment on a long recording, and do not kill the process while it runs or the file will have
    /// no index and will not play. Calling it twice returns the same result rather than failing.
    /// </remarks>
    /// <exception cref="ScreenRecorderException">The encoder failed and the output is unusable.</exception>
    Task<ScreenRecordingResult> Stop(CancellationToken ct = default);

    /// <summary>
    /// Ends the recording and deletes what was captured.
    /// </summary>
    /// <remarks>
    /// The file is removed where the platform wrote one. Safe to call after <see cref="Stop"/>, in
    /// which case it deletes the finished file too.
    /// </remarks>
    Task Cancel(CancellationToken ct = default);

    /// <summary>
    /// Fires when the recording ended without you asking - the user revoked the capture, the OS
    /// pre-empted it, the target went away, or the encoder failed.
    /// </summary>
    /// <remarks>
    /// <para>By the time this fires the session is finished; <see cref="Stop"/> will return
    /// whatever was salvaged rather than continuing. Where the file is intact,
    /// <see cref="ScreenRecordingFaultedEventArgs.Result"/> carries it - a
    /// <see cref="ScreenRecordingFaultReason.MaxDurationReached"/> fault always does, and a
    /// <see cref="ScreenRecordingFaultReason.RevokedByUser"/> one usually does.</para>
    /// <para>Raised on a native callback thread. Marshal before touching UI.</para>
    /// </remarks>
    event EventHandler<ScreenRecordingFaultedEventArgs>? Faulted;
}


/// <summary>Describes a recording that ended on its own.</summary>
/// <param name="Reason">Why it ended.</param>
/// <param name="Result">The salvaged file, where there is one. Null when nothing usable was written.</param>
/// <param name="Exception">The underlying failure, where the platform reported one.</param>
public record ScreenRecordingFaultedEventArgs(
    ScreenRecordingFaultReason Reason,
    ScreenRecordingResult? Result,
    Exception? Exception
);
