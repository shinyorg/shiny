using System;
using Microsoft.Extensions.Logging;

namespace Shiny;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "{handlerTypeName} handling lifecycle event for {eventTypeName}"
    )]
    public static partial void LifecycleInfo(this ILogger logger, string handlerTypeName, string eventTypeName);
    
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to execute lifecycle call - '{handlerTypeName}' for lifecycle event '{eventTypeName}'"
    )]
    public static partial void LifecycleError(this ILogger logger, Exception exception, string handlerTypeName, string eventTypeName);
}