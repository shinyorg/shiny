using System;
using Microsoft.Extensions.Logging;

namespace Shiny.Logging.Sqlite;


public class SqliteLogger : ILogger
{
    readonly LogLevel configLogLevel;
    readonly LoggingSqliteConnection conn;


    public SqliteLogger(LogLevel logLevel, LoggingSqliteConnection conn)
    {
        this.configLogLevel = logLevel;
        this.conn = conn;
    }


    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!this.IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        this.conn.GetConnection().Insert(new LogStore
        {
            Message = message,
            EventId = eventId.Id,
            LogLevel = logLevel,
            TimestampUtc = DateTime.UtcNow
        });
    }
}