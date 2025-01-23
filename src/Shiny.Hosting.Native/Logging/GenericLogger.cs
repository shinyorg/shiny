using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Logging;


public class GenericLogger<T> : ILogger<T>
{
    readonly ILogger internalLogger;
    public GenericLogger(ILoggerFactory loggerFactory)
        => this.internalLogger = loggerFactory.CreateLogger(typeof(T).FullName);


    public IDisposable BeginScope<TState>(TState state)
        => this.internalLogger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => this.internalLogger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        => this.internalLogger.Log(logLevel, eventId, state, exception, formatter);
}
