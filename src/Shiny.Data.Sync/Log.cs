using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Data.Sync;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Sync Repository {action} - Operation: {identifier} / {endpointKey}"
    )]
    public static partial void RepositoryChange(this ILogger logger, RepositoryAction action, string identifier, string endpointKey);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Sync Operation Update - {identifier} / {state}"
    )]
    public static partial void OperationUpdate(this ILogger logger, string identifier, SyncOperationState state);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "{methodName} - No Operation Found for {identifier}"
    )]
    public static partial void NoOperationFound(this ILogger logger, string identifier, [CallerMemberName] string? methodName = null);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Sync Operation: {identifier} - {message}"
    )]
    public static partial void StandardInfo(this ILogger logger, string identifier, string message);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Sync Operation: {identifier} - {message}"
    )]
    public static partial void StandardWarn(this ILogger logger, string identifier, string message);
}
