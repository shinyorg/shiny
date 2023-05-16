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
}