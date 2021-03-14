using System;
using Microsoft.Extensions.Logging;


namespace Shiny.Integrations.Sqlite.Logging
{
    public class SqliteLoggerProvider : ILoggerProvider
    {
        readonly LogLevel logLevel;
        public SqliteLoggerProvider(LogLevel logLevel) => this.logLevel = logLevel;

        public ILogger CreateLogger(string categoryName) => new SqliteLogger(this.logLevel, ShinyHost.Resolve<ShinySqliteConnection>());
        public void Dispose() { }
    }
}
