using System;
using System.Reactive.Disposables;

using Microsoft.Extensions.Logging;

using Shiny.Logging;
using Shiny.Models;


namespace Shiny.Integrations.Sqlite.Logging
{
    public class SqliteLogger : ILogger
    {
        readonly LogLevel configLogLevel;
        readonly ShinySqliteConnection conn;


        public SqliteLogger(LogLevel logLevel, ShinySqliteConnection conn)
        {
            this.configLogLevel = logLevel;
            this.conn = conn;
        }


        public IDisposable BeginScope<TState>(TState state) => Disposable.Empty;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= this.configLogLevel;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            this.conn.GetConnection().Insert(new LogStore
            {
                Description = message,
                Detail = exception?.ToString(),
                IsError = exception != null,
                //Parameters <= could come from scope
                TimestampUtc = DateTime.UtcNow
            });
        }
    }
}
