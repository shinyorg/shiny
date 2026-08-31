using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Screen recording starting - output '{outputPath}', target '{target}'"
    )]
    public static partial void RecordingStarting(this ILogger logger, string? outputPath, string? target);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Screen recording stopped - {duration}, {byteSize} bytes"
    )]
    public static partial void RecordingStopped(this ILogger logger, TimeSpan duration, long byteSize);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Screen recording paused at {elapsed}"
    )]
    public static partial void RecordingPaused(this ILogger logger, TimeSpan elapsed);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Screen recording resumed at {elapsed}"
    )]
    public static partial void RecordingResumed(this ILogger logger, TimeSpan elapsed);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Screen recording ended without being asked - {reason}"
    )]
    public static partial void RecordingFaulted(this ILogger logger, ScreenRecordingFaultReason reason, Exception? exception);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Screen recording hit its maximum duration of {maxDuration}"
    )]
    public static partial void MaxDurationReached(this ILogger logger, TimeSpan maxDuration);


    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "The duration watchdog failed - the recording will run until stopped"
    )]
    public static partial void WatchdogFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Tearing down a cancelled recording failed"
    )]
    public static partial void CancelFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "Finalising the recording after it faulted failed - nothing could be salvaged"
    )]
    public static partial void FinishAfterFaultFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "A Faulted handler threw"
    )]
    public static partial void FaultHandlerThrew(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Error,
        Message = "A StateChanged handler threw"
    )]
    public static partial void StateHandlerThrew(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "Could not delete the recording at '{path}'"
    )]
    public static partial void DeleteOutputFailed(this ILogger logger, string path, Exception exception);


    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Debug,
        Message = "Screen capture configured - {width}x{height} @ {frameRate}fps, {bitrate}bps"
    )]
    public static partial void CaptureConfigured(this ILogger logger, int width, int height, int frameRate, int bitrate);


    [LoggerMessage(
        EventId = 14,
        Level = LogLevel.Debug,
        Message = "Enumerated {count} capture target(s)"
    )]
    public static partial void TargetsEnumerated(this ILogger logger, int count);


    [LoggerMessage(
        EventId = 15,
        Level = LogLevel.Warning,
        Message = "Screen recording permission was refused - {detail}"
    )]
    public static partial void PermissionRefused(this ILogger logger, string detail);
}


internal static partial class PlatformLog
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Warning,
        Message = "The writer rejected a {track} sample - {reason}"
    )]
    public static partial void AppendRejected(this ILogger logger, string track, string reason);


    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Could not retime a sample at {seconds}s - the pause gap will show in the output"
    )]
    public static partial void RetimeFailed(this ILogger logger, double seconds);


    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Debug,
        Message = "Using the {path} recording path"
    )]
    public static partial void UsingCapturePath(this ILogger logger, string path);
}


internal static partial class CapturePlatformLog
{
    [LoggerMessage(
        EventId = 110,
        Level = LogLevel.Debug,
        Message = "The capture stopped but reported '{reason}' - the file is checked separately"
    )]
    public static partial void StopReportedError(this ILogger logger, string reason);


    [LoggerMessage(
        EventId = 111,
        Level = LogLevel.Warning,
        Message = "Screen recording consent was declined by the user"
    )]
    public static partial void ConsentDeclined(this ILogger logger);
}


internal static partial class AndroidPlatformLog
{
    [LoggerMessage(
        EventId = 120,
        Level = LogLevel.Warning,
        Message = "The muxer rejected a sample"
    )]
    public static partial void MuxerWriteFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 121,
        Level = LogLevel.Error,
        Message = "The muxer could not be finalised - the output file has no index and will not play"
    )]
    public static partial void MuxerStopFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 122,
        Level = LogLevel.Error,
        Message = "The {track} encoder drain loop failed"
    )]
    public static partial void EncoderDrainFailed(this ILogger logger, string track, Exception exception);


    [LoggerMessage(
        EventId = 123,
        Level = LogLevel.Warning,
        Message = "Tearing down the media projection failed"
    )]
    public static partial void ProjectionTeardownFailed(this ILogger logger, Exception exception);
}


internal static partial class WindowsPlatformLog
{
    [LoggerMessage(
        EventId = 130,
        Level = LogLevel.Warning,
        Message = "Could not wrap a captured frame as an encoder sample - the frame was dropped"
    )]
    public static partial void SampleCreationFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 131,
        Level = LogLevel.Warning,
        Message = "The transcoder did not finish within 30 seconds - the file may be incomplete"
    )]
    public static partial void TranscodeDidNotFinish(this ILogger logger);


    [LoggerMessage(
        EventId = 132,
        Level = LogLevel.Debug,
        Message = "Disposing the capture session failed"
    )]
    public static partial void CaptureSessionDisposeFailed(this ILogger logger, Exception exception);
}
