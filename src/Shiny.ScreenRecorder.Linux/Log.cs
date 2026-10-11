using Microsoft.Extensions.Logging;

namespace Shiny.ScreenRecorder;


internal static partial class LinuxLog
{
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Debug,
        Message = "Encoder command: {command}"
    )]
    public static partial void EncoderCommandBuilt(this ILogger logger, string command);


    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Debug,
        Message = "The desktop portal is not available on this session bus"
    )]
    public static partial void PortalUnavailable(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Debug,
        Message = "Closing the portal session failed"
    )]
    public static partial void PortalCloseFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Warning,
        Message = "Could not signal the encoder to stop (errno {errno}) - the recording may be missing its index"
    )]
    public static partial void SignalFailed(this ILogger logger, int errno);


    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Warning,
        Message = "The encoder had to be killed rather than finishing cleanly - the recording may not play"
    )]
    public static partial void EncoderDidNotFinalise(this ILogger logger);
}
