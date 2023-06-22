using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Stores.Impl;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "{message}: {typeFullName} to store: {storeAlias}"
    )]
    public static partial void BindInfo(this ILogger logger, string message, string typeFullName, string storeAlias);
    

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Failed to bind {propertyName} on {typeFullName}"
    )]
    public static partial void PropertyBindError(this ILogger logger, Exception exception, string typeFullName, string propertyName);
    
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Binding Failure: {typeFullName} - {storeAlias}"
    )]
    public static partial void BindError(this ILogger logger, Exception exception, string typeFullName, string storeAlias);

    //this.logger?.LogInformation($"Successfully bound model: {npc.GetType().FullName} to store: {store.Alias}");

    //this.logger?.LogError(ex, $"Failed to bind model: {npc?.GetType().FullName ?? "Unknown"} to store: {store.Alias}");
}