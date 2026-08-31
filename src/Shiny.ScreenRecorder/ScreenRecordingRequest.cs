namespace Shiny.ScreenRecorder;


/// <summary>
/// What to record and where to put it.
/// </summary>
/// <remarks>
/// Every optional member maps to a <see cref="ScreenRecorderCapabilities"/> flag. Setting one the
/// platform does not have throws <see cref="ScreenRecorderNotSupportedException"/> from
/// <see cref="IScreenRecorder.Start"/> rather than being quietly ignored - a recording that
/// silently came out at the wrong frame rate, without the microphone, or of the wrong display is
/// worse than one that refused to start.
/// </remarks>
public record ScreenRecordingRequest
{
    /// <summary>
    /// Where to write the video. Null puts it in the platform cache directory under a generated
    /// name.
    /// </summary>
    /// <remarks>
    /// <para>The extension is not honoured - each platform writes the container its encoder
    /// produces, and <see cref="ScreenRecordingResult.MimeType"/> reports which. In practice that
    /// is MP4 everywhere except some browsers, which produce WebM.</para>
    /// <para>Ignored on Blazor WebAssembly, which has no filesystem. Read the recording back with
    /// <see cref="ScreenRecordingResult.OpenRead"/>, which works on every platform.</para>
    /// <para>An existing file at this path is overwritten.</para>
    /// </remarks>
    public string? OutputPath { get; init; }

    /// <summary>
    /// The display, window or application to record, from <see cref="IScreenRecorder.GetTargets"/>.
    /// Null records the primary display - or, on iOS and Mac Catalyst, the app itself.
    /// </summary>
    /// <remarks>
    /// Needs <see cref="ScreenRecorderCapabilities.DisplaySelection"/> or
    /// <see cref="ScreenRecorderCapabilities.WindowSelection"/> depending on the target's
    /// <see cref="CaptureTarget.Kind"/>. On Linux and in the browser the compositor runs its own
    /// picker during <see cref="IScreenRecorder.Start"/>, so this must be left null there.
    /// </remarks>
    public CaptureTarget? Target { get; init; }

    /// <summary>Mix the microphone into the recording.</summary>
    /// <remarks>
    /// Needs <see cref="ScreenRecorderCapabilities.Microphone"/>, plus the platform's own
    /// microphone permission - <c>RECORD_AUDIO</c> on Android, <c>NSMicrophoneUsageDescription</c>
    /// on Apple. Call <see cref="IScreenRecorder.RequestAccess"/> with this set before starting.
    /// </remarks>
    public bool IncludeMicrophone { get; init; }

    /// <summary>Capture the audio the device is playing.</summary>
    /// <remarks>
    /// Needs <see cref="ScreenRecorderCapabilities.SystemAudio"/>. How much "system" covers varies
    /// sharply by platform - see the flag.
    /// </remarks>
    public bool IncludeSystemAudio { get; init; }

    /// <summary>Draw the mouse cursor into the recording. Defaults to true.</summary>
    /// <remarks>
    /// Needs <see cref="ScreenRecorderCapabilities.CursorToggle"/> to set this to false. The
    /// touch platforms have no cursor to draw and report the flag as unavailable rather than
    /// accepting a setting that would do nothing.
    /// </remarks>
    public bool ShowCursor { get; init; } = true;

    /// <summary>Frames per second. Null takes the platform default, which is 30 everywhere.</summary>
    /// <remarks>
    /// Needs <see cref="ScreenRecorderCapabilities.FrameRateControl"/>. This is a ceiling, not a
    /// promise - every backend here is change-driven and a still screen produces no frames at all.
    /// </remarks>
    public int? FrameRate { get; init; }

    /// <summary>Target video bitrate in bits per second. Null lets the platform choose from the resolution.</summary>
    /// <remarks>Needs <see cref="ScreenRecorderCapabilities.BitrateControl"/>.</remarks>
    public int? VideoBitrate { get; init; }

    /// <summary>
    /// Downscale so the video is no wider than this, preserving aspect ratio. Null records at
    /// native resolution.
    /// </summary>
    /// <remarks>
    /// <para>Needs <see cref="ScreenRecorderCapabilities.Downscaling"/>. Worth setting on modern
    /// phones and Retina displays, where native resolution produces very large files for very
    /// little visible gain.</para>
    /// <para>Rounded down to an even number of pixels - H.264 cannot encode odd dimensions.</para>
    /// </remarks>
    public int? MaxWidth { get; init; }

    /// <summary>
    /// Stop automatically after this long. Null records until told to stop.
    /// </summary>
    /// <remarks>
    /// The recording is stopped cleanly and the file is complete, then
    /// <see cref="IScreenRecording.Faulted"/> fires with
    /// <see cref="ScreenRecordingFaultReason.MaxDurationReached"/> carrying the result. Measured
    /// against <see cref="IScreenRecording.Elapsed"/>, so a paused span does not count towards it.
    /// </remarks>
    public TimeSpan? MaxDuration { get; init; }


    /// <summary>
    /// Checks the request against what the platform can do, throwing rather than letting a
    /// setting be silently dropped.
    /// </summary>
    /// <remarks>
    /// Called for you by <see cref="AbstractScreenRecorder.Start"/> before any native work starts.
    /// Public so a caller can validate a request while building UI - the exception message names
    /// the capability and the platform reason.
    /// </remarks>
    public void AssertValid(ScreenRecorderCapabilities capabilities, string platformReason)
    {
        if (!capabilities.HasFlag(ScreenRecorderCapabilities.Recording))
            throw ScreenRecorderNotSupportedException.For(ScreenRecorderCapabilities.Recording, platformReason);

        if (this.FrameRate is <= 0 or > 240)
            throw new ArgumentOutOfRangeException(nameof(this.FrameRate), this.FrameRate, "Frame rate must be between 1 and 240");

        if (this.VideoBitrate is <= 0)
            throw new ArgumentOutOfRangeException(nameof(this.VideoBitrate), this.VideoBitrate, "Video bitrate must be greater than zero");

        if (this.MaxWidth is <= 0)
            throw new ArgumentOutOfRangeException(nameof(this.MaxWidth), this.MaxWidth, "Max width must be greater than zero");

        if (this.MaxDuration is { } d && d <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(this.MaxDuration), this.MaxDuration, "Max duration must be greater than zero");

        Require(this.IncludeMicrophone, ScreenRecorderCapabilities.Microphone);
        Require(this.IncludeSystemAudio, ScreenRecorderCapabilities.SystemAudio);
        Require(!this.ShowCursor, ScreenRecorderCapabilities.CursorToggle);
        Require(this.FrameRate != null, ScreenRecorderCapabilities.FrameRateControl);
        Require(this.VideoBitrate != null, ScreenRecorderCapabilities.BitrateControl);
        Require(this.MaxWidth != null, ScreenRecorderCapabilities.Downscaling);

        if (this.Target != null)
        {
            var needed = this.Target.Kind == CaptureTargetKind.Display
                ? ScreenRecorderCapabilities.DisplaySelection
                : ScreenRecorderCapabilities.WindowSelection;

            Require(true, needed);
        }

        void Require(bool requested, ScreenRecorderCapabilities capability)
        {
            if (requested && !capabilities.HasFlag(capability))
                throw ScreenRecorderNotSupportedException.For(capability, platformReason);
        }
    }
}
