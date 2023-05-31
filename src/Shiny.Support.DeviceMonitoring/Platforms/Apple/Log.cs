using System;
using Microsoft.Extensions.Logging;
using Shiny.Net;

namespace Shiny;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Network change detected - {connectionTypes} - {access}"
    )]
    public static partial void NetworkChange(this ILogger logger, ConnectionTypes connectionTypes, NetworkAccess access);
}