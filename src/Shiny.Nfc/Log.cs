using Microsoft.Extensions.Logging;

namespace Shiny.Nfc;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "NFC scan session starting"
    )]
    public static partial void ScanStarting(this ILogger logger);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "NFC scan session ended"
    )]
    public static partial void ScanEnded(this ILogger logger);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "An unhandled tag was dropped for a newer one - the scan loop was still busy with the previous tag"
    )]
    public static partial void TagDropped(this ILogger logger);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "This Type 2 tag declares no dynamic lock bytes - it is marked read-only and its static lock bits are set, but memory past page 15 relies on readers honouring the capability container"
    )]
    public static partial void NoDynamicLockBytes(this ILogger logger);
}
