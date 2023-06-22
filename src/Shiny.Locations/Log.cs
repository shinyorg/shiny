using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Deferred Distance: Min: {minMeters}. Current: {meters} - FIRE EVENT: {fireEvent}"
    )]
    public static partial void DeferDistanceInfo(this ILogger logger, double minMeters, double meters, bool fireEvent);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Deferred Time: Min: {minTime}. Current: {currentTime} - FIRE EVENT: {fireEvent}"
    )]
    public static partial void DeferTimeInfo(this ILogger logger, TimeSpan minTime, TimeSpan currentTime, bool fireEvent);
}