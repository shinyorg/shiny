using Microsoft.Extensions.Logging;

namespace Shiny.Support.Repositories;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Type: {type} - Action: {action} - Identifier: {identifier}"
    )]
    public static partial void Change(this ILogger logger, RepositoryAction action, string type, string identifier);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "GET MISS - Type: {type} - Identifier: {identifier} does not exist - items in type db: {typeCount}"
    )]
    public static partial void GetNotExists(this ILogger logger, string type, string identifier, int typeCount);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Action: {action} - Type: {type} - Type Count: {typeCount}"
    )]
    public static partial void InternalCount(this ILogger logger, string action, string type, int typeCount);
}