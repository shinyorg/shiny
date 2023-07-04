using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "HTTP Transfer Repository {action} - HTTP Transfer: {identifier} / {uri}"
    )]
    public static partial void RepositoryChange(this ILogger logger, RepositoryAction action, string identifier, string uri);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "HTTP Transfer Update - {identifier} / {status}"
    )]
    public static partial void TransferUpdate(this ILogger logger, string identifier, HttpTransferState status);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "{methodName} - No Transfer Found for {identifier}"
    )]
    public static partial void NoTransferFound(this ILogger logger, string identifier, [CallerMemberName] string? methodName = null);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "{methodName} - {identifier}: {totalXfer} / {totalToXfer}"
    )]
    public static partial void TransferProgress(this ILogger logger, string identifier, long totalXfer, long totalToXfer, [CallerMemberName] string? methodName = null);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "{methodName} - {identifier}"
    )]
    public static partial void StateMethod(this ILogger logger, string identifier, [CallerMemberName] string? methodName = null);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "HTTP Transfer: {identifier} - {message}"
    )]
    public static partial void StandardInfo(this ILogger logger, string identifier, string message);
}