using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Integrations.Sqlite.Logging
{
    public class SqliteLogger : ILogger
    {
        public SqliteLogger(ShinySqliteConnection conn) { }

        public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
        public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => throw new NotImplementedException();
    }
}
