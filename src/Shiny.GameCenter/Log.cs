using Microsoft.Extensions.Logging;

namespace Shiny.GameCenter;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Game service player changed - {playerId}"
    )]
    public static partial void PlayerChanged(this ILogger logger, string? playerId);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Silent sign-in to the game service failed"
    )]
    public static partial void SilentSignInFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "'{key}' was rejected by the game service as an unknown or mismatched id - dropping it from the queue"
    )]
    public static partial void QueuedItemDropped(this ILogger logger, string key, Exception exception);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Sending queued game service work stopped - {remaining} item(s) kept for later"
    )]
    public static partial void FlushStopped(this ILogger logger, int remaining, Exception exception);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "'{key}' is not on this game service - skipping"
    )]
    public static partial void NotOnPlatform(this ILogger logger, string key);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Could not refresh achievement progress from the game service - incrementing from local progress"
    )]
    public static partial void SyncFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Could not save game service state to {path}"
    )]
    public static partial void SaveFailed(this ILogger logger, string path, Exception exception);


    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "{operation} is not supported by this game service"
    )]
    public static partial void NotSupported(this ILogger logger, string operation);
}
