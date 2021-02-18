using System;
using Microsoft.Extensions.Logging;
using Shiny.Models;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteLogger : ILogger
    {
        readonly ShinySqliteConnection conn;


        public SqliteLogger(ShinySqliteConnection conn)
        {
            this.conn = conn;
        }


        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
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
